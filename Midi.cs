using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;

// Midi.dll - https://github.com/micdah/midi-dot-net
using Midi.Devices;
using Midi.Enums;
using Midi.Instruments;
using Midi.Messages;

namespace Midi {

    public class MidiPlayer
    {
        public const int MaxVelocity = 0x7F;

        private IOutputDevice _device;

        public MidiPlayer(IOutputDevice device) {
            _device = device;
        }

        // Extended pitch
        private struct PitchX {
            public Pitch note;
            public int bend; // in midi format: 0..0x3FFF (0x2000 for no bend)
        }
        private static PitchX GetPitchX(float cents) {
            //    0f -> C4
            // 1200f -> C5
            float semitones = cents / 100f;
            int note = (int)Math.Round(semitones);
            float bend = semitones - note; // -0.5..0.5 in semitones
            return new PitchX {
                note = Pitch.C4 + note,
                bend = 0x2000 + (int)(0x1000 * bend)
            };
        }

        // Mapping virtual channels -> raw channels
        private struct VirtualChannelState {
            public Instrument instrument;
        }
        private struct RawChannelState {
            public Instrument instrument;
            public int bend;
            public int playingNotes;
        }

        private VirtualChannelState[] _virtualChannels = new VirtualChannelState[16];
        private RawChannelState[] _rawChannels = new RawChannelState[16]; // size of Midi.Enums.Channel
        private object _channelStateLock = new object();

        private Channel FindRawChannel(int virtualChannel, PitchX pitch) { // _channelStateLock locked
            Channel[] playingThisNote = GetPlayingNoteRawChannels(virtualChannel, pitch);
            VirtualChannelState v = _virtualChannels[virtualChannel];
            for (int ri = 0; ri < 16; ++ri) {
                if (playingThisNote.Contains((Channel)ri)) continue; // this note already played on this raw channel
                RawChannelState r = _rawChannels[ri];
                if (r.playingNotes == 0) return (Channel)ri;
                if (r.instrument == v.instrument && r.bend == pitch.bend) return (Channel)ri;
            }
            throw new Exception("No free midi channel");
        }

        public void SetInstrument(int virtualChannel, Instrument instrument) { // Main thread
            lock (_channelStateLock) {
                _virtualChannels[virtualChannel].instrument = instrument;
            }
        }

        // Saving playing notes
        private Dictionary<int, List<Channel>> _playingNotes = new Dictionary<int, List<Channel>>();
        private static int GetNoteKey(int virtualChannel, PitchX pitch) {
            return (virtualChannel << 24) | ((int)pitch.note << 16) | (pitch.bend);
        }
        private void SavePlayingNoteRawChannel(int virtualChannel, PitchX pitch, Channel rawChannel) {
            int noteKey = GetNoteKey(virtualChannel, pitch);
            List<Channel> cs = null;
            _playingNotes.TryGetValue(noteKey, out cs);
            if (cs == null) {
                cs = new List<Channel>();
                _playingNotes[noteKey] = cs;
            }
            cs.Add(rawChannel);            
        }
        private Channel GetPlayingNoteRawChannel(int virtualChannel, PitchX pitch, bool remove = true) {
            int noteKey = GetNoteKey(virtualChannel, pitch);
            List<Channel> cs = null;
            _playingNotes.TryGetValue(noteKey, out cs);
            if (cs == null || cs.Count == 0) throw new Exception("Playing note not found");
            Channel rawChannel = cs[0];
            if (remove) {
                cs.RemoveAt(0);
            }
            return rawChannel;
        }
        private Channel[] GetPlayingNoteRawChannels(int virtualChannel, PitchX pitch) {
            int noteKey = GetNoteKey(virtualChannel, pitch);
            List<Channel> cs = null;
            _playingNotes.TryGetValue(noteKey, out cs);
            if (cs == null) return new Channel[] { };
            return cs.ToArray();
        }


        // Clock

        // Message queue
        private abstract class Message {
            public float Start; // in beats
            public int VirtualChannel;
            public abstract void Run(MidiPlayer player);
        }
        private class NoteMessage : Message {
            public float Cents;
            public int Velocity;
            public float Duration; // in beats
            public override void Run(MidiPlayer player) {
                player.NoteOnRaw(VirtualChannel, Cents, Velocity);
                if (Duration > 0f) {
                    player.AddMessage(new NoteOffMessage {
                        Start = Start + Duration,
                        VirtualChannel = VirtualChannel,
                        Cents = Cents,
                        Velocity = Velocity
                    });
                }
            }
        }
        private class NoteOffMessage : Message {
            public float Cents;
            public int Velocity;
            public override void Run(MidiPlayer player) {
                player.NoteOffRaw(VirtualChannel, Cents, Velocity);
            }
        }

        private object _clockLock = new object(); // locking clock between Main and Clock threads
        private bool _clockStarted = false; // Main thread
        private bool _stopClock = false; // multithread
        private bool _waitForEnd = false; // set in Main thread before starting Clock
        private float _beatsPerMinute; // multithread; allowing changing tempo
        private List<Message> _messages = new List<Message>(); // multithread
        private Thread _clockThread = null;

        private long _currentTicks; // system ticks; Clock thread
        private float _currentBeats; // multithread; zero on start; allowing changing tempo

        public void StartClock(float beatsPerMinute = 120f, bool waitForEnd = false) {
            if (_clockStarted) throw new Exception("Clock already started");
            // Reset
            _beatsPerMinute = beatsPerMinute;
            _waitForEnd = waitForEnd;
            //_messages.Clear();
            _currentBeats = 0f;
            _stopClock = false;
            //
            if (_waitForEnd) { // Run the clock in same thread
                _clockStarted = true;
                ClockProc();
                _clockStarted = false;
            } else {
                if (_clockThread == null) {
                    _clockThread = new Thread(ClockProc);
                }
                _clockThread.Start();
                _clockStarted = true;
            }
        }

        public void StopClock() {
            if (!_clockStarted) return;
            lock (_clockLock) {
                _stopClock = true;
            }
            _clockThread.Join();
            _clockStarted = false;
        }

        private void ClockProc() { // Main thread (if _waitForEnd) or Clock thread
            _currentTicks = DateTime.Now.Ticks;
            for (;;) {
                // Update time beats
                lock (_clockLock) {
                    if (_stopClock) break;
                    //
                    long ticks = DateTime.Now.Ticks;
                    _currentBeats += _beatsPerMinute * (ticks - _currentTicks) / TimeSpan.TicksPerMinute;
                    _currentTicks = ticks;
                }
                // Get current messages
                Message[] current = GetCurrentMessages();
                if (current == null) { // empty queue
                    if (_waitForEnd) break;
                } else {
                    foreach (Message m in current) {
                        m.Run(this);
                    }
                }
                //
                Thread.Sleep(5);
            }
        }

        private float GetCurrentBeats() { // Main thread
            if (!_clockStarted) return 0;
            lock (_clockLock) {
                return _currentBeats;
            }
        }

        private void AddMessage(Message m) {
            lock (_messages) {
                // keep sorted by Message.Start
                for (int i = _messages.Count; i > 0; --i) {
                    if (_messages[i - 1].Start <= m.Start) {
                        _messages.Insert(i, m);
                        return;
                    }
                }
                _messages.Insert(0, m);
            }
        }

        private Message[] GetCurrentMessages() { // clock thread
            var current = new List<Message>();
            lock (_messages) {
                if (_messages.Count == 0) return null;
                do {
                    Message m = _messages[0];
                    if (_currentBeats < m.Start) break;
                    current.Add(m);
                    _messages.RemoveAt(0);
                } while (_messages.Count > 0);
            }
            return current.ToArray();
        }

        private void NoteOnRaw(int virtualChannel, float cents, int velocity = 0x7F)
        {
            PitchX pitch = GetPitchX(cents);
            VirtualChannelState v;
            Channel c;
            bool setInstrument = false;
            bool setBend = false;

            lock (_channelStateLock)
            {
                v = _virtualChannels[virtualChannel];
                c = FindRawChannel(virtualChannel, pitch);
                RawChannelState r = _rawChannels[(int)c];

                Debug.WriteLine("NoteOnRaw v{0}->r{1} {2}({3}+{4:X})", virtualChannel, (int)c, cents, pitch.note, pitch.bend);

                if (r.instrument != v.instrument) {
                    r.instrument = v.instrument;
                    setInstrument = true;
                }

                if (r.bend != pitch.bend) {
                    r.bend = pitch.bend;
                    setBend = true;
                }

                r.playingNotes += 1;
                SavePlayingNoteRawChannel(virtualChannel, pitch, c);

                _rawChannels[(int)c] = r;
            }

            // Raw midi
            if (setInstrument) {
                _device.SendProgramChange(c, v.instrument);
            }
            if (setBend) {
                _device.SendPitchBend(c, pitch.bend);
            }
            _device.SendNoteOn(c, pitch.note, velocity);
        }

        private void NoteOffRaw(int virtualChannel, float cents, int velocity = 0x7F)
        {
            PitchX pitch = GetPitchX(cents);
            Channel c;

            lock (_channelStateLock)
            {
                c = GetPlayingNoteRawChannel(virtualChannel, pitch);
                RawChannelState r = _rawChannels[(int)c];

                Debug.WriteLine("NoteOffRaw v{0}->r{1} {2}({3}+{4:X})", virtualChannel, (int)c, cents, pitch.note, pitch.bend);

                r.playingNotes -= 1;

                _rawChannels[(int)c] = r;
            }

            // Raw midi
            _device.SendNoteOff(c, pitch.note, velocity);
        }

        // Main thread
        public void NoteOn(int virtualChannel, float cents, int velocity = 0x7F, float duration = 0f) {
            if (duration < 0f) throw new ArgumentException("Negative duration");
            if (duration > 0f && !_clockStarted) throw new Exception("Start the clock to use note duration");
            //
            NoteOnRaw(virtualChannel, cents, velocity);
            //
            if (duration > 0f) {
                AddMessage(new NoteOffMessage {
                    Start = GetCurrentBeats() + duration,
                    VirtualChannel = virtualChannel,
                    Cents = cents,
                    Velocity = velocity
                });
            }
        }

        // Main thread
        public void ScheduleNote(int virtualChannel, float cents, int velocity = 0x7F, float delay = 0f, float duration = 0f) {
            if (delay < 0f) throw new ArgumentException("Negative delay");
            if (duration < 0f) throw new ArgumentException("Negative duration");
            //
            AddMessage(new NoteMessage {
                Start = GetCurrentBeats() + delay,
                VirtualChannel = virtualChannel,
                Cents = cents,
                Velocity = velocity,
                Duration = duration
            });
        }

        // Main thread
        public void NoteOff(int virtualChannel, float cents, int velocity = 0x7F, float delay = 0f) {
            if (delay < 0f) throw new ArgumentException("Negative delay");
            if (delay > 0f && !_clockStarted) throw new Exception("Start the clock to use delay");
            //
            if (delay == 0f) {
                NoteOffRaw(virtualChannel, cents, velocity);
            } else {
                AddMessage(new NoteOffMessage {
                    Start = GetCurrentBeats() + delay,
                    VirtualChannel = virtualChannel,
                    Cents = cents,
                    Velocity = velocity
                });
            }
        }

    }


    public static class Utils {

        private static void Test1(IOutputDevice outputDevice)
        {
            // Center the pitch bend.
            outputDevice.SendPitchBend(Channel.Channel1, 0x2000);

            // Play C, E, G in half second intervals.
            outputDevice.SendNoteOn(Channel.Channel1, Pitch.C4, 80);
            Thread.Sleep(200);
            outputDevice.SendNoteOn(Channel.Channel1, Pitch.E4, 80);
            Thread.Sleep(200);
            outputDevice.SendNoteOn(Channel.Channel1, Pitch.G4, 80);
            Thread.Sleep(200);

            // Now bend the pitches down.
            // https://www.midi.org/specifications-old/item/table-1-summary-of-midi-message
            //   Pitch Bend Change. This message is sent to indicate a change in the pitch bender (wheel or lever, typically). The pitch bender is measured by a fourteen bit value. 
            //   Center (no pitch change) is 2000H. Sensitivity is a function of the receiver, but may be set using RPN 0. (lllllll) are the least significant 7 bits. (mmmmmmm) are the most significant 7 bits.
            // https://sites.uci.edu/camp2014/2014/04/30/managing-midi-pitchbend-messages/
            //   Thus, on the scale from 0 to 16,383, a value of 0 means maximum downward bend, 8,192 means no bend, and 16,383 means maximum upward bend. 
            //   5. The amount of alteration in pitch caused by the pitchbend value is determined by the receiving device (i.e., the synthesizer or sampler). 
            //   A standard setting is variation by + or – 2 semitones. (For example, the note C could be bent as low as Bb or as high as D.) 
            //   Most synthesizers provide some way (often buried rather deep in some submenu of its user interface) to change the range of pitchbend to be + or – some other number of semitones.
            int steps = 10;
            for (var i = 0; i < steps; ++i) {
                outputDevice.SendPitchBend(Channel.Channel1, 0x2000 - 0x2000 * i / steps);
                Thread.Sleep(5000 / steps);
            }

            // Now release the C chord notes
            outputDevice.SendNoteOff(Channel.Channel1, Pitch.C4, 80);
            outputDevice.SendNoteOff(Channel.Channel1, Pitch.E4, 80);
            outputDevice.SendNoteOff(Channel.Channel1, Pitch.G4, 80);

            // Now center the pitch bend again.
            outputDevice.SendPitchBend(Channel.Channel1, 0x2000);
        }

        private static void Test2(IOutputDevice outputDevice) {
            // 3 same tones
            outputDevice.SendPitchBend(Channel.Channel1, 0x2000);
            outputDevice.SendNoteOn(Channel.Channel1, Pitch.C4, 80);
            Thread.Sleep(1000);
            outputDevice.SendNoteOff(Channel.Channel1, Pitch.C4, 80);
            //
            outputDevice.SendPitchBend(Channel.Channel1, 0x0);
            outputDevice.SendNoteOn(Channel.Channel1, Pitch.D4, 80);
            Thread.Sleep(1000);
            outputDevice.SendNoteOff(Channel.Channel1, Pitch.D4, 80);
            //
            outputDevice.SendPitchBend(Channel.Channel1, 0x4000 - 1);
            outputDevice.SendNoteOn(Channel.Channel1, Pitch.ASharp3, 80);
            Thread.Sleep(1000);
            outputDevice.SendNoteOff(Channel.Channel1, Pitch.ASharp3, 80);

            // Now center the pitch bend again.
            outputDevice.SendPitchBend(Channel.Channel1, 0x2000);
        }

        private static void Test3(IOutputDevice outputDevice)
        {
            Console.WriteLine("Playing the first two bars of Mary Had a Little Lamb...");
            var clock = new Clock(60 * 1); // 1 beat per second
            clock.Schedule(new NoteOnMessage(outputDevice, Channel.Channel1, Pitch.E4, 80, 0));
            clock.Schedule(new NoteOffMessage(outputDevice, Channel.Channel1, Pitch.E4, 80, 1));
            clock.Schedule(new NoteOnMessage(outputDevice, Channel.Channel1, Pitch.D4, 80, 1));
            clock.Schedule(new NoteOffMessage(outputDevice, Channel.Channel1, Pitch.D4, 80, 2));

            clock.Start();
            Thread.Sleep(5000);
            clock.Stop();
        }

        private static void Test4(IOutputDevice device) {
            // playing without clock

            var player = new MidiPlayer(device);

            player.SetInstrument(0, Instrument.Clarinet);

            player.NoteOn(0, 1200f);
            Thread.Sleep(1000);
            player.NoteOff(0, 1200f);

            player.NoteOn(0, 1250f);
            Thread.Sleep(1000);
            player.NoteOff(0, 1250f);

            player.NoteOn(0, 1300f);
            Thread.Sleep(1000);
            player.NoteOff(0, 1300f);
        }

        private static void Test5(IOutputDevice device) {
            // use clock for note duration

            var player = new MidiPlayer(device);
            player.StartClock(60 * 4);

            player.SetInstrument(0, Instrument.Clarinet);
            player.SetInstrument(1, Instrument.Banjo);

            player.NoteOn(0, 1200f, duration: 8f);

            Thread.Sleep(500);
            player.NoteOn(0, 1230f, duration: 8f);

            Thread.Sleep(500);
            player.NoteOn(1, 1260f, duration: 8f);

            Thread.Sleep(5000);

            player.StopClock();
        }

        private static void Test6(IOutputDevice device) {
            // use clock to schedule

            var player = new MidiPlayer(device);

            player.SetInstrument(0, Instrument.Cello);
            player.SetInstrument(1, Instrument.AcousticGrandPiano);

            player.ScheduleNote(1, 1200f, delay: 0f, duration: 4f);
            player.ScheduleNote(1, 1230f, delay: 1f, duration: 4f);
            player.ScheduleNote(0, 1260f, delay: 2f, duration: 4f);

            player.StartClock(60 * 2, waitForEnd: true);
        }

        private static void Test7(IOutputDevice device) {
            var player = new MidiPlayer(device);

            int n = 12 * 10; // steps in octave
            for (int i = 0; i < n; ++i) {
                player.ScheduleNote(
                    0, 
                    1200f * i/n, 
                    //i % 5 == 0 ? 0x7F: 0x5F, 
                    delay: i, 
                    duration: 1
                );
            }

            player.StartClock(60 * 8, waitForEnd: true);
        }

        public static void Test() {
            var device = DeviceManager.OutputDevices.FirstOrDefault();
            if (device == null) return;

            device.Open();

            // Midi.dll
            //Test1(device);
            //Test2(device);
            //Test3(device);

            // MidiPlayer
            //Test4(device);
            //Test5(device);
            //Test6(device);
            Test7(device);

            device.Close();
        }

    }
}
