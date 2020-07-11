using System;
using System.Threading;
using System.Diagnostics;

#if USE_SHARPAUDIO
using SA = SharpAudio;
#endif

// Realtime Engine for playing Wave Sound

namespace Rationals.Wave
{
    public abstract class SampleProvider
    {
        protected WaveFormat _format;

        public virtual void Initialize(WaveFormat format) {
            _format = format;
        }

        public WaveFormat GetFormat() { return _format; }

        public abstract bool Fill(byte[] buffer); // return false to stop (no bytes written)
    }

    public class BufferSampleProvider : SampleProvider
    {
        public BufferSampleProvider(byte[] fullDataBuffer) {
            _fullDataBuffer = fullDataBuffer;
        }

        protected byte[] _fullDataBuffer = null;
        protected int _currentByte = 0;

        public override bool Fill(byte[] buffer) {
            int left = _fullDataBuffer.Length - _currentByte;
            if (left == 0) return false; // no samples left to write

            int copy = Math.Min(left, buffer.Length);
            Array.Copy(_fullDataBuffer, _currentByte, buffer, 0, copy);

            _currentByte += copy;

            int clear = buffer.Length - copy;
            if (clear > 0) {
                Array.Clear(buffer, copy, clear);
            }

            return true;
        }
    }

    public class WaveEngineException : Exception {
        public WaveEngineException(string message) 
            : base(message) { }
        public WaveEngineException(string format, params object[] args) 
            : base(String.Format(format, args)) { }
        public WaveEngineException(Exception inner, string format, params object[] args)
            : base(String.Format(format, args), inner) { }
    }

#if USE_SHARPAUDIO
    public class WaveEngine : IDisposable
    {
        protected WaveFormat _format;

        protected SampleProvider _sampleProvider = null;

        protected int _bufferLengthMs;
        protected int _bufferSize; // in bytes
        protected byte[] _buffer;

        protected bool _restartOnFailure = false;

        protected SA.AudioFormat _audioFormat;
        protected SA.AudioEngine _engine;
        protected SA.AudioSource _source;

        protected const int _audioBufferCount = 3;
        protected SA.AudioBuffer[] _audioBuffers = new SA.AudioBuffer[_audioBufferCount];
        protected int _currentAudioBuffer = 0;

        protected bool _shouldPlay = false; // true is client requested to play and sound data is provided
        protected Thread _playingThread = null;

        protected Stopwatch _watch = new Stopwatch();

        public WaveEngine() : this(DefaultFormat) { }

        public WaveEngine(int bufferLengthMs) : this(DefaultFormat, bufferLengthMs) { }

        public WaveEngine(WaveFormat format, int bufferLengthMs = 1000, bool restartOnFailure = false)
        {
            _restartOnFailure = restartOnFailure;

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

            _format = format;
            _audioFormat = new SA.AudioFormat {
                BitsPerSample = format.bytesPerSample * 8,
                SampleRate    = format.sampleRate,
                Channels      = format.channels,
            };

            // Buffer chain
            for (int i = 0; i < _audioBuffers.Length; ++i) {
                _audioBuffers[i] = _engine.CreateBuffer();
            }

            // Create sample buffer
            _bufferLengthMs = bufferLengthMs;
            _bufferSize = _format.bytesPerSample
                        * _format.sampleRate 
                        * _format.channels 
                        * _bufferLengthMs / 1000;

            //!!! is it right validation?
            _bufferSize = _bufferSize & ~7;

            _buffer = new byte[_bufferSize];
            Debug.WriteLine("WaveEngine buffer length {0} ms; size {1} bytes; format {2}", 
                _bufferLengthMs, _bufferSize, _format);
        }

        public static WaveFormat DefaultFormat = new WaveFormat {
            bytesPerSample = 2,
            //floatSample    = false,
            sampleRate     = 44100,
            channels       = 1,
        };

        public void Dispose()
        {
            Stop();

            if (_playingThread != null) {
                if (_playingThread.IsAlive) {
                    _playingThread.Join();
                }
                _playingThread = null;
            }

            // Audio buffers are still queued in the source - so dispose them later.
            //   Otherwise we get error in AlNative.alDeleteSources.
            _source.Dispose();
            foreach (var b in _audioBuffers) b.Dispose();

            _engine.Dispose();
        }

        public void SetSampleProvider(SampleProvider p) {
            _sampleProvider = p;
            _sampleProvider.Initialize(_format);
        }

        public void Play(bool waitForEnd = false) {
            if (_sampleProvider == null) throw new WaveEngineException("Sample provider not set");
            if (_shouldPlay) return; // already requested to play

            if (!waitForEnd) {
                // finish previous thread if any
                if (_playingThread != null) {
                    if (_playingThread.IsAlive) {
                        _playingThread.Join();
                    }
                    _playingThread = null;
                }
            }

            // fill buffers to start immediatelly
            for (int i = 0; i < 2; ++i) {
                bool queued = ReadAndQueueBuffer();
                if (!queued) return;
            }

            if (waitForEnd) {
                _shouldPlay = true;
                _source.Play(); // start the source playing
                PlayingThread();
                _source.Stop(); // source may be not stopped yet if provider has not filled a buffer
                Debug.Assert(!_shouldPlay); // PlayingThread procedure waits for data end/failure
            }
            else {
                _shouldPlay = true;
                _source.Play(); // we have queued some buffers - so source can play before PlayingThread
                // create new thread
                _playingThread = new Thread(PlayingThread);
                _playingThread.Priority = ThreadPriority.Highest;
                _playingThread.Name = "WaveEngine Playing";
                _playingThread.Start();
            }
        }

        public bool IsPlaying() {
            return _shouldPlay;
        }

        public void Stop() {
            _shouldPlay = false;
            _source.Stop(); // this will also stop the PlayingThread
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
            SA.AudioBuffer audioBuffer = _audioBuffers[_currentAudioBuffer];
            audioBuffer.BufferData(_buffer, _audioFormat);
            // Queue audioBuffer to engine source
            //Debug.WriteLine("Queue audio buffer");
            _source.QueueBuffer(audioBuffer);

            // Switch to next audio buffer
            _currentAudioBuffer += 1;
            _currentAudioBuffer %= _audioBufferCount;

            return true;
        }

        protected void PlayingThread()
        {
            Debug.WriteLine("PlayingThread procedure start");

            while (true)
            {
                int queuedCount = _source.BuffersQueued; // this will "RemoveProcessed" buffers before we break the loop - so we may restart the source

                bool restart = false;
                if (!_source.IsPlaying()) {
                    if (!_shouldPlay) {
                        break; // client requested to stop
                    } else {
                        if (_restartOnFailure) {
                            // source has stopped: probably we are providing sound data slowly.
                            // we try to restart the source - having got a flick
                            Debug.WriteLine("Restart the Source");
                            restart = true;
                        } else {
                            Debug.WriteLine("Source failure but we don't restart");
                            _shouldPlay = false;
                            break;
                        }
                    }
                }

                bool panic = queuedCount <= 1;
                if (panic) {
                    Debug.WriteLine("Panic! Source BuffersQueued: {0}. Provider: {1}", queuedCount, _sampleProvider);
                }

                if (queuedCount < _audioBufferCount) {
                    bool queued = ReadAndQueueBuffer();
                    if (!queued) { // no data provided - stop the engine
                        _shouldPlay = false;
                        break;
                    }
                }

                if (restart) {
                    _source.Play(); // we try to restart the source
                    if (!_source.IsPlaying()) {
                        Debug.WriteLine("Can't restart the Source => finishing PlayingThread");
                        _shouldPlay = false;
                        break;
                    }
                }

                if (!restart && !panic) {
                    //!!! what speed needed here ?
                    //  10 for Debug ?
                    Thread.Sleep(_bufferLengthMs / 10);
                }
            }

            // wait the source to play all queued data
            Debug.Assert(!_shouldPlay);
            while (_source.IsPlaying()) {
                Thread.Sleep(_bufferLengthMs / 10);
            }

            Debug.WriteLine("PlayingThread procedure end");
        }
    }

    public static class Utils
    {
        public static void PlayBuffer(byte[] fullDataBuffer, WaveFormat format)
        {
#if DEBUG
            Debug.WriteLine("PlayBuffer of {0}:\n{1}", format, format.FormatBuffer(fullDataBuffer));
#endif

            var provider = new BufferSampleProvider(fullDataBuffer); // wrap the buffer for engine

            using (var engine = new WaveEngine(format)) {
                engine.SetSampleProvider(provider);
                
                engine.Play(waitForEnd: true);
                
                //engine.Play();
                //Thread.Sleep(5000);
            }
        }
    }

#endif // USE_SHARPAUDIO

}