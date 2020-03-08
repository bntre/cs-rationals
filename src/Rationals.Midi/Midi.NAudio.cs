using System;
using System.Diagnostics;


namespace Rationals.Midi
{
    using NM = NAudio.Midi;

    public class NAudioMidiOut : IMidiOut
    {
        private NM.MidiOut _device;

        public NAudioMidiOut(int deviceNo) {
            _device = new NM.MidiOut(deviceNo);
        }
        public void Dispose() {
            _device.Dispose();
        }

        public void Send(int message) {
            _device.Send(message);
        }

        public int MakeNoteOn(int channel, int noteNumber, int velocity) {
            var e = new NM.NoteOnEvent(0, channel, noteNumber, velocity, 0);
            return e.GetAsShortMessage();
        }
        public int MakeNoteOff(int channel, int noteNumber, int velocity) {
            var e = new NM.NoteEvent(0, channel, NM.MidiCommandCode.NoteOff, noteNumber, velocity);
            return e.GetAsShortMessage();
        }
        public int MakePatchChange(int channel, int patchNumber) {
            var e = new NM.PatchChangeEvent(0, channel, patchNumber);
            return e.GetAsShortMessage();
        }
        public int MakePitchWheelChange(int channel, int pitchWheel) {
            var e = new NM.PitchWheelChangeEvent(0, channel, pitchWheel);
            return e.GetAsShortMessage();
        }
    }
}
