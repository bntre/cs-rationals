using System;
using System.Collections.Generic;
using System.Threading;
using System.Diagnostics;
using Rationals.Testing;

namespace Rationals.Midi
{
    [Test]
    public static class MidiSamples
    {
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
