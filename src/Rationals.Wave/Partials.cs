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

        // ISignal -- actually we don't need this interface here
        public int GetNextValue() {
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
    public class Partials
    {
        protected WaveFormat _format;

        // Partial
        //           [sine table phase][precision]
        //           (        full phase         )

        protected const int _precisionBits = 12;                   // precision bits shifted out before getting sine value
        protected const int _precision = 1 << _precisionBits;
        protected const int _sineWidthBits = 12;
        protected const int _sineWidth = 1 << _sineWidthBits;  // width for sine table
        protected const int _sineWidthMask = 1 << _sineWidthBits;
        protected const int _phaseMask = (1 << (_sineWidthBits + _precisionBits)) - 1; // 0xFF..FF mask for full phase values

        protected const int _sineLevelBits = 20;
        protected const int _sineLevel = 1 << _sineLevelBits;    // max value of sine table

        protected static int[] _sineTable = IntegerTables.MakeSineWaveTable(_sineWidth, _sineLevel);

        public struct Partial { // !!! struct/class switching gives no performance change
            public Envelope env;
            public int phase;
            public int phaseStep; // per sample. freq

            public int GetLength() {
                return env.GetLength();
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

        public Partials(WaveFormat format) {
            _format = format;
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

        public Partial MakeFrequency(double freqHz, int durationMs, float level) {
            Debug.Assert(_format.IsInitialized(), "WaveFormat must be initialized");
            int levelInt = LevelToInt(level);
            Envelope env = new Envelope(
                new[] { levelInt, levelInt },
                new[] { _format.MsToSamples(durationMs) },
                0
            );
            Partial p = new Partial {
                env = env,
                phase = 0,
                phaseStep = HzToSampleStep(freqHz),
            };
            return p;
        }

        public Partial MakePartial(double freqHz, int attackMs, int releaseMs, float level, float curve = -4.0f) {
            Debug.Assert(_format.IsInitialized(), "WaveFormat must be initialized");
            Envelope env = new Envelope(
                _format.MsToSamples(attackMs),
                _format.MsToSamples(releaseMs),
                LevelToInt(level),
                curve
            );
            Partial p = new Partial {
                env = env,
                phase = 0,
                phaseStep = HzToSampleStep(freqHz),
            };
            return p;
        }

    }
}


namespace Rationals.Wave
{
    using Partial = Partials.Partial;

    // Partial provider - used for WaveEncoder

    public class PartialTimeline : SampleProvider
    {
        // Timeline parts
        public struct Part {
            public int startSample;
            public int endSample;
            public Partial partial;

            public static int CompareStart(Part a, Part b) { return a.startSample.CompareTo(b.startSample); }
        }

        protected List<Part> _parts = new List<Part>();

        // Timeline position
        protected int _currentSample = 0;

        // Factories
        protected Partials _partialFactory;

        public PartialTimeline(WaveFormat format) {
            base.Initialize(format);
            //
            _partialFactory = new Partials(format);
        }


        public void AddPartial(int startMs, double freqHz, int attackMs, int releaseMs, float level, float curve = -4.0f) {
            var p = _partialFactory.MakePartial(freqHz, attackMs, releaseMs, level, curve);
            int start = _format.MsToSamples(startMs);
            int length = p.GetLength();
            _parts.Add(new Part {
                startSample = start,
                endSample   = start + length,
                partial      = p
            });
            _parts.Sort(Part.CompareStart);
        }


        public override bool Fill(byte[] buffer)
        {
            if (_parts.Count == 0) return false; // stop if no partials left on timeline

            int bufferPos = 0; // in bytes
            int bufferSampleCount = buffer.Length / _format.bytesPerSample;

            var endedPartials = new List<int>();

            for (int i = 0; i < bufferSampleCount / _format.channels; ++i)
            {
                int sampleValue = 0;

                if (_parts.Count > 0)
                {
                    for (int j = 0; j < _parts.Count; ++j)
                    {
                        Part p = _parts[j]; // copy if Part is struct

                        if (p.endSample <= _currentSample) {
                            if (p.endSample != _currentSample) {
                                Debug.WriteLine("Warning! Part skipped: {0}", p); // partial added too late ?
                            }
                            endedPartials.Add(j);
                            continue;
                        }

                        if (p.startSample <= _currentSample) {
                            sampleValue += p.partial.GetNextValue();
                            _parts[j] = p; // for struct
                        } else {
                            break;
                        }
                    }

                    if (endedPartials.Count > 0) {
                        for (int j = 0; j < endedPartials.Count; ++j) {
                            _parts.RemoveAt(endedPartials[j]);
                        }
                        endedPartials.Clear();
                    }
                }

                _currentSample += 1;

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
