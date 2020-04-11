using System;
using System.Threading;
using System.Diagnostics;

#if USE_SHARPAUDIO
using SA = SharpAudio;
#endif

namespace Rationals.Wave
{
    public class WaveEngineException : Exception {
        public WaveEngineException(string message) 
            : base(message) { }
        public WaveEngineException(string format, params object[] args) 
            : base(String.Format(format, args)) { }
        public WaveEngineException(Exception inner, string format, params object[] args)
            : base(String.Format(format, args), inner) { }
    }

    public struct WaveFormat {
        public int bitsPerSample;
        public bool floatSample; // ignored?
        public int sampleRate;
        public int channels;
        //
        public override string ToString() {
            return String.Format("{0}x{1}x{2}", bitsPerSample, sampleRate, channels);
        }
    }

    public abstract class SampleProvider
    {
        protected WaveFormat _format;
        protected int _sampleBytes = 0;
        protected int _bufferSampleCount = 0;

        public virtual void Initialize(WaveFormat format, int bufferSize) {
            _format = format;
            _sampleBytes = _format.bitsPerSample / 8;
            _bufferSampleCount = bufferSize / _sampleBytes;
        }
        protected bool IsInitialized() { return _sampleBytes != 0; }

        public abstract bool Fill(byte[] buffer);

        // Helpers
        protected static void Clear(byte[] buffer) {
            Array.Fill<byte>(buffer, 0);
        }

        protected void WriteFloat(byte[] buffer, int pos, float value) {
            WriteInt(buffer, pos, (int)(value * int.MaxValue));
        }

        protected void WriteInt(byte[] buffer, int pos, int value) {
            // Like BinaryWriter.Write(int) https://github.com/microsoft/referencesource/blob/a7bd3242bd7732dec4aebb21fbc0f6de61c2545e/mscorlib/system/io/binarywriter.cs#L279
            // !!! Check byte order ?
            // !!! More variants: 
            //  "unsafe" https://stackoverflow.com/questions/1287143/simplest-way-to-copy-int-to-byte
            //  UnmanagedMemoryStream 
            switch (_sampleBytes) {
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
        protected string FormatBuffer(byte[] buffer) {
            string result = "";
            int sampleCount = buffer.Length / _sampleBytes;
            int chunkSize = sampleCount / 40; // divide to chunks
            using (var s = new System.IO.MemoryStream(buffer))
            using (var r = new System.IO.BinaryReader(s)) {
                int max = 0; // max value in chunk, 8 bit
                int i = 0; // sample index
                while (i < sampleCount) {
                    int v = 0; // value, 8 bit
                    switch (_sampleBytes) {
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
    }

#if USE_SHARPAUDIO
    public class WaveEngine : IDisposable
    {
        protected WaveFormat _format;

        protected SampleProvider _sampleProvider = null;

        protected int _bufferLengthMs;
        protected int _bufferSize; // in bytes
        protected byte[] _buffer;

        protected SA.AudioFormat _audioFormat;
        protected SA.AudioEngine _engine;
        protected SA.AudioSource _source;

        protected const int _audioBufferCount = 3;
        protected SA.AudioBuffer[] _audioBuffers = new SA.AudioBuffer[_audioBufferCount];
        protected int _currentAudioBuffer = 0;

        protected Thread _playingThread;

        protected Stopwatch _watch = new Stopwatch();

        public WaveEngine() : this(DefaultFormat) { }

        public WaveEngine(int bufferLengthMs) : this(DefaultFormat, bufferLengthMs) { }

        public WaveEngine(WaveFormat format, int bufferLengthMs = 1000)
        {
            //!!! Check T and format consistency

            // Audio engine
            Exception ex = null;
            try {
                _engine = SA.AudioEngine.CreateOpenAL();
                //_engine = SA.AudioEngine.CreateXAudio();
            } catch (Exception e) {
                ex = e;
            }
            if (_engine == null) {
                throw new WaveEngineException(ex, "Can't initialize Audio engine");
            }

            // Audio source
            _source = _engine.CreateSource();

            //!!! move this out
            if (format.floatSample) {
                throw new WaveEngineException("Float wave format not yet supported by SharpAudio");
            }
            _format = format;
            _audioFormat = new SA.AudioFormat {
                BitsPerSample = format.bitsPerSample,
                SampleRate    = format.sampleRate,
                Channels      = format.channels,
            };

            // Buffer chain
            for (int i = 0; i < _audioBuffers.Length; ++i) {
                _audioBuffers[i] = _engine.CreateBuffer();
            }

            // Create sample buffer
            _bufferLengthMs = bufferLengthMs;
            _bufferSize = _format.bitsPerSample / 8 
                * _format.sampleRate 
                * _format.channels 
                * _bufferLengthMs / 1000;
            _bufferSize = _bufferSize & ~7;
            //!!! validate size here
            _buffer = new byte[_bufferSize];
            Debug.WriteLine("WaveEngine buffer length {0} ms; size {1} bytes; format {2}", 
                _bufferLengthMs, _bufferSize, _format);

            // Prepare playing thread
            _playingThread = new Thread(PlayingThread);
            _playingThread.Priority = ThreadPriority.Highest;
        }

        public static WaveFormat DefaultFormat = new WaveFormat {
            bitsPerSample = 16,
            floatSample   = false,
            sampleRate    = 44100,
            channels      = 1,
        };

        public void Dispose() {
            _source.Stop();
            if (_playingThread.IsAlive) {
                _playingThread.Join();
            }

            // Audio buffers are still queued in the source - so dispose them later.
            //   Otherwise we get error in AlNative.alDeleteSources.
            _source.Dispose();
            foreach (var b in _audioBuffers) b.Dispose();

            _engine.Dispose();
        }

        public void SetSampleProvider(SampleProvider p) {
            p.Initialize(_format, _bufferSize);
            _sampleProvider = p;
        }

        public void Play(bool waitForEnd = false) {
            if (_sampleProvider == null) throw new WaveEngineException("Sample provider not set");
            if (_source.IsPlaying()) return;

            // fill buffers to start immediatelly
            for (int i = 0; i < 2; ++i) {
                bool queued = ReadAndQueueBuffer();
                if (!queued) return;
            }

            _source.Play();

            _playingThread.Start();

            if (waitForEnd) {
                _playingThread.Join();
            }
        }

        public bool IsPlaying() {
            return _source.IsPlaying();
        }

        protected virtual bool ReadAndQueueBuffer() {
            // Initially accessed from Main thread, then from Playing thread

            //Debug.WriteLine("Get samples from provider");

            // Read samples from Provider
            _watch.Restart();
            bool filled = _sampleProvider.Fill(_buffer);
            _watch.Stop();
            if (!filled) return false; // provider failed or wants to stop the engine
            //Debug.WriteLine("Sample buffer filled in {0} ms. Audio buffer: {1}", _watch.ElapsedMilliseconds, _currentAudioBuffer);
            if (_watch.ElapsedMilliseconds >= _bufferLengthMs * 9/10) {
                Debug.WriteLine("Warning! Sample buffer filled in {0} ms", _watch.ElapsedMilliseconds);
            }

            // Get free audio buffer and copy data from sample buffer
            var audioBuffer = _audioBuffers[_currentAudioBuffer];
            audioBuffer.BufferData(_buffer, _audioFormat);
            // Queue audioBuffer to engine source4
            //Debug.WriteLine("Queue audio buffer");
            _source.QueueBuffer(audioBuffer);

            // Switch to next audio buffer
            _currentAudioBuffer += 1;
            _currentAudioBuffer %= _audioBufferCount;

            return true;
        }

        public void PlayingThread()
        {
            while (_source.IsPlaying())
            {
                int queuedCount = _source.BuffersQueued;

                if (queuedCount == 1) Debug.WriteLine("Source BuffersQueued LAST!");

                if (queuedCount < _audioBufferCount) {
                    bool queued = ReadAndQueueBuffer();
                    if (!queued) break;
                }

                //!!! what speed needed here ?
                //  10 for Debug ?
                Thread.Sleep(_bufferLengthMs / 10);
            }

            Debug.WriteLine("Source stopped -> ending PlayingThread");
        }

        public void Stop() {
            _source.Stop();
        }
    }

#endif // USE_SHARPAUDIO

}