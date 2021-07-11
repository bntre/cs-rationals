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
                (double k) => (int)(level * 
                    Math.Sin(2 * Math.PI * k)
                    //Math.Cos(2 * Math.PI * k) // Cosine is easier to debug: starts from 1.0
                )
            );
        }

        public static int[] MakeConstantPowerPanTable(int width, int level) {
            // imitate http://www.cs.cmu.edu/~music/icm-online/readings/panlaws/
            return MakeTable(
                width,
                (double k) => (int)(level * 
                    (2.0 * (1.0 - k) * k * 0.9142f + k * k) // quadratic Bezier curve: 0 - (/2-.5) - 1
                )
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
                int key = (int)(curve * 100); //!!! do we need better precision?
                int[] table;
                if (!_tables.TryGetValue(key, out table)) {
                    table = MakeCurveTable(Width, Level, curve);
                    _tables[key] = table;
                }
                return table;
            }
        }

    }


    // Like SuperCollider Env https://doc.sccode.org/Classes/Env.html
    public class Envelope
    {
        protected struct Part {
            public int start;
            public int length;
            public int levelStart;
            public int levelChange;
            public int[] table;
        }

        protected Part[] _parts = null;

        protected int _length = 0; // in samples

        protected int _currentPart    = 0;
        protected int _currentPartPos = 0;

        public Envelope(int[] levels, int[] lengths, float curve)
        {
            Debug.Assert(levels.Length == lengths.Length + 1, "Envelope part lengths don't match");

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

            _length = pos;
        }

        // Like SuperCollider Env.perc
        public Envelope(int attack, int release, int level, float curve = -4f)
            : this(
                new int[] { 0, level, 0 },
                new int[] { attack, release },
                curve
            )
        { }

        public int GetLength() {
            return _length;
        }

        public int GetNextValue() {
            if (_currentPart == _parts.Length) return 0; // no parts left

            Part p = _parts[_currentPart];

            int value = p.levelStart;

            if (p.levelChange != 0) {
                int tableIndex = (int)(((Int64)_currentPartPos << IntegerTables.CurveTables.WidthBits) / p.length);

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
    public static class Partials
    {
        // Partial
        //           [sine table phase][precision]
        //           (        full phase         )

        private const int _precisionBits = 12;                   // precision bits shifted out before getting sine value
        private const int _precision = 1 << _precisionBits;
        private const int _sineWidthBits = 12;
        private const int _sineWidth = 1 << _sineWidthBits;  // width for sine table
        //private const int _sineWidthMask = 1 << _sineWidthBits;
        private const int _phaseMask = (1 << (_sineWidthBits + _precisionBits)) - 1; // 0xFF..FF mask for full phase values

        private const int _sineLevelBits = 20;
        private const int _sineLevel = 1 << _sineLevelBits;    // max value of sine table

        private static int[] _sineTable = IntegerTables.MakeSineWaveTable(_sineWidth, _sineLevel);

        private const int _panLevelBits = 12;
        private const int _panLevel = 1 << _panLevelBits;     // max value of pan table
        private const int _panWidth = 1 << 16;

        private static int[] _panTable = IntegerTables.MakeConstantPowerPanTable(_panWidth, _panLevel);

        public struct Partial { // !!! struct/class switching gives no performance change
            public Envelope envelope;
            public int phase; // current phase
            public int phaseStep; // per sample step. ~ freq

            public int GetNextValue()
            {
                phase += phaseStep; // change phase
                phase &= _phaseMask;

                int value = envelope.GetNextValue();
                
                if (value == 0) { // no signal amplitude
                    return 0;
                }

                /* comment out to check envelope only
                */
                int sine = _sineTable[phase >> _precisionBits];
                value = (int)(
                    ((Int64)value * sine) >> _sineLevelBits
                );

                return value;
            }

            public void GetNextStereoValue(int balance16, out int value0, out int value1)
            {
                phase += phaseStep; // change phase
                phase &= _phaseMask;

                Int64 value = envelope.GetNextValue();
                
                if (value == 0) { // no signal amplitude
                    value0 = 0;
                    value1 = 0;
                    return;
                }

                int pan0 = _panTable[_panWidth - 1 - balance16];
                int pan1 = _panTable[                balance16];

                /* comment out to check envelope only
                */
                int sine = _sineTable[phase >> _precisionBits];
                value *= sine;

                value0 = (int)( (value * pan0) >> (_sineLevelBits + _panLevelBits) );
                value1 = (int)( (value * pan1) >> (_sineLevelBits + _panLevelBits) );
            }

            public override string ToString() {
                return String.Format("Partial step {0} env {1}", phaseStep, envelope.ToString());
            }

            public static int MakeBalance16(float balance) { // -1..1 -> 0..FFFF
                return (int)((balance + 1f) / 2 * 0xFFFF); 
            }
        }

        #region Helpers
        private static int LevelToInt(float level) {
            return (int)(level * int.MaxValue);
        }

        private static int HzToSampleStep(double hz, int sampleRate) {
            return (int)(hz * _sineWidth * _precision / sampleRate);
        }
        private static double SampleStepToHz(int phaseStep, int sampleRate) {
            return (double)(((Int64)phaseStep * sampleRate) >> _precisionBits) / _sineWidth;
        }

        public static int MsToSamples(int ms, int sampleRate) { // !!! move out ?
            return (int)(
                (Int64)sampleRate * ms / 1000
            );
        }

        //!!! "cents" stuff might be moved out
        public static double CentsToFactor(double cents) {
            return Math.Pow(2.0, cents / 1200.0);
        }
        public static double CentsToHz(double cents) {
            // Like in Rationals.Midi.MidiPlayer (Midi.cs):
            //    0.0 -> C4 (261.626 Hz)
            // 1200.0 -> C5
            return 261.626 * CentsToFactor(cents);
        }
        public static double HzToCents(double hz) {
            return Math.Log(hz / 261.626, 2.0) * 1200.0;
        }
        #endregion

        public static Partial MakeFrequency(int sampleRate, double freqHz, int durationMs, float level) {
            //Debug.Assert(_format.IsInitialized(), "WaveFormat must be initialized");
            int levelInt = LevelToInt(level);
            Envelope env = new Envelope(
                new[] { levelInt, levelInt },
                new[] { MsToSamples(durationMs, sampleRate) },
                0
            );
            Partial p = new Partial {
                envelope = env,
                phase = 0,
                phaseStep = HzToSampleStep(freqHz, sampleRate),
            };

#if DEBUG
            double freqHz1 = SampleStepToHz(p.phaseStep, sampleRate);
            double centsDiff = HzToCents(freqHz) - HzToCents(freqHz1);
            Debug.WriteLine("MakeFrequency {0} hz -> {1} step samples ({2} hz)", freqHz, p.phaseStep, freqHz1);
#endif

            return p;
        }

        public static Partial MakePartial(int sampleRate, double freqHz, int attackMs, int releaseMs, float level, float curve = -4.0f) {
            //Debug.Assert(_format.IsInitialized(), "WaveFormat must be initialized");
            Envelope env = new Envelope(
                MsToSamples(attackMs, sampleRate),
                MsToSamples(releaseMs, sampleRate),
                LevelToInt(level),
                curve
            );
            Partial p = new Partial {
                envelope = env,
                phase = 0,
                phaseStep = HzToSampleStep(freqHz, sampleRate),
            };
            return p;
        }

    }
}
