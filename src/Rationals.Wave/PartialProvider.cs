using System;
using System.Collections.Generic;
using System.Diagnostics;

//using Rationals;


namespace Rationals.Wave
{
    using Arg   = System.Single; // to fill table
    using Value = System.Single;
    //using Freq  = System.Single;

    #region Tables
    public class TableFunc
    {
        // statics
        public static readonly int TableSizeBits = 12;
        public static readonly int TableSize = 1 << TableSizeBits;            
        public static Arg IndexToArg(int i) { return (Arg)i / TableSize; }
        public static int ArgToIndex(Arg a) { return (int)(a * TableSize); }

        //
        public Value[] Table = new Value[TableSize];

        public void FillTable(Func<int, Value> func) {
            for (int i = 0; i < TableSize; ++i) {
                Table[i] = func(i);
            }
        }

        public void FillTable(Func<Arg, Value> func) {
            for (int i = 0; i < TableSize; ++i) {
                Table[i] = func(IndexToArg(i));
            }
        }

        public virtual Value GetValue(Arg a) {
            return Table[ArgToIndex(a)];
        }
    }

    public class SineTable : TableFunc {
        public SineTable() {
            FillTable(
                (int i) => (Value)Math.Sin(2 * Math.PI * i / TableFunc.TableSize)
            );
        }
    }

    public class CurveTable : TableFunc { // Like SuperCollider curve value
        public CurveTable(double curve) {
            double e = Math.Pow(2.0, curve);
            FillTable(
                (int i) => (Value)Math.Pow((double)i / TableFunc.TableSize, e)
            );
        }

        //!!! use some CacheDictionary
        private static Dictionary<int, CurveTable> _tables = new Dictionary<int, CurveTable>();
        public static CurveTable GetTable(double curve) {
            int key = (int)(curve * 100); //!!! do we need better precision
            CurveTable table;
            if (!_tables.TryGetValue(key, out table)) {
                table = new CurveTable(curve);
                _tables[key] = table;
            }
            return table;
        }
    }

    #endregion

    public abstract class FuncBase {
        public int Length = 0;

        protected int _currentPos = 0;
        public virtual void ResetPos() { _currentPos = 0; }
        public abstract Value GetNextValue();
    }

    // Like SuperCollider Env https://doc.sccode.org/Classes/Env.html
    public class Envelope : FuncBase
    {
        protected struct Part {
            public int start;
            public int length;
            public Value levelStart;
            public Value levelStep;
            public TableFunc func;
        }

        protected Part[] _parts = null;

        protected int _currentPart = -1;
        protected int _partPos = 0;

        public Envelope(Value[] levels, int[] lengths, float curve)
        {
            Debug.Assert(levels.Length == lengths.Length + 1, "Envelope lengths don't match");

            int partCount = lengths.Length;
            _parts = new Part[partCount];
            int pos = 0;
            for (int i = 0; i < partCount; ++i) {
                Part p = new Part();
                p.start = pos;
                p.length = lengths[i];
                pos += p.length;
                p.levelStart = levels[i];
                p.levelStep  = levels[i+1] - levels[i];
                p.func = CurveTable.GetTable(curve);
                _parts[i] = p;
            }

            Length = pos;

            ResetPos();
        }

        public override void ResetPos() {
            _currentPart = 0;
            _partPos = 0;
        }

        // Like SuperCollider Env.perc
        public Envelope(int attack, int release, Value level, float curve = -4)
            : this (
                new Value[] { 0, level, 0 },
                new int[] { attack, release },
                curve
            )
        { }

        public override Value GetNextValue() {
            if (_currentPart == -1 || _currentPart == _parts.Length) return 0;

            Part p = _parts[_currentPart];
            Value value = p.levelStart + p.levelStep * p.func.GetValue((Arg)_partPos / p.length);

            _partPos += 1;
            if (_partPos == p.length) {
                _partPos = 0;
                _currentPart += 1;
            }

            return value;
        }

    }


    public class PartialProvider<T> : SampleProvider<T>
        where T : unmanaged
    {
        protected struct Partial {
            public Envelope env;
            //public Freq freq;
            public Arg currentPhase;
            public Arg phaseStep; // per sample
            public int sampleEnded;
            //
            public override string ToString() { return String.Format("Partial step {0}", phaseStep); }
        }

        protected SineTable _sineTable = new SineTable();

        // Safe threads
        protected int _currentSample = 0; // atomic. MainThread, PlayingThread. ok if it overflows: it's like a phase
        protected object _partialsLock = new object();
        protected Partial[] _partials = new Partial[] {}; // PlayingThread
        protected List<Partial> _partialsToAdd = new List<Partial>(); // locking. MainThread, PlayingThread

        public PartialProvider() {
        }

        public string FormatStatus() {
            return String.Format("Partials count: {0}", _partials.Length);
        }

        protected int MsToSamples(int ms) {
            return _format.sampleRate * ms / 1000;
        }
        protected Arg FreqToStep(float freq) {
            return (Arg)freq / _format.sampleRate;
        }

        public void AddPartial(float freqHz, int attackMs, int releaseMs, Value level, float curve = -4.0f) {
            Envelope env = new Envelope(
                MsToSamples(attackMs),
                MsToSamples(releaseMs),
                level,
                curve
            );
            Partial p = new Partial {
                env = env,
                currentPhase = 0,
                phaseStep = FreqToStep(freqHz),
            };
            lock (_partialsLock) {
                _partialsToAdd.Add(p);
            }
        }

        protected Value GetPartialNextValue(ref Partial p) {
            Value level = p.env.GetNextValue();

            p.currentPhase += p.phaseStep;
            p.currentPhase %= 1;
            Debug.Assert(0 <= p.currentPhase && p.currentPhase < 1);
            //Value amp = _sineTable.GetValue(p.currentPhase);
            Value amp = _sineTable.Table[(int)(p.currentPhase * TableFunc.TableSize)];

            return level * amp;
        }

        // implement SampleProvider
        // PlayingThread.
        public override bool Fill(T[] buffer) {
            if (_partials.Length == 0) {
                bool empty;
                lock (_partialsLock) {
                    empty = _partialsToAdd.Count == 0;
                }
                if (empty) {
                    Clear(buffer);
                    return true;
                }
            }

            int sampleCount = buffer.Length;
            int sampleIndex = 0;

            for (int i = 0; i < sampleCount / _format.channels; ++i)
            {
                // Add new partials
                if ((_currentSample & 0xFFF) == 0) { // don't lock every sample
                    lock (_partialsLock) {
                        if (_partialsToAdd.Count > 0) {
                            int l = _partials.Length;
                            Array.Resize(ref _partials, l + _partialsToAdd.Count);
                            for (int j = 0; j < _partialsToAdd.Count; ++j) {
                                Partial p = _partialsToAdd[j];
                                unchecked {
                                    p.sampleEnded = _currentSample + p.env.Length;
                                }
                                _partials[l + j] = p;
                                Debug.WriteLine("Partial added: {0}", p);
                            }
                            _partialsToAdd.Clear();
                            Debug.WriteLine("Partials count: {0}", _partials.Length);
                        }
                    }
                }

                // Remove ended partials
                if (_partials.Length > 0) {
                    var newPartials = new List<Partial>();
                    for (int j = 0; j < _partials.Length; ++j) {
                        if (_partials[j].sampleEnded != _currentSample) {
                            newPartials.Add(_partials[j]);
                        } else {
                            Debug.WriteLine("Partial removed: {0}", _partials[j]);
                        }
                    }
                    if (_partials.Length != newPartials.Count) {
                        _partials = newPartials.ToArray();
                        Debug.WriteLine("Partials count: {0}", _partials.Length);
                    }
                }

                unchecked {
                    _currentSample += 1; // ok if it overflows
                }

                // Get sample value
                float sampleValue = 0;
                for (int j = 0; j < _partials.Length; ++j) {
                    sampleValue += GetPartialNextValue(ref _partials[j]);
                }

                // Write sample value to all channels
                for (int c = 0; c < _format.channels; ++c) {
                    buffer[sampleIndex++] = this.FromFloat(sampleValue);
                }
            }

            return true;

        }

    }
}