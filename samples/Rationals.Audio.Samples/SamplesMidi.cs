//#define TEST_NAUDIO

using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Rationals.Testing;

#if TEST_NAUDIO
using NAudio.Midi;
#endif

// https://github.com/naudio/NAudio/blob/master/Docs/MidiInAndOut.md
// http://opensebj.blogspot.com/2009/09/naudio-tutorial-7-basics-of-midi-files.html

namespace Rationals.Midi
{
    [Test]
    public static class MidiSamples
    {
#if TEST_NAUDIO
        [Sample]
        private static void Pitch1()
        {
            var midiOut = new MidiOut(0);

            var e0 = new NoteOnEvent(0, channel: 1, noteNumber: 72, 0x40, 0); // C4
            var e1 = new NoteOnEvent(0, channel: 1, noteNumber: 76, 0x40, 0); // E4
            var e2 = new NoteOnEvent(0, channel: 1, noteNumber: 79, 0x40, 0); // G4

            // Center the pitch bend.
            midiOut.Send(new PitchWheelChangeEvent(0, channel: 1, 0x2000).GetAsShortMessage());

            // Play C, E, G in half second intervals.
            midiOut.Send(e0.GetAsShortMessage()); Thread.Sleep(200);
            midiOut.Send(e1.GetAsShortMessage()); Thread.Sleep(200);
            midiOut.Send(e2.GetAsShortMessage()); Thread.Sleep(200);

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
                int pitch = 0x2000 - 0x2000 * i / steps;
                midiOut.Send(new PitchWheelChangeEvent(0, channel: 1, pitch).GetAsShortMessage());
                Thread.Sleep(5000 / steps);
            }

            // Now release the C chord notes
            midiOut.Send(e0.OffEvent.GetAsShortMessage());
            midiOut.Send(e1.OffEvent.GetAsShortMessage());
            midiOut.Send(e2.OffEvent.GetAsShortMessage());

            // Now center the pitch bend again.
            midiOut.Send(new PitchWheelChangeEvent(0, channel: 1, 0x2000).GetAsShortMessage());

            midiOut.Dispose();
        }

        [Sample]
        private static void Pitch3() {
            var midiOut = new MidiOut(0);

            midiOut.Send(new NAudio.Midi.PatchChangeEvent(0, channel: 1, 6).GetAsShortMessage());

            // play 3 same tones: C4, D4(-2), A#3(+2)
            midiOut.Send(new PitchWheelChangeEvent(0, channel: 1, 0x2000).GetAsShortMessage());
            var e0 = new NoteOnEvent(0, channel: 1, noteNumber: 72, 0x40, 0); // C4
            midiOut.Send(e0.GetAsShortMessage());
            Thread.Sleep(1000);
            midiOut.Send(e0.OffEvent.GetAsShortMessage());
            //
            midiOut.Send(new PitchWheelChangeEvent(0, channel: 1, 0x0).GetAsShortMessage());
            var e1 = new NoteOnEvent(0, channel: 1, noteNumber: 74, 0x40, 0); // D4
            midiOut.Send(e1.GetAsShortMessage());
            Thread.Sleep(1000);
            midiOut.Send(e1.OffEvent.GetAsShortMessage());
            //
            midiOut.Send(new PitchWheelChangeEvent(0, channel: 1, 0x4000-1).GetAsShortMessage());
            var e2 = new NoteOnEvent(0, channel: 1, noteNumber: 70, 0x40, 0); // A#4
            midiOut.Send(e2.GetAsShortMessage());
            Thread.Sleep(1000);
            midiOut.Send(e2.OffEvent.GetAsShortMessage());

            // Now center the pitch bend again.
            midiOut.Send(new PitchWheelChangeEvent(0, channel: 1, 0x2000).GetAsShortMessage());

            midiOut.Dispose();
        }

        /*
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
        */
#endif

        [Sample]
        private static void Player1_NoClock() {
            // playing without clock

            var player = new Rationals.Midi.MidiPlayer(0);

            player.SetInstrument(0, 72-1); // Clarinet

            player.NoteOn(0, 1200f);
            Thread.Sleep(1000);
            player.NoteOff(0, 1200f);

            player.NoteOn(0, 1250f);
            Thread.Sleep(1000);
            player.NoteOff(0, 1250f);

            player.NoteOn(0, 1300f);
            Thread.Sleep(1000);
            player.NoteOff(0, 1300f);

            player.Dispose();
        }

        [Sample]
        private static void Player2_Duration() {
            // use clock for note duration

            var player = new MidiPlayer(0);
            player.StartClock(60 * 4);

            player.SetInstrument(0, 72-1); // Clarinet
            player.SetInstrument(1, 106-1); // Banjo

            player.NoteOn(0, 1200f, duration: 8f);

            Thread.Sleep(500);
            player.NoteOn(0, 1230f, duration: 8f);

            Thread.Sleep(500);
            player.NoteOn(1, 1260f, duration: 8f);

            Thread.Sleep(5000);

            player.StopClock();
            player.Dispose();
        }

        [Sample]
        private static void Player3_Delay() {
            // use clock to schedule

            var player = new MidiPlayer(0);

            player.SetInstrument(0, 43-1); // Cello
            player.SetInstrument(1, 0); // Piano

            player.ScheduleNote(1, 1200f, delay: 0f, duration: 4f);
            player.ScheduleNote(1, 1230f, delay: 1f, duration: 4f);
            player.ScheduleNote(0, 1260f, delay: 2f, duration: 4f);

            player.StartClock(60 * 2, waitForEnd: true);

            player.Dispose();
        }

        [Sample]
        private static void Player4_Delay()
        {
            var player = new MidiPlayer(0);

            //player.SetInstrument(0, 74-1); // Flute

            int n = 8; // steps in halftone
            for (int i = 0; i <= 12 * n; ++i) {
                player.ScheduleNote(
                    0, 
                    cents:    100f * i/n, 
                    velocity: i % n == 0 ? 0x67 : 0x5F,
                    delay:    i, 
                    duration: 1
                );
            }

            player.StartClock(60 * n, waitForEnd: true);

            player.Dispose();
        }
    }

    static class Program {
        static int Main() {
            var assembly = System.Reflection.Assembly.GetExecutingAssembly();
            try {
                bool result = Rationals.Testing.Utils.RunAssemblySamples(assembly);
                return result ? 0 : 1;
            } catch (System.Exception ex) {
                Console.Error.WriteLine(ex.GetType().FullName + " " + ex.Message);
                return -1;
            }
        }   
    }
}
