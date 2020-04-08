using System;
using System.Collections.Generic;
using System.Diagnostics;

//using Rationals;


namespace Rationals.Wave
{
    public static class IntegerTables
    {
        public static int[] MakeTable(int width, Func<double, int> func) { // func (0..1) -> int
            int[] table = new int[width];
            for (int i = 0; i < width; ++i) {
                table[i] = func((double)i / width);
            }
            return table;
        }

        public static int[] MakeSineWaveTable(int width, int level) {
            return MakeTable(
                width,
                (double k) => (int)(level * Math.Sin(2 * Math.PI * k))
            );
        }

        // Like SuperCollider curve value
        public static int[] MakeCurveTable(int width, int level, double curve) {
            double e = Math.Pow(2.0, curve);
            return MakeTable(
                width,
                (double k) => (int)(Math.Pow(k, e) * level)
            );
        }

        public static class CurveTables {
            public const int WidthBits = 10;
            public const int Width     = 1 << WidthBits;
            public const int LevelBits = 12;
            public const int Level     = 1 << LevelBits; // Don't choose too large to avoid overflow on: level0 + (levelD * table[j]) >> LevelBits;
            //
            private static Dictionary<int, int[]> _tables = new Dictionary<int, int[]>();
            public static int[] Get(double curve) {
                int key = (int)(curve * 100); //!!! do we need better precision
                int[] table;
                if (!_tables.TryGetValue(key, out table)) {
                    table = MakeCurveTable(Width, Level, curve);
                    _tables[key] = table;
                }
                return table;
            }
        }

    }

    public abstract class Signal {
        // All values in samples
        public int Length = 0;
        
        protected int _currentPos = 0;
        public abstract int GetNextValue();

        //public virtual void ResetPos() { _currentPos = 0; }
    }

    // Like SuperCollider Env https://doc.sccode.org/Classes/Env.html
    public class Envelope : Signal
    {
        protected struct Part {
            public int start;
            public int length;
            public int levelStart;
            public int levelChange;
            public int[] table;
        }

        protected Part[] _parts = null;

        protected int _currentPart    = 0;
        protected int _currentPartPos = 0;

        public Envelope(int[] levels, int[] lengths, float curve)
        {
            Debug.Assert(levels.Length == lengths.Length + 1, "Envelope lengths don't match");

            int partCount = lengths.Length;
            _parts = new Part[partCount];
            int pos = 0;
            for (int i = 0; i < partCount; ++i) {
                Part p = new Part();
                p.start = pos;
                p.length = lengths[i];
                p.levelStart  = levels[i];
                p.levelChange = levels[i+1] - levels[i];
                p.table = IntegerTables.CurveTables.Get(curve);
                _parts[i] = p;
                pos += p.length;
            }

            Length = pos;

            //ResetPos();
        }

        // Like SuperCollider Env.perc
        public Envelope(int attack, int release, int level, float curve = -4)
            : this (
                new int[] { 0, level, 0 },
                new int[] { attack, release },
                curve
            )
        { }

        public override int GetNextValue() {
            if (_currentPart == _parts.Length) return 0; // no parts left

            Part p = _parts[_currentPart];

            int tableIndex = (_currentPartPos << IntegerTables.CurveTables.WidthBits) / p.length;
            //!!! may overflow!

            int levelChange = (p.levelChange * p.table[tableIndex]) >> IntegerTables.CurveTables.LevelBits;
            int value = p.levelStart + levelChange;

            // Step to next sample
            //!!! bug: if there is an empty part
            _currentPartPos += 1;
            if (_currentPartPos == p.length) {
                _currentPartPos = 0;
                _currentPart += 1;
            }

            return value;
        }

#if DEBUG
        public override string ToString() {
            int maxLevel = 0;
            foreach (var p in _parts) {
                if (maxLevel < p.levelStart) {
                    maxLevel = p.levelStart;
                }
            }
            return maxLevel.ToString();
        }
#endif
    }
}


namespace Rationals.Wave
{
    using Value = Int16;

    public class PartialProvider : SampleProvider<Value>
    {
        // Partial
        //           [sine table phase][precision]
        //           (        full phase         )

        protected const int _precisionBits = 12;                   // precision bits shifted out before getting sine value
        protected const int _precision     = 1 << _precisionBits;
        protected const int _sineWidthBits = 12;
        protected const int _sineWidth     = 1 << _sineWidthBits;  // width for sine table
        protected const int _sineWidthMask = 1 << _sineWidthBits;
        protected const int _phaseMask     = (1 << (_sineWidthBits + _precisionBits)) - 1; // 0xFF..FF mask for full phase values

        protected const int _sineLevelBits = 15;
        protected const int _sineLevel     = 1 << _sineLevelBits;    // max value of sine table

        protected static int[] _sineTable = IntegerTables.MakeSineWaveTable(_sineWidth, _sineLevel);

        protected struct Partial { // !!! struct/class switching gives no performance change
            public Envelope env;
            public int phase;
            public int phaseStep; // per sample. freq
            public int sampleEnded; // to compare with _currentSample !!! max length ?
            //
            public void SetEnd(int currentSample) {
                unchecked {
                    sampleEnded = currentSample + env.Length; // overflowing
                }
            }
            public int GetNextValue() {
                int value = env.GetNextValue();

                phase += phaseStep; // change phase
                phase &= _phaseMask;

                int sine = _sineTable[phase >> _precisionBits];
                value = (value * sine) >> _sineLevelBits;

                return value;
            }

            public override string ToString() { 
                return String.Format("Partial step {0} env {1}", phaseStep, env.ToString());
            }
        }

        // Safe threads
        protected int _currentSample = 0; // atomic. MainThread, PlayingThread. ok if it overflows: it's like a phase
        protected object _partialsLock = new object();
        protected Partial[] _partials = new Partial[0x200]; // PlayingThread
        protected int _partialCount = 0;
        protected Partial[] _partialsToAdd = new Partial[0x100]; // locking
        protected int _partialToAddCount = 0;

        public PartialProvider() {
        }

        public string FormatStatus() {
            return String.Format("Partial count: {0}", _partialCount);
        }

        protected int MsToSamples(int ms) {
            return _format.sampleRate * ms / 1000;
        }
        protected int LevelToValue(float level) {
            return (int)(level * Value.MaxValue);
        }
        protected int FreqToSampleStep(double freq) {
            return (int)(freq * _sineWidth * _precision / _format.sampleRate);
        }

        public bool AddPartial(double freqHz, int attackMs, int releaseMs, float level, float curve = -4.0f) {
            Envelope env = new Envelope(
                MsToSamples(attackMs),
                MsToSamples(releaseMs),
                LevelToValue(level),
                curve
            );
            Partial p = new Partial {
                env = env,
                phase = 0,
                phaseStep = FreqToSampleStep(freqHz),
            };
            lock (_partialsLock) {
                if (_partialToAddCount < _partialsToAdd.Length) {
                    _partialsToAdd[_partialToAddCount++] = p;
                    return true;
                }
            }
            return false;
        }

#if DEBUG
        protected static string FormatBuffer(Int16[] buffer) {
            int n = buffer.Length / 20;
            int i = n;
            Int16 max = 0;
            string s = "";
            foreach (Int16 b in buffer) {
                if (max < b) max = b;
                if (--i == 0) {
                    s += (max >> (16-4)).ToString("X"); // leave 4 bits
                    i = n;
                }
            }
            return s;
        }
#endif

        // implement SampleProvider
        // PlayingThread.
        public override bool Fill(Value[] buffer) {
            if (_partialCount == 0) {
                if (_partialToAddCount == 0) { // atomic
                    Clear(buffer);
                    return true;
                }
            }

            int bufferLength = buffer.Length;
            int bufferPos    = 0;

            for (int i = 0; i < bufferLength / _format.channels; ++i)
            {
                // Add new partials
                if ((_currentSample & 0xFFF) == 0) { // don't lock every sample
                    lock (_partialsLock) {
                        if (_partialToAddCount > 0) {
                            for (int j = 0; j < _partialToAddCount; ++j) {
                                _partialsToAdd[j].SetEnd(_currentSample);
                                //Debug.WriteLine("Partial added: {0}", _partialsToAdd[j]);
                            }
                            Array.Copy(_partialsToAdd, 0, _partials, _partialCount, _partialToAddCount);
                            _partialCount += _partialToAddCount;
                            _partialToAddCount = 0;
                            //Debug.WriteLine("Partials count: {0}", _partialCount);
                        }
                    }
                }

                int sampleValue = 0;

                if (_partialCount > 0) {
                    int current = 0;
                    for (int j = 0; j < _partialCount; ++j) {
                        // Removing ended partials
                        if (_partials[j].sampleEnded == _currentSample) {
                            //Debug.WriteLine("Partial removed: {0}", _partials[j]);
                        } else {
                            if (current != j) {
                                _partials[current] = _partials[j];
                            }
                            // Get sample value
                            sampleValue += _partials[current].GetNextValue();
                            current += 1;
                        }
                    }
                    if (_partialCount != current) {
                        _partialCount = current;
                        //Debug.WriteLine("Partials count: {0}", _partialCount);
                    }
                }

                unchecked {
                    _currentSample += 1; // overflowing
                }

                // Write sample value to all channels
                for (int c = 0; c < _format.channels; ++c) {
                    buffer[bufferPos++] = (Value)sampleValue;
                }

            }

            //Debug.WriteLine(FormatBuffer(buffer));

            return true;

        }

    }
}