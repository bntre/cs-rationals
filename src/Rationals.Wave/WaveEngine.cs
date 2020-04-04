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

    public abstract class SampleProvider<T>
        where T : unmanaged
    {
        protected WaveFormat _format;

        public virtual void Initialize(WaveFormat format) {
            _format = format;
        }

        public abstract bool Fill(T[] buffer);

        // Helpers
        protected void Clear(T[] buffer) {
            Array.Fill<T>(buffer, default(T));
        }
        public Func<float, T> FromFloat = null;
    }

    public static class Converters {
        public static float ToFloat(float v) { return v; }
        public static Int16 ToInt16(float v) { return (Int16)(Int16.MaxValue * v); }
        public static Int32 ToInt32(float v) { return (Int32)(Int32.MaxValue * v); }
    }

#if USE_SHARPAUDIO
    public class WaveEngine<T> : IDisposable
        where T : unmanaged
    {
        protected WaveFormat _waveFormat;

        protected SampleProvider<T> _sampleProvider = null;

        protected int _bufferLengthMs;
        protected int _bufferSampleCount;
        protected T[] _sampleBuffer;

        protected SA.AudioFormat _format;
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
            _bufferSampleCount = _waveFormat.sampleRate * _waveFormat.channels * _bufferLengthMs / 1000;
            _sampleBuffer = new T[_bufferSampleCount];

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

        public void SetSampleProvider(SampleProvider<T> p) {
            p.Initialize(_waveFormat);
            _sampleProvider = p;
        }

        public void Play(bool waitForEnd = false) {
            if (_sampleProvider == null) throw new WaveEngineException("Sample provider not set");
            if (_source.IsPlaying()) return;

            // fill a buffer to start immediatelly
            bool queued = ReadAndQueueBuffer();
            if (!queued) return;

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
            bool filled = _sampleProvider.Fill(_sampleBuffer);
            _watch.Stop();
            if (!filled) return false; // provider failed or wants to stop the engine
            Debug.WriteLine("Sample buffer filled in {0} ms. Audio buffer: {1}", _watch.ElapsedMilliseconds, _currentAudioBuffer);

            // Get free audio buffer and copy data from sample buffer
            var audioBuffer = _audioBuffers[_currentAudioBuffer];
            audioBuffer.BufferData(_sampleBuffer, _format);
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

            Debug.WriteLine("Source stopped -> PlayingThread finished");
        }

        public void Stop() {
            _source.Stop();
        }
    }

#endif // USE_SHARPAUDIO

}