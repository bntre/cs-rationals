using System;
using System.IO;
using System.Text;
using System.Diagnostics;


namespace Rationals.Wave
{
    public class WaveWriter : IDisposable {

        protected WaveFormat _format;

        protected FileStream _stream = null;
        protected BinaryWriter _writer = null;

        protected int _dataSize = 0;

        public WaveWriter(WaveFormat format, string file) {
            _format = format;
            _stream = new FileStream(file, FileMode.Create);
            _writer = new BinaryWriter(_stream);

            WriteHeader();
        }

        protected static byte[] ToBytes(string s) {
            return Encoding.ASCII.GetBytes(s);
        }  

        protected void WriteHeader() {
            Debug.Assert(_writer != null, "No writer initialized");

            // Following http://soundfile.sapp.org/doc/WaveFormat/

            // RIFF header
            _writer.Write(ToBytes("RIFF"));                 // ChunkID
            _writer.Write((UInt32)36);      // reserved     // ChunkSize: 36 + SubChunk2Size
            _writer.Write(ToBytes("WAVE"));                 // Format

            // "fmt " SubChunk
            _writer.Write(ToBytes("fmt "));                 // Subchunk1ID
            _writer.Write((UInt32)16);                      // Subchunk1Size: 16 for PCM
            _writer.Write((UInt16)1);                       // AudioFormat: PCM = 1
            _writer.Write((UInt16)_format.channels);        // NumChannels
            _writer.Write((UInt32)_format.sampleRate);      // SampleRate
            int byteRate = _format.sampleRate * _format.channels * _format.bitsPerSample / 8;
            _writer.Write((UInt32)byteRate);                // ByteRate: SampleRate * NumChannels * BitsPerSample/8
            int blockAlign = _format.channels * _format.bitsPerSample / 8;
            _writer.Write((UInt16)blockAlign);              // BlockAlign: NumChannels * BitsPerSample/8
            _writer.Write((UInt16)_format.bitsPerSample);   // BitsPerSample

            // "data" SubChunk
            _writer.Write(ToBytes("data"));                 // Subchunk2ID
            _writer.Write((UInt32)0);       // reserved     // Subchunk2Size: NumSamples * NumChannels * BitsPerSample/8
        }

        public void Write(byte[] data) {
            Debug.Assert(_writer != null, "No writer initialized");
            _writer.Write(data);
            _dataSize += data.Length;
        }

        public void Finalize() {
            if (_dataSize == 0) return; // nothing to finalize

            Debug.Assert(_stream != null, "No file opened");
            Debug.Assert(_writer != null, "I want the same writer");

            // Finalize: write data size to the Header
            _stream.Position = 4; // ChunkSize
            _writer.Write((UInt32)(36 + _dataSize));
            _stream.Position = 40; // Subchunk2Size
            _writer.Write((UInt32)_dataSize);

            _dataSize = 0;

            // Close at once
            _writer.Close(); _writer = null;
            _stream.Close(); _stream = null;
        }

        public void Dispose() {
            if (_dataSize != 0) { // forgot to finalize ?
                Finalize();
            }
            if (_writer != null) { _writer.Dispose(); _writer = null; }
            if (_stream != null) { _stream.Dispose(); _stream = null; }
        }

    }
}

