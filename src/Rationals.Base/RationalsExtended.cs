// Extended classes for Rationals: Narrows, Subgroup, Temperament

using System;
using System.Collections.Generic;
#if DEBUG
  using System.Linq;
  using System.Diagnostics;
#endif

namespace Rationals
{
    using Matrix = Rationals.Vectors.Matrix;

#if USE_CHAR_POWERS
    using Pow = System.Char; //!!! adapt other code to chars
#else
    using Pow = System.Int32;
#endif

#if USE_BIGINTEGER
    using Long     = System.Numerics.BigInteger;
    using LongMath = System.Numerics.BigInteger;
#else
    using Long     = System.Int64;
    using LongMath = System.Math;
#endif

    // Narrow intervals needed to calculate rational-s parent in the tree to show.
    // They are kind of an alternative basis: {2. 3/2. 5/4} instead of {2. 3. 5}
    // Ordered by max prime index.
    //   E.g.   subgroup    ->      narrows
    //                              for (2)  (3)  (5)  (7)  ...
    //          2. 3. 5                 [-,  3/2, 5/4]
    //          3. 5. 7                 [     -,  5/3, 7/9]  or  [-, 5/3, 7/5]
    //          3. 5. 7. 11/4           [     -,  5/3, 7/9, 11/12]


    // narrowIndex               0    1    2    3     4
    // base 0 (denominator 2) -> 2/1, 3/2, 5/4, 7/4, 11/8, ..
    // base 1 (denominator 3) -> (2), 3/1, 5/3, 7/3, 11/9, ..

    // base 1 (denominator 3): 11/4 -> 11/12

    public static class NarrowUtils
    {
        public static Rational MakeNarrow(Rational r, Rational b) {
            if (r.GetHighPrimeIndex() == b.GetHighPrimeIndex()) return r; // can't narrow on same prime level
            double r_ = r.ToDouble();
            double b_ = b.ToDouble();
            double l = Math.Log(r_, b_); // e.g. 1.58 for {3, 2}
            Pow e = (Pow)(l + 0.25); // something between Floor and Round
            r /= b.Power(e);
            return r;
        }

        public static Rational MakeNarrow(int narrowIndex, int basePrimeIndex) {
            Rational r = Rational.Prime(narrowIndex); // just prime: 2/1, 3/1, 5/1, 7/1, ..
            return MakeNarrow(r, basePrimeIndex);
        }

        public static Rational MakeNarrow(Rational r, int basePrimeIndex) {
            if (r.GetHighPrimeIndex() <= basePrimeIndex) return r; // can't narrow
            r = ValidateNarrow(r);
            //
            double rr = r.ToDouble();
            if (rr < 1.0) return r; // e.g. for 5/6 -   doubtful !!!

            int b = Utils.GetPrime(basePrimeIndex); // base prime: 2, 3,..
            double l = Math.Log(rr - 1.0, b);
            Pow e = (Pow)Math.Round(l);
            Long d = Utils.Pow(b, e);
            r /= d;
            return r;
        }

        public static Rational ValidateNarrow(Rational n) {
            if (n.IsDefault()) return default(Rational); // invalid
            int h = n.GetHighPrimeIndex();
            if (h == -1) return default(Rational); // "1" can't be a narrow
            if (n.GetPrimePower(h) < 0) { // max prime should be in nominator
                n = Rational.One / n;
            }
            return n;
        }
        public static Rational[] ValidateNarrows(Rational[] ns) {
            if (ns == null) return null;
            for (int i = 0; i < ns.Length; ++i) {
                ns[i] = ValidateNarrow(ns[i]);
            }
            return ns;
        }

        public static Rational[] GetDefault(int count, int basePrimeIndex = 0) {
            var narrows = new Rational[count];
            for (int i = 0; i < count; ++i) {
                narrows[i] = MakeNarrow(i, basePrimeIndex);
            }
            return narrows;
        }

        public static Pow[] GetNarrowPowers(this Rational r, Rational[] narrows = null) {
            int len = r.GetHighPrimeIndex() + 1;
            if (narrows == null) {
                narrows = NarrowUtils.GetDefault(len);
            } else {
                if (len > narrows.Length) return null;
            }
            Pow[] res = new Pow[len];
            r = r.Clone();
            for (int i = len - 1; i >= 0; --i) {
                if (narrows[i].IsDefault()) continue;
                Pow e = r.GetPrimePower(i);
                res[i] = e;
                if (e != 0) {
                    r /= narrows[i].Power(e);
                }
            }
            return res;
        }
        public static string FormatNarrowPowers(this Rational r, Rational[] narrows = null) {
            Pow[] pows = GetNarrowPowers(r, narrows);
            if (pows == null) return null; //!!! where invalid narrowPrimes from?
            return Powers.ToString(pows, "|}");
        }

    }


    public class Subgroup
    {
        private Rational[] _items;
        
        private Matrix _matrix; // matrix made from subgroup items; used e.g. to validate temperament intervals

        // Prime "range" of the subgroup
        private Rational _baseItem; // an item with smallest high-prime index
        private int _basePrimeIndex = 0;
        private int _highPrimeIndex = 0; // largest high-prime index

        // Narrows
        private Rational[] _narrows; // NB! per prime index, may contain null-s
        private string _error = null; // error message for a gui control

        public Subgroup(int limitPrimeIndex) {
            _items = Rational.Primes(limitPrimeIndex + 1);
            _matrix = new Matrix(_items, makeDiagonal: true);
            UpdateRange();
            UpdateNarrows();
        }

        public Subgroup(Rational[] items) {
            _items = items;
            _matrix = new Matrix(_items, makeDiagonal: true);
            UpdateRange();
            UpdateNarrows();
        }

        private void UpdateRange() {
            _baseItem = default(Rational);
            _basePrimeIndex = -1;
            _highPrimeIndex = -1;
            //
            int maxIndex = 0;
            int minIndex = int.MaxValue;
            //
            foreach (Rational r in _items) {
                int i = r.GetHighPrimeIndex();
                if (maxIndex < i) {
                    maxIndex = i;
                }
                if (minIndex > i) {
                    minIndex = i;
                    _baseItem = r;
                }
            }
            //
            _highPrimeIndex = maxIndex;
            _basePrimeIndex = _baseItem.GetHighPrimeIndex();
        }

        public string GetError() { return _error; }
        public Rational[] GetItems() { return _items; }
        public int GetHighPrimeIndex() { return _highPrimeIndex; }

        public bool IsInRange(Rational r) {
            return _matrix != null && _matrix.FindCoordinates(r) != null;
        }

        #region Narrows
        public void UpdateNarrows(Rational[] userNarrows = null)
        {
            _narrows = new Rational[_highPrimeIndex + 1];
            _error = null;

            // set default narrows
            foreach (Rational item in _items) {
                Rational narrow = NarrowUtils.MakeNarrow(item, _baseItem);
                SetNarrow(narrow);
            }

            // set custom user narrows
            if (userNarrows != null) {
                var invalidNarrows = new List<Rational>();
                foreach (Rational narrow in userNarrows) {
                    if (narrow.IsDefault()) continue;
                    if (!IsInRange(narrow)) {
                        invalidNarrows.Add(narrow);
                    } else {
                        SetNarrow(narrow);
                    }
                }
                if (invalidNarrows.Count > 0) {
                    _error = "Narrows out of subgroup: " + String.Join(", ", invalidNarrows);
                }
            }

            // !!! here we should check if resulting narrows can solve each generated rational;
            //   currently we add all missing narrows (even if it's out of subgroup) instead.
            /*
            for (int i = 0; i < _narrows.Length; ++i) {
                if (_narrows[i].IsDefault()) {
                    _narrows[i] = NarrowUtils.MakeNarrow(Rational.Prime(i), _baseItem); // default narrow prime
                }
            }
            */
#if DEBUG
            Debug.WriteLine("Narrows set: " + Rational.FormatRationals(_narrows, ".", "-"));
#endif
        }

        private bool SetNarrow(Rational n) {
            // make high prime positive
            n = NarrowUtils.ValidateNarrow(n);
            if (n.IsDefault()) return false;
            // set narrow to the array - by high prime index
            int h = n.GetHighPrimeIndex();
            if (0 <= h && h < _narrows.Length) {
                _narrows[h] = n;
                return true;
            }
            return false;
        }

        public Rational GetBaseItem() { return _baseItem; }

        public Rational[] GetNarrowItems() { // just exclude missing (null) items without Linq
            var ns = new List<Rational>();
            foreach (Rational n in _narrows) {
                if (!n.IsDefault()) {
                    ns.Add(n);
                }
            }
            return ns.ToArray();
        }

        public Rational GetNarrow(int primeIndex) { return _narrows[primeIndex]; }

        public Pow[] GetNarrowPowers(Rational r) {
            return r.GetNarrowPowers(_narrows);
        }

        public string FormatNarrowPowers(Rational r) {
            return r.FormatNarrowPowers(_narrows);
        }

        public Rational GetNarrowParent(Rational r) {
            int lastLevel = r.GetHighPrimeIndex();
            if (lastLevel > _highPrimeIndex) return default(Rational);
            if (lastLevel <= _basePrimeIndex) return default(Rational); // We don't draw lines between base intervals (e.g. 1/2 - 1 - 2 - 4).
            //
            Rational step = _narrows[lastLevel]; // last level step
            if (step.IsDefault()) return default(Rational); //!!! exception ?
            int lastPower = r.GetPrimePower(lastLevel); // last level coordinate
            if (lastPower > 0) {
                return r / step;
            } else {
                return r * step;
            }
        }

        #endregion Narrows
    }


#if DEBUG
    [DebuggerDisplay("{rational.FormatFraction()}->{cents}c")]
#endif
    public struct Tempered {
        public Rational rational;
        public float cents; // rational tempered to (e.g. 3/2 -> 700)
    }

    public class Temperament
    {
        // Solving rationals with a matrix:
        //  Tempered intervals + primes (so we can solve each narrow prime of basis)
        private Matrix _matrix = null; // null if unset

        private float[] _pureCents  = null;
        private float[] _deltaCents = null;

        // Apply a measure (0..1)
        private float _measure = 0;
        private float[] _measuredCents = null; // measured cents per rational coordinate: pure_cents + delta_cents * measure

        public bool IsSet() { return _matrix != null; }

        public void SetTemperament(Tempered[] tempered, Subgroup subgroup)
        {
            // skip invalid tempered intervals
            Tempered[] ts = Validate(tempered, subgroup);

            if (ts == null || ts.Length == 0) {
                // unset temperament
                _matrix        = null;
                _pureCents     = null;
                _deltaCents    = null;
                _measuredCents = null;
                return;
            }

            // Fill cents arrays and temperament matrix.
            // We add the subgroup items to be able to solve each narrow prime of the basis.
            Rational[] subgroupItems = subgroup.GetItems();
            int matrixSize = ts.Length + subgroupItems.Length;
            Rational[] rs               = new Rational[matrixSize];
            _pureCents       = new float[matrixSize];
            _deltaCents      = new float[matrixSize];
            _measuredCents   = new float[matrixSize];
            for (int i = 0; i < ts.Length; ++i) { // tempered intervals
                Rational r = ts[i].rational;
                rs[i] = r;
                float cents = (float)r.ToCents();
                _pureCents [i] = cents;
                _deltaCents[i] = ts[i].cents - cents;
            }
            for (int i = 0; i < subgroupItems.Length; ++i) { // subgroup intervals
                int j = ts.Length + i;
                Rational r = subgroupItems[i];
                rs[j] = r;
                float cents = (float)r.ToCents();
                _pureCents [j] = cents;
                _deltaCents[j] = 0f;
            }
            _matrix = new Matrix(rs, makeDiagonal: true);
                
            //
            UpdateMeasuredCents();
        }

        public void SetMeasure(float measure) {
            _measure = measure;
            UpdateMeasuredCents();
        }

        private void UpdateMeasuredCents() {
            if (_pureCents == null) {
                _measuredCents = null;
            } else {
                int matrixSize = _pureCents.Length;
                _measuredCents = new float[matrixSize];
                for (int i = 0; i < matrixSize; ++i) {
                    _measuredCents[i] = 
                        _pureCents[i] + 
                        _deltaCents[i] * _measure;
                }
            }
        }

        public float CalculateMeasuredCents(Rational r) {
            if (_matrix == null) {
                return (float)r.ToCents();
            } 

            float[] coords = _matrix.FindFloatCoordinates(r);
            if (coords == null) {
                //throw new Exception("Can't solve temperament");
                return (float)r.ToCents();
            }

            float cents = 0;
            for (int j = 0; j < coords.Length; ++j) {
                cents += coords[j] * _measuredCents[j];
            }
            return cents;
        }

        #region Helpers

        // Returns per-row errors
        public static string[] GetErrors(Tempered[] ts, Subgroup subgroup) {
            if (ts == null) return null;

            string[] errors = new string[ts.Length];

            Rational[] indep = new Rational[ts.Length]; // independent intervals
            int indepSize = 0;

            for (int i = 0; i < ts.Length; ++i) {
                Rational r = ts[i].rational;
                string error = null;
                if (r.IsDefault()) {
                    error = "Invalid rational";
                } else if (r.Equals(Rational.One)) {
                    error = "1/1 can't be tempered";
                } else if (!subgroup.IsInRange(r)) {
                    error = "Out of JI range";
                } else {
                    if (indepSize > 0) {
                        var m = new Vectors.Matrix(indep, -1, indepSize, makeDiagonal: true);
                        var coords = m.FindRationalCoordinates(r);
                        if (coords != null) {
                            error = "";
                            for (int j = 0; j < coords.Length; ++j) {
                                if (coords[j].sign != 0) {
                                    error += String.Format(" * {0}^{1}", indep[j].FormatFraction(), coords[j].FormatFraction());
                                }
                            }
                            if (error.Length != 0) {
                                error = "Dependend: " + error.Substring(2);
                            }
                            
                        }
                    }
                    indep[indepSize++] = r;
                }
                errors[i] = error;
            }

            return errors;
        }

        // Filters out invalid and dependent vectors
        public static Tempered[] Validate(Tempered[] ts, Subgroup subgroup) {
            if (ts == null) return null;

            var result = new List<Tempered>();

            Rational[] indep = new Rational[ts.Length]; // independent intervals
            int indepSize = 0;

            for (int i = 0; i < ts.Length; ++i) {
                Rational r = ts[i].rational;
                if (r.IsDefault()) continue;
                // skip if out of subgroup
                if (!subgroup.IsInRange(r)) continue;
                // skip if dependend
                if (indepSize > 0) {
                    var m = new Matrix(indep, -1, indepSize, makeDiagonal: true);
                    if (m.FindCoordinates(r) != null) continue;
                }
                indep[indepSize++] = r;
                //
                result.Add(ts[i]);
            }

            return result.Count == 0 ? null : result.ToArray();
        }

        #endregion Helpers
    }

    
    public class SomeInterval // a rational OR a specific (in cents)
                              // !!! make struct ? 
    {
        public Rational rational = default(Rational);
        public float cents = 0;

        public bool Equals(SomeInterval other) {
            return rational.Equals(other.rational) && cents == other.cents; //!!! compare cents with threshold?
        }

        public bool IsRational() { return !rational.IsDefault(); }

        public float ToCents() {
            if (!rational.IsDefault()) return (float)rational.ToCents();
            return cents;
        }
        public override string ToString() {
            if (!rational.IsDefault()) return rational.FormatFraction();
            return Rationals.Utils.FormatCents(cents);
        }
        public static SomeInterval Parse(string text) {
            var t = new SomeInterval();
            t.rational = Rational.Parse(text);
            if (t.rational.IsDefault()) {
                if (!float.TryParse(text.TrimEnd('c'), out t.cents)) {
                    return null;
                }
            }
            return t;
        }
    }

}
