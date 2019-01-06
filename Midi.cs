using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

// Midi.dll - https://github.com/micdah/midi-dot-net
using Midi.Devices;
using Midi.Enums;
using Midi.Instruments;
using Midi.Messages;

namespace Midi {

    public static class Utils {

        public static void Test1() {
            var outputDevice = DeviceManager.OutputDevices.FirstOrDefault();
            if (outputDevice == null) return;

            outputDevice.Open();

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
                outputDevice.SendPitchBend(Channel.Channel1, 0x2000 - 0x2000 * i/steps);
                Thread.Sleep(5000 / steps);
            }

            // Now release the C chord notes
            outputDevice.SendNoteOff(Channel.Channel1, Pitch.C4, 80);
            outputDevice.SendNoteOff(Channel.Channel1, Pitch.E4, 80);
            outputDevice.SendNoteOff(Channel.Channel1, Pitch.G4, 80);

            // Now center the pitch bend again.
            outputDevice.SendPitchBend(Channel.Channel1, 0x2000);

            // Close the output device.
            outputDevice.Close();
        }

    }
}
