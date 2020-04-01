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
        public bool floatSample;
        public int sampleRate;
        public int channels;
    }

    public abstract partial class SampleBuffer {
        protected int _sampleCount = 0; // available lenght of buffer
        protected int _sampleIndex = 0; // writing position
        public SampleBuffer(int sampleCount) { _sampleCount = sampleCount; }
        public int GetLength() { return _sampleCount; }
        public bool IsFull() { return _sampleIndex == _sampleCount; }
        public void Reset() { _sampleIndex = 0; } // reset writing position
        public void Clear() {
            while (!IsFull()) { //!!! do some memory fill
                Write(0.0f);
            }
        }

        public abstract void Write(float value);
    }

    public abstract class SampleProvider {
        protected WaveFormat _format;

        // All methods called from using engine
        virtual public void Initialize(WaveFormat format) {
            _format = format;
        }

        public abstract bool Fill(SampleBuffer buffer); // accessed from PlayingThread
    }

#if USE_SHARPAUDIO

    public abstract partial class SampleBuffer {
        internal abstract void BufferData(SA.AudioBuffer audioBuffer, SA.AudioFormat format);
    }
    #region SampleBuffer-s
    public class FloatSampleBuffer : SampleBuffer {
        private float[] _samples;
        public FloatSampleBuffer(int sampleCount) : base(sampleCount) {
            _samples = new float[sampleCount];
        }
        public override void Write(float value) {
            _samples[_sampleIndex++] = value;
        }
        internal override void BufferData(SA.AudioBuffer audioBuffer, SA.AudioFormat format) {
            audioBuffer.BufferData<float>(_samples, format);
        }
    }
    public class Int16SampleBuffer : SampleBuffer {
        private Int16[] _samples;
        public Int16SampleBuffer(int sampleCount) : base(sampleCount) {
            _samples = new Int16[sampleCount];
        }
        public override void Write(float value) {
            _samples[_sampleIndex++] = (Int16)(Int16.MaxValue * value);
        }
        internal override void BufferData(SA.AudioBuffer audioBuffer, SA.AudioFormat format) {
            audioBuffer.BufferData<Int16>(_samples, format);
        }
    }
    public class Int8SampleBuffer : SampleBuffer {
        private sbyte[] _samples;
        public Int8SampleBuffer(int sampleCount): base(sampleCount)  {
            _samples = new sbyte[sampleCount];
        }
        public override void Write(float value) {
            _samples[_sampleIndex++] = (sbyte)(sbyte.MaxValue * value);
        }
        internal override void BufferData(SA.AudioBuffer audioBuffer, SA.AudioFormat format) {
            audioBuffer.BufferData<sbyte>(_samples, format);
        }
    }
    #endregion

    public class WaveEngine : IDisposable
    {
        protected SA.AudioFormat _format;
        protected SA.AudioEngine _engine;
        protected SA.AudioSource _source;

        protected int _bufferLengthMs;

        protected const int _audioBufferCount = 3;
        protected SA.AudioBuffer[] _audioBuffers = new SA.AudioBuffer[_audioBufferCount];
        protected int _currentAudioBuffer = 0;
        protected Thread _playingThread;

        protected WaveFormat _waveFormat;
        protected SampleBuffer _sampleBuffer = null;
        protected SampleProvider _sampleProvider = null;

        public WaveEngine() : this(DefaultFormat) { }

        public WaveEngine(int bufferLengthMs) : this(DefaultFormat, bufferLengthMs) { }

        public WaveEngine(WaveFormat format, int bufferLengthMs = 1000)
        {
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
            _waveFormat = format;
            _format = new SA.AudioFormat {
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
            int bufferSampleCount = _waveFormat.sampleRate * _waveFormat.channels * _bufferLengthMs / 1000;
            _sampleBuffer = CreateSampleBuffer(_waveFormat, bufferSampleCount);

            // Prepare playing thread
            _playingThread = new Thread(PlayingThread);

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

        private static SampleBuffer CreateSampleBuffer(WaveFormat waveFormat, int sampleCount) {
            if (waveFormat.bitsPerSample == 8) {
                return new Int8SampleBuffer(sampleCount);
            } else if (waveFormat.bitsPerSample == 16) {
                return new Int16SampleBuffer(sampleCount);
            } else {
                throw new WaveEngineException("Can't create sample buffer");
            }
        }

        public void SetSampleProvider(SampleProvider p) {
            p.Initialize(_waveFormat);
            _sampleProvider = p;
        }

        public void Play(bool waitForEnd = false) {
            if (_sampleProvider == null) throw new WaveEngineException("Sample provider not set");
            if (_source.IsPlaying()) return;

            // fill all buffers to start immediatelly
            for (int i = 0; i < _audioBufferCount; ++i) {
                bool queued = ReadAndQueueBuffer(true);
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

        private Stopwatch _watch = new Stopwatch();

        private bool ReadAndQueueBuffer(bool read) { 
            // Initially accessed from Main thread, then from Playing thread

            //Debug.WriteLine("Get samples from provider");

            if (read) {
                // Read samples from Provider
                _sampleBuffer.Reset();
                _watch.Restart();
                bool filled = _sampleProvider.Fill(_sampleBuffer);
                _watch.Stop();
                if (!filled) return false; // provider failed or wants to stop the engine
                Debug.WriteLine("Sample buffer filled (samples filled: {1} ms; audio buffer: {0})", _currentAudioBuffer, _watch.ElapsedMilliseconds);
            } else {
                // We are in panic - just clear the buffer
                _sampleBuffer.Reset();
                _sampleBuffer.Clear();
                Debug.WriteLine("Sample buffer cleared (audio buffer: {0})", _currentAudioBuffer);
            }

            // Get free audio buffer
            var audioBuffer = _audioBuffers[_currentAudioBuffer];
            _currentAudioBuffer += 1;
            _currentAudioBuffer %= _audioBufferCount;
            // Copy data from sample buffer to free audioBuffer
            _sampleBuffer.BufferData(audioBuffer, _format);

            //Debug.WriteLine("Queue audio buffer");

            // Queue audioBuffer to engine source
            _source.QueueBuffer(audioBuffer);

            return true;
        }

        public void PlayingThread()
        {
            while (_source.IsPlaying())
            {
                int queuedCount = _source.BuffersQueued;

                Debug.WriteLine("Source BuffersQueued {0}", queuedCount);

                if (queuedCount < _audioBufferCount)
                {
                    bool panic = queuedCount == 1;
                    // Last buffer queued left - provider is slow - just clear buffer, avoid engine stopping
                    bool queued = ReadAndQueueBuffer(read: !panic);
                    if (!queued) break;
                }

                //!!! what speed needed here ?
                //  10 for Debug ?
                Thread.Sleep(_bufferLengthMs / 10);
            }

            Debug.WriteLine("Source stopped -> PlayingThread finished");
        }

        public void Stop() {
            _source.Stop();
        }
    }

#endif // USE_SHARPAUDIO

}