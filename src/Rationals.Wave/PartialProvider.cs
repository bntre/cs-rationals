using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Rationals.Wave
{
    using Partial = Partials.Partial;

    // Partial realtime provider - used for WaveEngine

    public class PartialProvider : SampleProvider
    {
        protected struct Part {
            public int end;
            public Partial partial; // don't use ISignal here to avoid unboxing: we need performance
            //
            public void SetTime(int currentSample) {
                unchecked {
                    end = currentSample + partial.GetLength(); // may overflow ?
                }
            }
        }

        protected Partials _factory = null;

        // Safe threads
        protected int _currentSample = 0; // atomic. MainThread, PlayingThread. ok if it overflows: it's like a phase
        protected Part[] _partials = new Part[0x200]; // PlayingThread
        protected int _partialCount = 0;
        
        // Add partials - locking
        protected object _addPartialsLock = new object();
        protected Part[] _addPartials = new Part[0x100];
        protected int _addPartialsCount = 0;

        // Main thread only. FlushPartials to flush to _addPartials
        protected List<Partial> _preparedPartials = new List<Partial>();

        public PartialProvider() {
        }

        public override void Initialize(WaveFormat format/*, int bufferSize*/) {
            base.Initialize(format);
            _factory = new Partials(format);
        }

        public override string ToString() {
            return FormatStatus();
        }

        public string FormatStatus() {
            return String.Format("Partial count: {0}", _partialCount);
        }

        protected int LevelToInt(float level) {
            return (int)(level * int.MaxValue);
        }

        public bool IsEmpty() {
            if (_partialCount == 0) {
                if (_addPartialsCount == 0) { // atomic
                    return true;
                }
            }
            return false;
        }

        public void AddFrequency(double freqHz, int durationMs, float level) {
            Partial p = _factory.MakeFrequency(freqHz, durationMs, level);
            _preparedPartials.Add(p);
        }

        public void AddPartial(double freqHz, int attackMs, int releaseMs, float level, float curve = -4.0f) {
            Partial p = _factory.MakePartial(freqHz, attackMs, releaseMs, level, curve);
            _preparedPartials.Add(p);
        }

        public bool FlushPartials() {
            // locking move _preparedPartials -> _addPartials
            // true if flushed fully
            if (_preparedPartials.Count == 0) return true;
            int i = 0;
            lock (_addPartialsLock) {
                while (i < _preparedPartials.Count && _addPartialsCount < _addPartials.Length) {
                    _addPartials[_addPartialsCount++] = new Part {
                        end = 0, // reserved
                        partial = _preparedPartials[i++]
                    };
                }
            }
            _preparedPartials.RemoveRange(0, i);
            return _preparedPartials.Count == 0;
        }

        // implement SampleProvider
        // PlayingThread.
        public override bool Fill(byte[] buffer)
        {
            if (IsEmpty()) { // No partials to play
                WaveFormat.Clear(buffer);
                return true;
            }

            int bufferPos = 0; // in bytes
            int bufferSampleCount = buffer.Length / _format.bytesPerSample;

            for (int i = 0; i < bufferSampleCount / _format.channels; ++i)
            {
                // Add new partials: _addPartials -> _partials
                if ((_currentSample & 0xFFF) == 0) { // don't lock every sample
                    if (_addPartialsCount > 0) { // atomic
                        lock (_addPartialsLock) {
                            for (int j = 0; j < _addPartialsCount; ++j) {
                                _addPartials[j].SetTime(_currentSample);
                                //Debug.WriteLine("Partial added: {0}", _addPartials[j]);
                            }
                            Array.Copy(_addPartials, 0, _partials, _partialCount, _addPartialsCount);
                            _partialCount += _addPartialsCount;
                            _addPartialsCount = 0;
                        }
                        //Debug.WriteLine("Partials count: {0}", _partialCount);
                    }
                }

                int sampleValue = 0;

                if (_partialCount > 0) {
                    int c = 0; // current partial index
                    for (int j = 0; j < _partialCount; ++j) {
                        // Removing ended partials
                        if (_partials[j].end == _currentSample) {
                            //Debug.WriteLine("Partial removed: {0}", _partials[j]);
                        } else {
                            if (c != j) {
                                _partials[c] = _partials[j];
                            }
                            // Get sample value
                            sampleValue += _partials[c].partial.GetNextValue();
                            c += 1;
                        }
                    }
                    if (_partialCount != c) {
                        _partialCount = c;
                        //Debug.WriteLine("Partials count: {0}", _partialCount);
                    }
                }

                unchecked {
                    _currentSample += 1; // overflowing
                }

                // Write sample value to all channels
                for (int c = 0; c < _format.channels; ++c) {
                    _format.WriteInt(buffer, bufferPos, sampleValue);
                    bufferPos += _format.bytesPerSample;
                }

            }

            //Debug.WriteLine(FormatBuffer(buffer));

            return true;
        }

    }
}
