using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Rationals.Wave
{
    using Partial = Generators.Partial;

    // Partial realtime provider - used for WaveEngine.
    // Allows to add partials/frequencies to currently played audio source.
    // Use:  AddPartial/AddFrequency, then FlushPartials to apply.

    public class PartialProvider : SampleProvider
    {
        protected struct Part {
            public int end;
            public ISampleValueProvider item;
            //
            public void SetTime(int currentSample) {
                unchecked {
                    end = currentSample + item.GetLength(); // may overflow ?
                }
            }
        }

        // Safe threads
        protected int _currentSample = 0; // atomic. MainThread, PlayingThread. ok if it overflows: it's like a phase
        protected Part[] _parts = new Part[0x200]; // PlayingThread
        protected int _partCount = 0;
        
        // Add partials - locking
        protected object _addPartsLock = new object();
        protected Part[] _addParts = new Part[0x100];
        protected volatile int _addPartsCount = 0;

        // Main thread only. FlushPartials to flush to _addPartials
        protected List<ISampleValueProvider> _preparedItems = new List<ISampleValueProvider>();

        protected bool _stopWhenEmpty = false;

        public PartialProvider(bool stopWhenEmpty = false) {
            _stopWhenEmpty = stopWhenEmpty;
        }

        public override string ToString() {
            return FormatStatus();
        }

        public string FormatStatus() {
            return String.Format("Partial count: {0}", _partCount);
        }

        public bool IsEmpty() {
            if (_partCount == 0) {
                if (_addPartsCount == 0) { // atomic
                    return true;
                }
            }
            return false;
        }

        public void AddItem(ISampleValueProvider item) {
            _preparedItems.Add(item);
        }
        public void AddItems(ISampleValueProvider[] items) {
            foreach (var item in items) {
                _preparedItems.Add(item);
            }
        }

        public void AddFrequency(double freqHz, int durationMs, float level) {
            AddItem(Generators.MakeFrequency(_format.sampleRate, freqHz, durationMs, level));
        }

        public void AddPartial(double freqHz, int attackMs, int releaseMs, float level, float curve = -4.0f) {
            AddItem(Generators.MakePartial(_format.sampleRate, freqHz, attackMs, releaseMs, level, curve));
        }

        public bool FlushItems() {
            // locking move _preparedPartials -> _addPartials
            // true if flushed fully
            if (_preparedItems.Count == 0) return true;
            int i = 0;
            lock (_addPartsLock) {
                while (i < _preparedItems.Count && _addPartsCount < _addParts.Length) {
                    _addParts[_addPartsCount++] = new Part {
                        end = 0, // reserved
                        item = _preparedItems[i++]
                    };
                }
            }
            _preparedItems.RemoveRange(0, i);
            return _preparedItems.Count == 0;
        }

        // implement SampleProvider
        // PlayingThread.
        public override bool Fill(byte[] buffer)
        {
            if (IsEmpty()) { // No partials to play
                WaveFormat.Clear(buffer);
                return !_stopWhenEmpty;
            }

            int bufferPos = 0; // in bytes
            int bufferSampleCount = buffer.Length / _format.bytesPerSample;

            for (int i = 0; i < bufferSampleCount / _format.channels; ++i)
            {
                // Add new partials: _addPartials -> _partials
                if ((_currentSample & 0xFFF) == 0) { // don't lock every sample
                    if (_addPartsCount > 0) { // atomic
                        lock (_addPartsLock) {
                            for (int j = 0; j < _addPartsCount; ++j) {
                                _addParts[j].SetTime(_currentSample);
                                //Debug.WriteLine("Partial added: {0}", _addPartials[j]);
                            }
                            Array.Copy(_addParts, 0, _parts, _partCount, _addPartsCount);
                            _partCount += _addPartsCount;
                            _addPartsCount = 0;
                        }
                        //Debug.WriteLine("Partials count: {0}", _partialCount);
                    }
                }

                int sampleValue = 0;

                if (_partCount > 0) {
                    int c = 0; // current partial index
                    for (int j = 0; j < _partCount; ++j) {
                        // Removing ended partials
                        if (_parts[j].end == _currentSample) {
                            //Debug.WriteLine("Partial removed: {0}", _partials[j]);
                        } else {
                            if (c != j) {
                                _parts[c] = _parts[j];
                            }
                            // Get sample value
                            sampleValue += _parts[c].item.GetNextValue();
                            c += 1;
                        }
                    }
                    if (_partCount != c) {
                        _partCount = c;
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
