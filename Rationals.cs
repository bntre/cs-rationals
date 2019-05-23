//#define USE_CHAR_POWERS
//#define USE_BIGINTEGER

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Rewrite: https://bitbucket.org/bntr/harmony/src/default/rationals.py

// https://en.wikipedia.org/wiki/Fundamental_theorem_of_arithmetic#Canonical_representation_of_a_positive_integer
// https://oeis.org/wiki/Prime_factorization#Prime_power_decomposition_of_rational_numbers

namespace Rationals
{
#if USE_CHAR_POWERS
    using Pow = System.Char; //!!! adapt other code to chars
#else
    using Pow = System.Int32;
#endif

#if USE_BIGINTEGER
    using Long = System.Numerics.BigInteger;
    using LongMath = System.Numerics.BigInteger;
#else
    using Long = System.Int64;
    using LongMath = System.Math;
#endif

    public static partial class Utils
    {
        private static int[] primes = new[] {
            2, 3, 5, 7, 11, 13, 17, 19, 23, 29,
            31, 37, 41, 43, 47, 53, 59, 61, 67, 71,
            73, 79, 83, 89, 97, 101, 103, 107, 109, 113,
            127, 131, 137, 139, 149, 151, 157, 163, 167, 173,
            179, 181, 191, 193, 197, 199, 211, 223, 227, 229,
            233, 239, 241, 251, 257, 263, 269, 271, 277, 281,
        };

        public static int GetPrime(int i) {
            if (i < primes.Length) return primes[i];
            throw new NotImplementedException("Here should be a generator");
            //return 0;
        }

        public static Long Pow(Long n, int e) {
#if USE_BIGINTEGER
            return LongMath.Pow(n, e);
#else
            if (e < 0) throw new ArgumentOutOfRangeException("Negative exponent");
            if (e == 0) return 1;
            checked {
                // like in https://stackoverflow.com/questions/383587/how-do-you-do-integer-exponentiation-in-c
                Long result = 1;
                for (;;) {
                    if ((e & 1) == 1) {
                        result *= n;
                    }
                    e >>= 1;
                    if (e == 0) break;
                    n *= n;
                }
                return result;
            }
#endif
        }
    }



    // Raw Pow[] powers utils
    public static class Powers
    {
        public static Pow SafeAt(Pow[] pows, int i) {
            return i < pows.Length ? pows[i] : (Pow)0;
        }
        private static Pow[] MaxLength(Pow[] p0, Pow[] p1) {
            return new Pow[Math.Max(p0.Length, p1.Length)];
        }

        public static Pow[] Clone(Pow[] p) {
            Pow[] r = new Pow[p.Length];
            p.CopyTo(r, 0);
            return r;
        }

        // Using Monzo notation https://en.xen.wiki/w/Monzos
        public static string ToString(Pow[] pows, string brackets = "|>") {
            string s = brackets.Substring(0, 1);
            for (int i = 0; i < pows.Length; ++i) {
                if (i != 0) s += " ";
                s += pows[i].ToString();
                //s += pows[i].ToString("+0;-0");
            }
            s += brackets.Substring(1);
            return s;
        }

        //!!! optimize avoiding trailing zeros {xxx,0,0,0,0,0,0,0,0}

        public static Pow[] Mul(Pow[] p0, Pow[] p1) {
            Pow[] pows = MaxLength(p0, p1);
            for (int i = 0; i < pows.Length; ++i) {
                pows[i] = (Pow)(SafeAt(p0, i) + SafeAt(p1, i));
            }
            return pows;
        }

        public static Pow[] Div(Pow[] p0, Pow[] p1) {
            Pow[] pows = MaxLength(p0, p1);
            for (int i = 0; i < pows.Length; ++i) {
                pows[i] = (Pow)(SafeAt(p0, i) - SafeAt(p1, i));
            }
            return pows;
        }

        public static Pow[] Power(Pow[] p, int e) {
            Pow[] pows = new Pow[p.Length];
            for (int i = 0; i < pows.Length; ++i) {
                pows[i] = (Pow)(p[i] * e);
            }
            return pows;
        }

        public static bool Equal(Pow[] p0, Pow[] p1) {
            //if (p0 == null) return p1 == null;
            int len = Math.Max(p0.Length, p1.Length);
            for (int i = 0; i < len; ++i) {
                if (SafeAt(p0, i) != SafeAt(p1, i)) return false;
            }
            return true;
        }

        public static int GetLength(Pow[] pows) { // ignoring trailing zeros
            int len = pows.Length;
            while (len > 0 && pows[len-1] == 0) --len;
            return len;
        }

        public static int GetHash(Pow[] pows) {
            int len = GetLength(pows);
            int h = 0;
            for (int i = 0; i < len; ++i) {
                int h1 = pows[i].GetHashCode();
                h = ((h << 5) + h) ^ h1; // like https://referencesource.microsoft.com/#System.Web/Util/HashCodeCombiner.cs
            }
            return h;
        }

        public static int Compare(Pow[] p0, Pow[] p1) {
            Long n, d;
            ToFraction(Div(p0, p1), out n, out d);
            return n.CompareTo(d);
        }

        public static Pow[] FromFraction(Long n, Long d) {
            return Div(FromInt(n), FromInt(d));
        }

        public static void Split(Pow[] pows, out Pow[] ns, out Pow[] ds) {
            ns = new Pow[pows.Length];
            ds = new Pow[pows.Length];
            for (int i = 0; i < pows.Length; ++i) {
                Pow e = pows[i];
                if (e == 0) continue;
                if (e > 0)
                    ns[i] = e;
                else
                    ds[i] = (Pow)(-e);
            }
        }

        public static void ToFraction(Pow[] pows, out Long n, out Long d) {
            Pow[] ns, ds;
            Split(pows, out ns, out ds);
            n = ToInt(ns);
            d = ToInt(ds);
        }

        public static Pow[] FromInt(Long n) {
            if (n <= 0) throw new ArgumentException();
            var pows = new List<Pow>();
            for (int i = 0; n != 1; ++i) {
                Long p = (Long)Utils.GetPrime(i);
                //if (p == 0) break; // out of known primes
                Pow e = (Pow)0;
                for (;;) {
                    Long rem;
                    Long n1 = LongMath.DivRem(n, p, out rem);
                    if (rem != 0) break;
                    n = n1;
                    e += (Pow)1;
                }
                pows.Add(e);
            }
            return pows.ToArray();
        }

        public static Long ToInt(Pow[] pows) {
            Long n = 1;
            for (int i = 0; i < pows.Length; ++i) {
                Pow e = pows[i];
                if (e == 0) continue;
                if (e < 0) throw new Exception("Negative powers - this rational is not an integer");
                checked {
                    n *= Utils.Pow((Long)Utils.GetPrime(i), e);
                }
            }
            return n;
        }

        public static double ToDouble(Pow[] pows) {
            double d = 1.0;
            for (int i = 0; i < pows.Length; ++i) {
                Pow e = pows[i];
                if (e == 0) continue;
                d *= Math.Pow(Utils.GetPrime(i), e);
            }
            return d;
        }

    }


    [System.Diagnostics.DebuggerDisplay("{FormatFraction()} {FormatMonzo()}")]
    public struct Rational
    {
        private Pow[] pows;

        public Rational(Long integer) {
            this.pows = Powers.FromFraction(integer, 1);
        }
        public Rational(Long nominator, Long denominator) {
            this.pows = Powers.FromFraction(nominator, denominator);
        }
        public Rational(Pow[] primePowers) {
            this.pows = primePowers;
        }
        public Rational(Rational r) {
            this.pows = Powers.Clone(r.pows);
        }

        public bool IsDefault() {
            return pows == null;
        }

        public int GetPowerCount() { //!!! find better name
            return Powers.GetLength(pows);
        }

        public bool Equals(Rational r) {
            if (r.IsDefault()) return this.IsDefault();
            return Powers.Equal(pows, r.pows);
        }

        public override int GetHashCode() {
            return Powers.GetHash(pows);
        }
        public override bool Equals(object obj) {
            return obj is Rational && Equals((Rational)obj);
        }

        public Rational Clone() {
            return new Rational(Powers.Clone(pows));
        }

        public Pow[] GetPrimePowers() {
            return pows;
        }

        public bool IsInteger() {
            Pow[] ns, ds;
            Powers.Split(pows, out ns, out ds);
            return Powers.GetLength(ds) == 0;
        }

        public struct Fraction {
            public Long N;
            public Long D;
        }

        public Fraction ToFraction() {
            Fraction f;
            Powers.ToFraction(pows, out f.N, out f.D);
            return f;
        }

        public string FormatFraction(string delimiter = "/") {
            Fraction f = this.ToFraction();
            string s = f.N.ToString();
            if (f.D != 1) s += delimiter + f.D.ToString();
            return s;
        }

        public override string ToString() {
            return FormatFraction();
        }

        public string FormatMonzo() {
            return Powers.ToString(pows);
        }

        public double ToCents() {
            return Math.Log(Powers.ToDouble(pows), 2) * 1200.0;
        }

        public static readonly Rational One = new Rational(1);
        public static readonly Rational Two = new Rational(2);

        // Operators
        public static Rational operator *(Rational r0, Rational r1) { return new Rational(Powers.Mul(r0.pows, r1.pows)); }
        public static Rational operator /(Rational r0, Rational r1) { return new Rational(Powers.Div(r0.pows, r1.pows)); }
        public static Rational operator *(Rational r, int n) { return r * new Rational(n); }
        public static Rational operator /(Rational r, int n) { return r / new Rational(n); }
        //!!! troubles with null != null
        //private static bool SomeNull(Rational r0, Rational r1) { return object.ReferenceEquals(r0, null) || object.ReferenceEquals(r1, null); }
        //public static bool operator ==(Rational r0, Rational r1) { return SomeNull(r0, r1) ? false : Powers.Equal(r0.pows, r1.pows); }
        //public static bool operator !=(Rational r0, Rational r1) { return SomeNull(r0, r1) ? true : !Powers.Equal(r0.pows, r1.pows); }
        public static bool operator < (Rational r0, Rational r1) { return Powers.Compare(r0.pows, r1.pows) <  0; }
        public static bool operator > (Rational r0, Rational r1) { return Powers.Compare(r0.pows, r1.pows) >  0; }
        public static bool operator <=(Rational r0, Rational r1) { return Powers.Compare(r0.pows, r1.pows) <= 0; }
        public static bool operator >=(Rational r0, Rational r1) { return Powers.Compare(r0.pows, r1.pows) >= 0; }

        public Rational Power(int e) {
            return new Rational(Powers.Power(pows, e));
        }

        static public Rational Prime(int primeIndex) {
            var pows = new Pow[primeIndex + 1];
            pows[primeIndex] = (Pow)1;
            return new Rational(pows);
        }

#region Epimorics
        // We use p/(p-1) ratios (p is prime). See:
        //  https://en.wikipedia.org/wiki/Superparticular_ratio
        //  https://en.wikipedia.org/wiki/Leibniz_formula_for_%CF%80#Euler_product

        public Pow[] GetEpimoricPowers() {
            int len = pows.Length;
            Pow[] res = new Pow[len];
            var r = this.Clone();
            for (int i = len - 1; i >= 0; --i) {
                Pow e = r.pows[i];
                res[i] = e;
                if (e != 0) {
                    int p = Utils.GetPrime(i);
                    r /= new Rational(p, p - 1).Power(e);
                }
            }
            return res;
        }
#endregion

#region Narrow Primes
        public static Rational GetNarrowPrime(int i, int basePrimeIndex = 0) {
            // base 0 (denominator 2) -> 2/1, 3/2, 5/4, 7/4, 11/8,..
            // base 1 (denominator 3) -> (2), 3/1, 5/3, 7/3, 11/9,..

            if (i <= basePrimeIndex) return Rational.Prime(i);

            int n = Utils.GetPrime(i);
            int b = Utils.GetPrime(basePrimeIndex);
            //
            /*
            int d = 1;
            for (;;) {
                int dd = d * b;
                if (dd >= n) break;
                d = dd;
            }
            */
            double l = Math.Log(n - 1, b);
            Pow e = (Pow)Math.Round(l);
            Long d = Utils.Pow(b, e);
            return new Rational(n, d);
        }
        public static Rational ValidateNarrow(Rational n) {
            if (n.IsDefault()) return n; // invalid
            int powerCount = n.GetPowerCount();
            if (powerCount > 0) {
                if (n.GetPrimePowers()[powerCount - 1] < 0) { // max prime should be in nominator
                    n = Rational.One / n;
                }
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
        public static Rational[] GetNarrowPrimes(int count, int basePrimeIndex = 0, Rational[] customNarrows = null) {
            Rational[] ns = new Rational[count];
            // set custom narrows
            if (customNarrows != null) {
                for (int i = 0; i < customNarrows.Length; ++i) {
                    Rational n = ValidateNarrow(customNarrows[i]);
                    int maxPrimeIndex = n.GetPowerCount() - 1;
                    if (maxPrimeIndex < ns.Length) {
                        ns[maxPrimeIndex] = n;
                    }
                }
            }
            // set defaults
            for (int i = 0; i < count; ++i) {
                if (ns[i].IsDefault()) {
                    ns[i] = GetNarrowPrime(i, basePrimeIndex); // default narrow prime
                }
            }
            return ns;
        }
        public Pow[] GetNarrowPowers(Rational[] narrowPrimes) {
            int len = pows.Length;
            if (len > narrowPrimes.Length) return null;
            Pow[] res = new Pow[len];
            Rational r = this.Clone();
            for (int i = len - 1; i >= 0; --i) {
                Pow e = r.pows[i];
                res[i] = e;
                if (e != 0) {
                    r /= narrowPrimes[i].Power(e);
                }
            }
            return res;
        }
        public string FormatNarrows(Rational[] narrowPrimes = null) {
            if (narrowPrimes == null) {
                narrowPrimes = GetNarrowPrimes(this.GetPowerCount());
            }
            return Powers.ToString(GetNarrowPowers(narrowPrimes), "|}");
        }
#endregion

#region Parse
        public static Rational Parse(string s) {
            if (String.IsNullOrWhiteSpace(s)) return default(Rational);
            s = s.Trim();
            Long n;
            if (Long.TryParse(s, out n)) { // an integer
                return new Rational(n);
            } if (s.Contains('/') || s.Contains(':')) { // a fraction "n/d" or "n:d"
                string[] parts = s.Split(new[]{'/',':'});
                if (parts.Length == 2) {
                    Long d;
                    if (Long.TryParse(parts[0], out n) &&
                        Long.TryParse(parts[1], out d)) {
                        try {
                            return new Rational(n, d);
                        } catch {
                            return default(Rational);
                        }
                    }
                }
            } else if (s.StartsWith("|")) {
                // Parse a monzo "|a b c d e f... >" like https://en.xen.wiki/w/Monzos
                s = s.Trim('|','>');
                string[] parts = s.Split(new[]{' ','\t'}, StringSplitOptions.RemoveEmptyEntries);
                Pow[] pows = new Pow[parts.Length];
                for (int i = 0; i < pows.Length; ++i) {
                    if (!Pow.TryParse(parts[i], out pows[i])) {
                        pows = null;
                        break;
                    }
                }
                if (pows != null) return new Rational(pows);
            }
            return default(Rational);
        }
#endregion
    }



    public class Temperament {
        int _equalSteps;
        double _stepCents;
        string[] _noteNames = null;
        public Temperament(int equalSteps, Rational Base) {
            _equalSteps = equalSteps;
            _stepCents = Base.ToCents() / equalSteps;
            if (_stepCents == 12 && Base.Equals(Rational.Two)) {
                _noteNames = "C C# D D# E F F# G G# A A# B B#".Split(' ');
            }
        }
        public string FormatRational(Rational r) {
            return FormatCents(r.ToCents());
        }
        public string FormatCents(double cents) {
            int tone = (int)Math.Round(cents / _stepCents);
            double shift = cents - tone * _stepCents;
            int octave = tone / _equalSteps;
            tone = tone % _equalSteps;
            return string.Format("{0}{1}{2:+0;-0;+0}c",
                octave == 0 ? "" : String.Format("{0}_", octave),
                _noteNames != null ? _noteNames[tone] : tone.ToString(),
                shift
            );
        }
    }



    public class Bands<T>
    {
        //!!! use barks instead of cents: https://en.wikipedia.org/wiki/Bark_scale

        protected float _bandWidth; // in cents
        protected int _bandCount;
        protected int _bandShift; // negative
        protected List<T>[] _bands;

        protected int GetBandIndex(float cents) {
            return (int)Math.Round(cents / _bandWidth) - _bandShift;
        }

        public Bands(float bandWidth = 100, float cents0 = -7 * 1200f, float cents1 = 7 * 1200f) {
            _bandWidth = bandWidth;
            _bandCount = (int)Math.Ceiling((cents1 - cents0) / _bandWidth);
            _bandShift = (int)Math.Floor((cents0) / _bandWidth);
            _bands = new List<T>[_bandCount];
            for (int i = 0; i < _bandCount; ++i) {
                _bands[i] = new List<T>();
            }
        }

        public bool AddItem(float cents, T item) {
            int index = GetBandIndex(cents);
            if (index < 0 || index >= _bandCount) return false;
            _bands[index].Add(item);
            return true;
        }

        public T[] GetNeighbors(float cents) {
            int index = GetBandIndex(cents);
            int i0 = Math.Max(index - 1, 0);
            int i1 = Math.Min(index + 1, _bandCount - 1);
            //
            int len = 0;
            for (int i = i0; i <= i1; ++i) {
                len += _bands[i].Count;
            }
            //
            T[] result = new T[len];
            int p = 0;
            for (int i = i0; i <= i1; ++i) {
                _bands[i].CopyTo(result, p);
                p += _bands[i].Count;
            }
            return result;
        }
    }


}
