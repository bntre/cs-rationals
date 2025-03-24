using System;
using System.Collections.Generic;
using System.Diagnostics;


namespace Rationals.Wave
{
    using Int    = System.Int32;
    using IntX   = System.Int64;

    public static class Const {
        public const int IntBits = 32;
    }

    public static class IntegerTables
    {
        public static Int[] MakeTable(int width, Func<double, Int> func) { // func [0..1) -> Int
            Int[] table = new Int[width];
            for (int i = 0; i < width; ++i) {
                table[i] = func((double)i / width);
            }
            return table;
        }

        public static Int[] MakeSineWaveTable(int width, Int level) {
            return MakeTable(
                width,
                (double k) => (Int)(level * 
                    Math.Sin(2 * Math.PI * k)
                    //Math.Cos(2 * Math.PI * k) // Cosine is easier to debug: starts from 1.0
                )
            );
        }

        public static Int[] MakeTriangleWaveTable(int width, Int level) {
            return MakeTable(
                width,
                (double k) => (Int)(level *
                    (Math.Abs(k - 0.5) - 0.25) * 4
                )
            );
        }

        public static Int[] MakeConstantPowerPanTable(int width, Int level) {
            // imitate http://www.cs.cmu.edu/~music/icm-online/readings/panlaws/
            return MakeTable(
                width,
                (double k) => (Int)(level * 
                    (2.0 * (1.0 - k) * k * 0.9142f + k * k) // quadratic Bezier curve: 0 - (/2-.5) - 1
                )
            );
        }

        // Smt like SuperCollider curve value.
        //  https://doc.sccode.org/Classes/Env.html
        //  curve: 0 means linear, positive and negative numbers curve the segment up and down.
        // pictures: http://www.musicaecodice.it/SC_Env/SC_Env.php
        public static Int[] MakeCurveTable(int width, Int level, double curve) {
            Func<double, Int> func = null;
            if (curve > 0) {            //      __/     ‾‾\
                func = (double k) => {
                    double a = Math.Pow(2.0, curve); // a > 1: 0 -> 1, 1 -> 2
                    double x = k;
                    double y = Math.Pow(a, x);
                    double res = (y - 1) / (a - 1);
                    return (Int)(level * res);
                };
            }
            else if (curve < 0) {       //      /‾‾     \__
                func = (double k) => {
                    double a = Math.Pow(2.0, -curve); // a > 1
                    double x = 1 + k * (a - 1);
                    double y = Math.Log(x, a);
                    double res = y;
                    return (Int)(level * res);
                };
            }
            else {                      //       /       \
                func = (double k) => 
                    (Int)(level * k);
            }
            return MakeTable(width, func);
        }

        public static class CurveTables {
            public const int WidthBits = 10;
            public const int Width     = 1 << WidthBits;
            public const int LevelBits = 12;
            public const Int Level     = 1 << LevelBits; // Don't choose too large to avoid overflow on: level0 + (levelD * table[j]) >> LevelBits;
            //
            private static Dictionary<int, Int[]> _tables = new Dictionary<int, Int[]>();
            public static Int[] Get(double curve) {
                int key = (int)(curve * 100); //!!! do we need better precision?
                Int[] table;
                if (!_tables.TryGetValue(key, out table)) {
                    table = MakeCurveTable(Width, Level, curve);
                    _tables[key] = table;
                }
                return table;
            }
        }

    }


    // Like SuperCollider Env https://doc.sccode.org/Classes/Env.html
    public class Curve
    {
        protected struct Part {
            public int start;
            public int length;
            public Int levelStart;
            public Int levelChange;
            public Int[] table;
        }

        protected Part[] _parts = null;

        protected int _length = 0; // in samples

        protected int _currentPart    = 0;
        protected int _currentPartPos = 0;

        public Curve(Int[] levels, int[] lengths, float curve = 0)
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

        public int GetLength() {
            return _length;
        }

        public Int GetNextValue() {
            if (_currentPart == _parts.Length) return 0; // no parts left

            Part p = _parts[_currentPart];

            Int value = p.levelStart;

            if (p.levelChange != 0) {
                int tableIndex = (int)(((IntX)_currentPartPos << IntegerTables.CurveTables.WidthBits) / p.length);

                value += (Int)(
                    ((IntX)p.levelChange * p.table[tableIndex]) >> IntegerTables.CurveTables.LevelBits
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
            Int maxLevel = 0;
            foreach (var p in _parts) {
                if (maxLevel < p.levelStart) {
                    maxLevel = p.levelStart;
                }
            }
            return String.Format("Curve max level %d", maxLevel);
        }
#endif
    }


    public interface ISampleValueProvider {
        public int GetLength(); // length in samples
        public Int GetNextValue();
        public void GetNextStereoValue(int balance16, out Int value0, out Int value1);
    }

    public static class Generators
    {
        // Generator of a partial
        //           [sine table phase][precision]
        //           (        full phase         )

        private const int _precisionBits = 12;                   // precision bits shifted out before getting sine value
        private const int _precision = 1 << _precisionBits;
        private const int _sineWidthBits = 12;
        private const int _sineWidth = 1 << _sineWidthBits;  // width for sine table
        //private const int _sineWidthMask = 1 << _sineWidthBits;
        private const int _phaseMask = (1 << (_sineWidthBits + _precisionBits)) - 1; // 0xFF..FF mask for full phase values

        private const int _sineLevelBits = 20;
        private const Int _sineLevel = 1 << _sineLevelBits;    // max value of sine table

        private static Int[] _sineTable = IntegerTables.MakeSineWaveTable(_sineWidth, _sineLevel);

        // 
        private static Int[] _triangleTable = IntegerTables.MakeTriangleWaveTable(_sineWidth, _sineLevel);  // Triangle wave https://en.wikipedia.org/wiki/Triangle_wave

        // Pan (Balance)
        private const int _panLevelBits = 12;
        private const Int _panLevel = 1 << _panLevelBits;     // max value of pan table
        private const int _panWidth = 1 << 16;

        private static Int[] _panTable = IntegerTables.MakeConstantPowerPanTable(_panWidth, _panLevel);

        // Noise
        private static Random _random = new Random();

        //!!! rename to Oscillator ? might be also triangle etc
        public class Partial : ISampleValueProvider {
            public Curve envelope; // mandatory
            public int phase = 0; // current phase
            public int phaseStep = 0; // per sample step. ~ freq
            public Curve phaseStepCurve = null; // may be used instead of constant phaseStep; otherwise null
            public bool isTriangle = false; //!!! optimize with Int[] pointer ?
            public Int clipValue = 0; //!!! clips upper side only, use for distortion ?

            public int GetLength() {
                return envelope.GetLength();
            }

            // ISampleValueProvider
            public Int GetNextValue()
            {
                // change phase
                phase += phaseStepCurve != null ?
                    phaseStepCurve.GetNextValue() :
                    phaseStep;
                phase &= _phaseMask;

                Int value = envelope.GetNextValue();
                
                if (value == 0) { // no signal amplitude
                    return 0;
                }

#if true  // exclude the block to check envelope only
                Int sine = (isTriangle ? _triangleTable : _sineTable)[phase >> _precisionBits];
                //!!! this cast will not work for x64 (64 bit int). use Int32 instead?
                value = (Int)(
                    ((IntX)value * sine) >> _sineLevelBits
                );

                if (clipValue != 0 && value > clipValue) { // clip upper side for distortion
                    value = clipValue;
                }
#endif
                return value;
            }

            public void GetNextStereoValue(int balance16, out Int value0, out Int value1)
            {
                // change phase
                phase += phaseStepCurve != null ?
                    phaseStepCurve.GetNextValue() :
                    phaseStep;
                phase &= _phaseMask;

                IntX value = (IntX)envelope.GetNextValue();
                
                if (value == 0) { // no signal amplitude
                    value0 = 0;
                    value1 = 0;
                    return;
                }

                Int pan0 = _panTable[_panWidth - 1 - balance16];
                Int pan1 = _panTable[                balance16];

#if true  // exclude to check envelope only
                Int sine = (isTriangle ? _triangleTable : _sineTable)[phase >> _precisionBits];
                value *= sine;

                if (clipValue != 0 && value > clipValue) { // clip upper side for distortion
                    value = clipValue;
                }
#endif
                value0 = (Int)( (value * pan0) >> (_sineLevelBits + _panLevelBits) );
                value1 = (Int)( (value * pan1) >> (_sineLevelBits + _panLevelBits) );
            }

            public override string ToString() {
                return String.Format("Partial step {0} env {1}", phaseStep, envelope.ToString());
            }
        }

        #region Helpers
        public static Int LevelToInt(float level) {
            return (Int)(level * Int.MaxValue); // [0..1] -> [0..MaxValue]
        }
        public static Int[] LevelsToInt(float[] levels) {
            var res = new Int[levels.Length];
            for (int i = 0; i < levels.Length; ++i) {
                res[i] = LevelToInt(levels[i]);
            }
            return res;
        }
        public static int HzToSampleStep(double hz, int sampleRate) {
            return (int)(hz * _sineWidth * _precision / sampleRate);
        }
        public static int[] HzToSampleStep(double[] hz, int sampleRate) {
            var res = new Int[hz.Length];
            for (int i = 0; i < hz.Length; ++i) {
                res[i] = HzToSampleStep(hz[i], sampleRate);
            }
            return res;
        }

        private static double SampleStepToHz(Int32 phaseStep, int sampleRate) {
            return (double)(((Int64)phaseStep * sampleRate) >> _precisionBits) / _sineWidth;
        }

        public static int MsToSamples(int ms, int sampleRate) { // !!! move out ?
            return (int)(sampleRate * ms / 1000);
        }
        public static int[] MsToSamples(int[] ms, int sampleRate) {
            var res = new Int[ms.Length];
            for (int i = 0; i < ms.Length; ++i) {
                res[i] = MsToSamples(ms[i], sampleRate);
            }
            return res;
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

        public static int MakeBalance16(float balance) { // [-1..1] -> [0..FFFF]
            return (int)((balance + 1f) / 2 * 0xFFFF); 
        }
        #endregion

        public static Curve MakeCurve(int sampleRate, float[] levels, int[] durationsMs, float curve = 0f) {
            return new Curve(
                LevelsToInt(levels),
                MsToSamples(durationsMs, sampleRate), 
                curve
            );
        }

        public static Curve MakePitchCurve(int sampleRate, double[] hz, int[] durationsMs, float curve = 0f) {
            return new Curve(
                HzToSampleStep(hz, sampleRate),
                MsToSamples(durationsMs, sampleRate), 
                curve
            );
        }

        public static Partial MakeFrequency(int sampleRate, double freqHz, int durationMs, float level) {
            //Debug.Assert(_format.IsInitialized(), "WaveFormat must be initialized");
            Int levelInt = LevelToInt(level);
            Curve env = new Curve(
                new[] { levelInt, levelInt },
                new[] { MsToSamples(durationMs, sampleRate) },
                0
            );
            Partial p = new Partial {
                envelope = env,
                phaseStep = HzToSampleStep(freqHz, sampleRate),
            };

#if DEBUG
            double freqHz1 = SampleStepToHz(p.phaseStep, sampleRate);
            double centsDiff = HzToCents(freqHz) - HzToCents(freqHz1);
            Debug.WriteLine("MakeFrequency {0} hz -> {1} step samples ({2} hz)", freqHz, p.phaseStep, freqHz1);
#endif

            return p;
        }

        // Like SuperCollider Env.perc
        public static Curve MakeEnvelope(int sampleRate, int attackMs, int releaseMs, float level, float curve = -4.0f) {
            int a = MsToSamples(attackMs, sampleRate);
            int r = MsToSamples(releaseMs, sampleRate);
            Int l = LevelToInt(level);
            return new Curve(
                new Int[] { 0, l, 0 },
                new int[] { a, r },
                curve
            );
        }

        public static Partial MakePartial(int sampleRate, double freqHz, int attackMs, int releaseMs, float level, float curve = -4.0f) {
            //Debug.Assert(_format.IsInitialized(), "WaveFormat must be initialized");
            Curve env = MakeEnvelope(sampleRate, attackMs, releaseMs, level, curve);
            Partial p = new Partial {
                envelope = env,
                phaseStep = HzToSampleStep(freqHz, sampleRate),
            };
            return p;
        }

        public class Noise : ISampleValueProvider {
            public Curve envelope;

            public enum Type { 
                White = 0,
                Violet = 1, // https://en.wikipedia.org/wiki/Colors_of_noise#Violet_noise
            }
            public Type type = Type.White;

            private Int _prevRandValue = 0; // used for Violet noise

            public int GetLength() {
                return envelope.GetLength();
            }

            public Int GetNextValue()
            {
                Int value = envelope.GetNextValue();

                if (value == 0) { // no signal amplitude
                    return 0;
                }

#if true  // exclude the block to check envelope only
                Int rand = _random.Next(Int.MinValue, Int.MaxValue);
                if (type == Type.Violet) {
                    IntX diff = (IntX)rand - _prevRandValue; // Violet noise is differentiated white noise
                    _prevRandValue = rand;
                    rand = (Int)(diff >> 1);
                }
                value = (Int)(
                    ((Int64)value * rand) >> Const.IntBits
                );
#endif
                return value;
            }

            public void GetNextStereoValue(int balance16, out Int value0, out Int value1)
            {
                Int64 value = (Int64)envelope.GetNextValue();

                if (value == 0) { // no signal amplitude
                    value0 = 0;
                    value1 = 0;
                    return;
                }

                int pan0 = _panTable[_panWidth - 1 - balance16];
                int pan1 = _panTable[balance16];

#if true  // exclude the block to check envelope only
                Int rand = _random.Next(Int.MinValue, Int.MaxValue);
                if (type == Type.Violet) {
                    IntX diff = (IntX)rand - _prevRandValue; // Violet noise is differentiated white noise
                    _prevRandValue = rand;
                    rand = (Int)(diff >> 1);
                }
                value = (value * rand) >> Const.IntBits;
#endif

                value0 = (Int)((value * pan0) >> _panLevelBits);
                value1 = (Int)((value * pan1) >> _panLevelBits);
            }

            public override string ToString() {
                return String.Format("Noise type {0} env {1}", type.ToString(), envelope.ToString());
            }
        }

        public static Noise MakeNoise(int sampleRate, Generators.Noise.Type type, int attackMs, int releaseMs, float level, float curve = -4.0f) {
            //Debug.Assert(_format.IsInitialized(), "WaveFormat must be initialized");
            Curve env = MakeEnvelope(sampleRate, attackMs, releaseMs, level, curve);
            Noise n = new Noise {
                envelope = env,
                type = type,
            };
            return n;
        }

    }
}
