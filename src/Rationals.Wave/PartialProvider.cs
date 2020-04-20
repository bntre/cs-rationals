using System;
using System.Collections.Generic;
using System.Diagnostics;


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
                (double k) => 
                    (int)(level * Math.Sin(2 * Math.PI * k))
                    //(int)(level * Math.Cos(2 * Math.PI * k)) // Cosine is easier to debug: starts from 1.0
            );
        }

        // Smt like SuperCollider curve value.
        //  https://doc.sccode.org/Classes/Env.html
        //  curve: 0 means linear, positive and negative numbers curve the segment up and down.
        // pictures: http://www.musicaecodice.it/SC_Env/SC_Env.php
        public static int[] MakeCurveTable(int width, int level, double curve) {
            Func<double, int> func = null;
            if (curve > 0) {            //      __/     ‾‾\
                func = (double k) => {
                    double a = Math.Pow(2.0, curve); // a > 1: 0 -> 1, 1 -> 2
                    double x = k;
                    double y = Math.Pow(a, x);
                    double res = (y - 1) / (a - 1);
                    return (int)(level * res);
                };
            }
            else if (curve < 0) {       //      /‾‾     \__
                func = (double k) => {
                    double a = Math.Pow(2.0, -curve); // a > 1
                    double x = 1 + k * (a - 1);
                    double y = Math.Log(x, a);
                    double res = y;
                    return (int)(level * res);
                };
            }
            else {                      //       /       \
                func = (double k) => 
                    (int)(level * k);
            }
            return MakeTable(width, func);
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
        public Envelope(int attack, int release, int level, float curve = -4f)
            : this(
                new int[] { 0, level, 0 },
                new int[] { attack, release },
                curve
            )
        { }

        public override int GetNextValue() {
            if (_currentPart == _parts.Length) return 0; // no parts left

            Part p = _parts[_currentPart];

            int value = p.levelStart;

            if (p.levelChange != 0) {
                int tableIndex = (_currentPartPos << IntegerTables.CurveTables.WidthBits) / p.length;

                value += (int)(
                    ((Int64)p.levelChange * p.table[tableIndex]) >> IntegerTables.CurveTables.LevelBits
                );
            }

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
    public class PartialProvider : SampleProvider
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

        protected const int _sineLevelBits = 20;
        protected const int _sineLevel     = 1 << _sineLevelBits;    // max value of sine table

        protected static int[] _sineTable = IntegerTables.MakeSineWaveTable(_sineWidth, _sineLevel);

        protected struct Partial { // !!! struct/class switching gives no performance change
            public Envelope env;
            public int phase;
            public int phaseStep; // per sample. freq
            public int sampleEnded; // to compare with _currentSample !!! max length ?
            //
            public void SetTime(int currentSample) {
                unchecked {
                    sampleEnded = currentSample + env.Length; // overflowing
                }
            }
            public int GetNextValue()
            {
                phase += phaseStep; // change phase
                phase &= _phaseMask;

                int value = env.GetNextValue();
                if (value == 0) return 0;

                /* comment out to check envelope
                */
                int sine = _sineTable[phase >> _precisionBits];
                value = (int)(
                    ((Int64)value * sine) >> _sineLevelBits
                );

                return value;
            }

            public override string ToString() { 
                return String.Format("Partial step {0} env {1}", phaseStep, env.ToString());
            }
        }

        // Safe threads
        protected int _currentSample = 0; // atomic. MainThread, PlayingThread. ok if it overflows: it's like a phase
        protected Partial[] _partials = new Partial[0x200]; // PlayingThread
        protected int _partialCount = 0;
        // Add partials - locking
        protected object _addPartialsLock = new object();
        protected Partial[] _addPartials = new Partial[0x100];
        protected int _addPartialsCount = 0;

        protected List<Partial> _preparedPartials = new List<Partial>(); // main thread only - FlushPartials should be added

        public PartialProvider() {
        }

        public override string ToString() {
            return FormatStatus();
        }

        public string FormatStatus() {
            return String.Format("Partial count: {0}", _partialCount);
        }

        protected int MsToSamples(int ms) {
            return (int)(
                (Int64)_format.sampleRate * ms / 1000
            );
        }
        protected int LevelToInt(float level) {
            return (int)(level * int.MaxValue);
        }
        protected int HzToSampleStep(double hz) {
            return (int)(hz * _sineWidth * _precision / _format.sampleRate);
        }

        public static double CentsToHz(double cents) {
            // Like in Rationals.Midi.MidiPlayer (Midi.cs):
            //    0.0 -> C4 (261.626 Hz)
            // 1200.0 -> C5
            return 261.626 * Math.Pow(2.0, cents / 1200.0);
        }

        public void AddFrequency(double freqHz, int durationMs, float level) {
            Debug.Assert(IsInitialized(), "Provider must be initialized");
            int levelInt = LevelToInt(level);
            Envelope env = new Envelope(
                new[] { levelInt, levelInt },
                new[] { MsToSamples(durationMs) },
                0
            );
            Partial p = new Partial {
                env = env,
                phase = 0,
                phaseStep = HzToSampleStep(freqHz),
            };
            _preparedPartials.Add(p);
        }

        public void AddPartial(double freqHz, int attackMs, int releaseMs, float level, float curve = -4.0f) {
            Debug.Assert(IsInitialized(), "Provider must be initialized");
            Envelope env = new Envelope(
                MsToSamples(attackMs),
                MsToSamples(releaseMs),
                LevelToInt(level),
                curve
            );
            Partial p = new Partial {
                env = env,
                phase = 0,
                phaseStep = HzToSampleStep(freqHz),
            };
            _preparedPartials.Add(p);
        }

        public bool IsEmpty() {
            if (_partialCount == 0) {
                if (_addPartialsCount == 0) { // atomic
                    return true;
                }
            }
            return false;
        }

        public bool FlushPartials() { 
            // locking move _preparedPartials -> _partialToAddCount
            // true if flushed fully
            if (_preparedPartials.Count == 0) return true;
            int i = 0;
            lock (_addPartialsLock) {
                while (i < _preparedPartials.Count && _addPartialsCount < _addPartials.Length) {
                    _addPartials[_addPartialsCount++] = _preparedPartials[i++];
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
                Clear(buffer);
                return true;
            }

            int bufferPos = 0;

            for (int i = 0; i < _bufferSampleCount / _format.channels; ++i)
            {
                // Add new partials
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
                        if (_partials[j].sampleEnded == _currentSample) {
                            //Debug.WriteLine("Partial removed: {0}", _partials[j]);
                        } else {
                            if (c != j) {
                                _partials[c] = _partials[j];
                            }
                            // Get sample value
                            sampleValue += _partials[c].GetNextValue();
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
                    WriteInt(buffer, bufferPos, sampleValue);
                    bufferPos += _sampleBytes;
                }

            }

            //Debug.WriteLine(FormatBuffer(buffer));

            return true;
        }

    }
}
