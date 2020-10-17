#if USE_MANAGEDMIDI // ignore this module if no managed-midi reference

using System;
using System.Linq;
using System.Diagnostics;

using Commons.Music.Midi;

namespace Rationals.Midi
{
    public class ManagedMidiMidiOut : IMidiOut
    {
        IMidiOutput _device;
        byte[] buffer = new byte[0x100];

        public ManagedMidiMidiOut(int deviceIndex) {
            var api = MidiAccessManager.Default;
#if DEBUG
            int i = 0;
            foreach (var o in api.Outputs) {
                Console.WriteLine("{0,2}. {1}, version: {2}, id: {3}", i++, o.Name, o.Version, o.Id);
            }
#endif
            var output = api.Outputs.Skip(deviceIndex).FirstOrDefault();
            _device = api.OpenOutputAsync(output.Id).Result;
        }
        public void Dispose() {
            _device.Dispose();
            _device = null;
        }

        private void Send(MidiEvent m) {
            var size = MidiEvent.FixedDataSize(m.StatusByte);
            buffer[0] = m.StatusByte;
            buffer[1] = m.Msb;
            buffer[2] = m.Lsb;
            _device.Send(buffer, 0, size + 1, 0);
        }

        private static void SplitToLM(int word, out byte lsb, out byte msb) {
            // 00mmmmmmmlllllll -> 0lllllll, 0mmmmmmm
            lsb = (byte)( word       & 0x7F);
            msb = (byte)((word >> 7) & 0x7F);
        }

        public void SendNoteOn(int channel, int noteNumber, int velocity) {
            Send(new MidiEvent((byte)(MidiEvent.NoteOn + channel), (byte)noteNumber, (byte)velocity, null, 0, 0));
        }
        public void SendNoteOff(int channel, int noteNumber, int velocity) {
            Send(new MidiEvent((byte)(MidiEvent.NoteOff + channel), (byte)noteNumber, (byte)velocity, null, 0, 0));
        }
        public void SendPatchChange(int channel, int patchNumber) {
            Send(new MidiEvent((byte)(MidiEvent.Program + channel), (byte)patchNumber, (byte)0, null, 0, 0));
        }
        public void SendPitchWheelChange(int channel, int pitchWheel) {
            SplitToLM(pitchWheel, out byte lsb, out byte msb);
            Send(new MidiEvent((byte)(MidiEvent.Pitch + channel), lsb, msb, null, 0, 0));
        }
    }
}

#endif