using System;
using System.Threading;
using System.Diagnostics;

namespace Rationals.Wave
{
    public struct WaveFormat
    {
        public int bytesPerSample;
        public bool floatSample; // ignored?
        public int sampleRate;
        public int channels;

        public override string ToString() {
            return String.Format("{0}x{1}x{2}", bytesPerSample * 8, sampleRate, channels);
        }

        public bool IsInitialized() { return bytesPerSample != 0; }

        public int MsToSamples(int ms) {
            return (int)(
                (Int64)sampleRate * ms / 1000
            );
        }

        #region Buffer utils
        public static void Clear(byte[] buffer) {
            Array.Fill<byte>(buffer, 0);
        }

        public void WriteFloat(byte[] buffer, int pos, float value) {
            WriteInt(buffer, pos, (int)(value * int.MaxValue));
        }

        public void WriteInt(byte[] buffer, int pos, int value) {
            // Like BinaryWriter.Write(int) https://github.com/microsoft/referencesource/blob/a7bd3242bd7732dec4aebb21fbc0f6de61c2545e/mscorlib/system/io/binarywriter.cs#L279
            // !!! Check byte order ?
            // !!! More variants: 
            //  "unsafe" https://stackoverflow.com/questions/1287143/simplest-way-to-copy-int-to-byte
            //  UnmanagedMemoryStream 
            switch (bytesPerSample) {
                case 1:
                    buffer[pos]     = (byte)((value >> 24) + 0x80); // wave 8-bit is unsigned!
                    break;
                case 2:
                    buffer[pos]     = (byte)(value >> 16);
                    buffer[pos + 1] = (byte)(value >> 24);
                    break;
                case 4:
                    buffer[pos]     = (byte)value;
                    buffer[pos + 1] = (byte)(value >> 8);
                    buffer[pos + 2] = (byte)(value >> 16);
                    buffer[pos + 3] = (byte)(value >> 24);
                    break;
            }
        }

#if DEBUG
        public string FormatBuffer(byte[] buffer) {
            string result = "";
            int sampleCount = buffer.Length / bytesPerSample;
            int chunkSize = sampleCount / 40; // divide to chunks
            using (var s = new System.IO.MemoryStream(buffer))
            using (var r = new System.IO.BinaryReader(s)) {
                int max = 0; // max value in chunk, 8 bit
                int i = 0; // sample index
                while (i < sampleCount) {
                    int v = 0; // value, 8 bit
                    switch (bytesPerSample) {
                        case 1: v = r.ReadByte() - 0x80; break; // wave 8-bit is unsigned! - make signed
                        case 2: v = r.ReadInt16() >>  8; break;
                        case 4: v = r.ReadInt32() >> 24; break;
                    }
                    if (max < v) max = v;
                    i += 1;
                    if ((i % chunkSize) == 0) {
                        result += max == 0 ? "." 
                            : (max >> 3).ToString("X"); // leave 4 bits of sbyte: sIIIIOOO
                        max = 0;
                    }
                }
            }
            return result;
        }
#endif
        #endregion
    }

    
    public abstract class SampleProvider
    {
        protected WaveFormat _format;

        public virtual void Initialize(WaveFormat format) {
            _format = format;
        }
        
        //public bool IsInitialized() { return _format.IsInitialized(); }

        public abstract bool Fill(byte[] buffer);
    }

}