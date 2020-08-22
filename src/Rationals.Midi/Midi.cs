using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Diagnostics;


// NoteOn cents value:
//    0 -> C4
// 1200 -> C5


namespace Rationals.Midi
{
    public interface IMidiOut {
        void Dispose();
        //void Send(int message);

        void SendNoteOn(int channel, int noteNumber, int velocity);
        void SendNoteOff(int channel, int noteNumber, int velocity);
        void SendPatchChange(int channel, int patchNumber);
        void SendPitchWheelChange(int channel, int pitchWheel);
    }

    public class MidiPlayer
    {
        static public IMidiOut CreateDevice(int deviceIndex) {
#if USE_MANAGEDMIDI
            return new ManagedMidiMidiOut(deviceIndex);
#else
            return null;
#endif
        }

        private IMidiOut _device;

        public MidiPlayer(int deviceIndex) {
            _device = CreateDevice(deviceIndex);
        }

        public void Dispose() {
            _device.Dispose();
            _device = null;
        }

        public const int MaxVelocity = 0x7F;

        // Extended pitch
        private struct PitchX {
            public int note; // in midi format: 60 = C3, 72 = C4
            public int bend; // in midi format: 0..0x3FFF (0x2000 for no bend)
        }
        private static PitchX GetPitchX(float cents) {
            //    0f -> C4
            // 1200f -> C5
            float semitones = cents / 100f;
            int note = (int)Math.Round(semitones);
            float bend = semitones - note; // -0.5..0.5 in semitones
            int midiNote = 72 + note;

            //!!! clip no midi spec
            while (midiNote <   0) midiNote += 12;
            while (midiNote > 127) midiNote -= 12;

            return new PitchX {
                note = midiNote,
                bend = 0x2000 + (int)(0x1000 * bend)
            };
        }

        // Mapping virtual channels -> raw channels
        private struct VirtualChannelState {
            public int instrument;
        }
        private struct RawChannelState {
            public int instrument;
            public int bend;
            public int playingNotes;
        }

        private VirtualChannelState[] _virtualChannels = new VirtualChannelState[16];
        private RawChannelState[] _rawChannels = new RawChannelState[16]; // size of Midi.Enums.Channel
        private object _channelStateLock = new object();

        private int FindRawChannel(int virtualChannel, PitchX pitch) { // _channelStateLock locked
            int[] playingThisNote = GetPlayingNoteRawChannels(virtualChannel, pitch);
            VirtualChannelState v = _virtualChannels[virtualChannel];
            for (int ri = 0; ri < 16; ++ri) {
                if (playingThisNote.Contains(ri)) continue; // this note already played on this raw channel
                RawChannelState r = _rawChannels[ri];
                if (r.playingNotes == 0) return ri;
                if (r.instrument == v.instrument && r.bend == pitch.bend) return ri;
            }
            throw new Exception("No free midi channel");
        }

        // https://en.wikipedia.org/wiki/General_MIDI#Program_change_events
        public void SetInstrument(int virtualChannel, int instrument) { // Main thread
            lock (_channelStateLock) {
                _virtualChannels[virtualChannel].instrument = instrument;
            }
        }

        // Saving playing notes
        private Dictionary<int, List<int>> _playingNotes = new Dictionary<int, List<int>>();
        private static int GetNoteKey(int virtualChannel, PitchX pitch) {
            return (virtualChannel << 24) | ((int)pitch.note << 16) | (pitch.bend);
        }
        private void SavePlayingNoteRawChannel(int virtualChannel, PitchX pitch, int rawChannel) {
            int noteKey = GetNoteKey(virtualChannel, pitch);
            List<int> cs = null;
            _playingNotes.TryGetValue(noteKey, out cs);
            if (cs == null) {
                cs = new List<int>();
                _playingNotes[noteKey] = cs;
            }
            cs.Add(rawChannel);            
        }
        private int GetPlayingNoteRawChannel(int virtualChannel, PitchX pitch, bool remove = true) {
            int noteKey = GetNoteKey(virtualChannel, pitch);
            List<int> cs = null;
            _playingNotes.TryGetValue(noteKey, out cs);
            if (cs == null || cs.Count == 0) throw new Exception("Playing note not found");
            int rawChannel = cs[0];
            if (remove) {
                cs.RemoveAt(0);
            }
            return rawChannel;
        }
        private int[] GetPlayingNoteRawChannels(int virtualChannel, PitchX pitch) {
            int noteKey = GetNoteKey(virtualChannel, pitch);
            List<int> cs = null;
            _playingNotes.TryGetValue(noteKey, out cs);
            if (cs == null) return new int[] { };
            return cs.ToArray();
        }


        // Clock

        // Message queue
        private abstract class Message {
            public float Time; // occurrence time. in beats
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
                        Time = Time + Duration,
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
        private List<Message> _messages = new List<Message>(); // messages sorted by .Time // multithread
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
                Debug.Assert(_clockThread == null, "We can't restart a thread");
                _clockThread = new Thread(ClockProc);
                _clockThread.Name = "MidiPlayer Clock";
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
            _clockThread = null;
            _clockStarted = false;
        }

        public bool IsClockStarted() {
            return _clockStarted;
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
                    if (_messages[i - 1].Time <= m.Time) {
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
                    if (_currentBeats < m.Time) break;
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
            int c; // zero-based raw channel index
            bool setInstrument = false;
            bool setBend = false;

            lock (_channelStateLock)
            {
                v = _virtualChannels[virtualChannel];
                c = FindRawChannel(virtualChannel, pitch);
                RawChannelState r = _rawChannels[c];

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

                _rawChannels[c] = r;
            }

            // Raw midi
            if (setInstrument) {
                _device.SendPatchChange(channel: c + 1, v.instrument);
            }
            if (setBend) {
                _device.SendPitchWheelChange(channel: c + 1, pitch.bend);
            }
            _device.SendNoteOn(channel: c + 1, noteNumber: pitch.note, velocity);
        }

        private void NoteOffRaw(int virtualChannel, float cents, int velocity = 0x7F)
        {
            PitchX pitch = GetPitchX(cents);
            int c;

            lock (_channelStateLock)
            {
                c = GetPlayingNoteRawChannel(virtualChannel, pitch);
                RawChannelState r = _rawChannels[c];

                Debug.WriteLine("NoteOffRaw v{0}->r{1} {2}({3}+{4:X})", virtualChannel, (int)c, cents, pitch.note, pitch.bend);

                r.playingNotes -= 1;

                _rawChannels[c] = r;
            }

            // Raw midi
            _device.SendNoteOff(channel: c + 1, noteNumber: pitch.note, velocity);
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
                    Time = GetCurrentBeats() + duration,
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
                Time = GetCurrentBeats() + delay,
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
                    Time = GetCurrentBeats() + delay,
                    VirtualChannel = virtualChannel,
                    Cents = cents,
                    Velocity = velocity
                });
            }
        }

    }
}
