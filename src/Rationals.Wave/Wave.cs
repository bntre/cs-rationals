//#define USE_NAUDIO
#define USE_SHARPAUDIO

using System;
using System.Threading;
using System.Diagnostics;

#if USE_NAUDIO
using NA = NAudio.Wave;
//using NAudio.Wave.SampleProviders;
#endif

#if USE_SHARPAUDIO
using SA = SharpAudio;
#endif

namespace Rationals.Wave
{
#if USE_NAUDIO
    public class SampleProvider : NA.ISampleProvider
    {
        private const double TwoPi = 2 * Math.PI;
        private int _nSample;

        public SampleProvider() : this(44100, 2) {}
        public SampleProvider(int sampleRate, int channel) {
            WaveFormat = NA.WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, channel);
            // Default
            Frequency = 440.0;
            Gain = 1;
        }

        public double Frequency { get; set; }
        public double Gain { get; set; }

        // ISampleProvider
        public NA.WaveFormat WaveFormat { get; }
        public int Read(float[] buffer, int offset, int count)
        {
            int outIndex = offset;

            // Generator current value
            double sampleValue;

            // Complete Buffer
            for (int i = 0; i < count / WaveFormat.Channels; ++i)
            {
                sampleValue = Gain * Math.Sin(TwoPi * Frequency * _nSample / WaveFormat.SampleRate);

                Frequency += 0.001;
                _nSample++;

                for (int c = 0; c < WaveFormat.Channels; ++c) {
                    buffer[outIndex++] = (float)sampleValue;
                }
            }

            return count;
        }

    }
#endif


#if USE_SHARPAUDIO

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

        internal static WaveFormat FromAudioFormat(SA.AudioFormat f) {
            return new WaveFormat {
                bitsPerSample = f.BitsPerSample,
                floatSample   = false, //!!! not yet supported by SharpAudio
                sampleRate    = f.SampleRate,
                channels      = f.Channels,
            };
        }
    }

    #region SampleBuffer-s
    public abstract class SampleBuffer {
        protected int _sampleCount = 0; // available lenght of buffer
        protected int _sampleIndex = 0; // writing position
        public SampleBuffer(int sampleCount) { _sampleCount = sampleCount; }
        public int GetSampleCount() { return _sampleCount; }
        public void Reset() { _sampleIndex = 0; } // reset writing position
        public abstract void Write(float value);

        internal abstract void BufferData(SA.AudioBuffer audioBuffer, SA.AudioFormat format);
    }

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

    public abstract class SampleProvider {
        protected WaveFormat _format;

        // All methods called from using engine
        virtual public void Initialize(WaveFormat format) {
            _format = format;
        }
        public abstract bool Fill(SampleBuffer buffer);
    }

    public class WaveEngine : IDisposable
    {
        protected SA.AudioFormat _format;
        protected SA.AudioEngine _engine;
        protected SA.AudioSource _source;

        protected const int _audioBufferCount = 3;
        protected SA.AudioBuffer[] _audioBuffers = new SA.AudioBuffer[_audioBufferCount];
        protected int _currentAudioBuffer = 0;
        protected Thread _playingThread;

        protected WaveFormat _waveFormat;
        protected SampleBuffer _sampleBuffer = null;
        protected SampleProvider _sampleProvider = null;


        public WaveEngine() : this(_defaultFormat) { }

        public WaveEngine(WaveFormat format)
        {
            // Audio engine
            Exception ex = null;
            try {
                _engine = SA.AudioEngine.CreateOpenAL();
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

            // Create sample buffer - for 1 second
            _sampleBuffer = CreateSampleBuffer(_waveFormat, _waveFormat.sampleRate);

            // Prepare playing thread
            _playingThread = new Thread(PlayingThread);

        }

        private static WaveFormat _defaultFormat = new WaveFormat {
            bitsPerSample = 16,
            floatSample   = false,
            sampleRate    = 44100,
            channels      = 2,
        };

        public void Dispose() {
            _source.Stop();
            _playingThread.Join();

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

            // fill a buffer to start immediatelly
            bool queued = ReadAndQueueBuffer();
            if (!queued) return; // buffers not ready

            _source.Play();

            _playingThread.Start();

            if (waitForEnd) {
                _playingThread.Join();
            }
        }

        private bool ReadAndQueueBuffer() { 
            // Initially accessed from Main thread, then from Playing thread

            //Debug.WriteLine("Get samples from provider");

            // Read samples from Provider
            _sampleBuffer.Reset();
            bool filled = _sampleProvider.Fill(_sampleBuffer);
            if (!filled) return false; // provider failed or wants to stop the engine

            //Debug.WriteLine("Fill audio buffer {0}", _currentAudioBuffer);

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

        public void PlayingThread() {
            while (_source.IsPlaying()) {
                if (_source.BuffersQueued < 3)
                {
                    bool queued = ReadAndQueueBuffer();
                    if (!queued) break;
                }

                Thread.Sleep(100);
            }
        }

        public void Stop() {
            _source.Stop();
        }
    }


#endif
}