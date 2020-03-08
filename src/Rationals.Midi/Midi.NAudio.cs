using System;
using System.Diagnostics;

using NAudio.Midi;

namespace Rationals.Midi
{
    public class NAudioMidiOut : IMidiOut
    {
        private MidiOut _device;

        public NAudioMidiOut(int deviceNo) {
            _device = new MidiOut(deviceNo);
        }
        public void Dispose() {
            _device.Dispose();
            _device = null;
        }

        private void Send(MidiEvent e) {
            _device.Send(e.GetAsShortMessage());
        }

        public void SendNoteOn(int channel, int noteNumber, int velocity) {
            Send(new NoteOnEvent(0, channel, noteNumber, velocity, 0));
        }
        public void SendNoteOff(int channel, int noteNumber, int velocity) {
            Send(new NoteEvent(0, channel, MidiCommandCode.NoteOff, noteNumber, velocity));
        }
        public void SendPatchChange(int channel, int patchNumber) {
            Send(new PatchChangeEvent(0, channel, patchNumber));
        }
        public void SendPitchWheelChange(int channel, int pitchWheel) {
            Send(new PitchWheelChangeEvent(0, channel, pitchWheel));
        }
    }
}
