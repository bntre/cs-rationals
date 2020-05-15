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
    using Long     = System.Numerics.BigInteger;
    using LongMath = System.Numerics.BigInteger;
#else
    using Long     = System.Int64;
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
            if (i < 0) throw new ArgumentException("Negative index");
            if (i < primes.Length) return primes[i];
            throw new NotImplementedException("Here should be a generator");
            // it throws e.g. on parsing a user rational
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

        public static int Sign(Long n) {
#if USE_BIGINTEGER
            return n.Sign;
#else
            return Math.Sign(n);
#endif
        }

        public static string FormatCents(float cents) {
            return String.Format("{0:F3}c", cents);
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

        public static Pow[] Max(Pow[] p0, Pow[] p1) { // kind of LCM
            Pow[] pows = MaxLength(p0, p1);
            for (int i = 0; i < pows.Length; ++i) {
                pows[i] = (Pow)Math.Max(SafeAt(p0, i), SafeAt(p1, i));
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
            int len0 = GetLength(p0);
            int len1 = GetLength(p1);
            if (len0 != len1) return false;
            for (int i = 0; i < len0; ++i) {
                if (p0[i] != p1[i]) return false;
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
        : IComparable<Rational>
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
        public bool IsZero() {
            return pows == null;
        }
        public bool IsInfinity() {
            return pows != null && pows.Length == 1 && pows[0] == Pow.MaxValue;
        }

        public Pow[] GetPrimePowers() {
            return pows;
        }
        public Pow GetPrimePower(int i) {
            return pows[i]; //!!! use SafeAt ?
        }
        public int GetInvolvedPowerCount() { // HighPrimeIndex - 1
            return Powers.GetLength(pows);
        }
        public int GetHighPrimeIndex() {
            return Powers.GetLength(pows) - 1;
        }

        public bool Equals(Rational r) {
            if (r.IsDefault()) return this.IsDefault();
            return Powers.Equal(pows, r.pows);
        }
        public bool Equals(Long nominator, Long denominator) {
            if (this.IsDefault()) return false;
            return Powers.Equal(pows, Powers.FromFraction(nominator, denominator));
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
            if (IsDefault()) return null;
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

        public double ToDouble() {
            return Powers.ToDouble(pows);
        }

        public double ToCents() {
            return Math.Log(ToDouble(), 2) * 1200.0;
        }

        public static readonly Rational Zero     = new Rational(null);
        public static readonly Rational Infinity = new Rational(new[] { Pow.MaxValue });
        public static readonly Rational One = new Rational(1);
        public static readonly Rational Two = new Rational(2);


        // Operators
        public static Rational operator *(Rational r0, Rational r1) { return new Rational(Powers.Mul(r0.pows, r1.pows)); }
        public static Rational operator /(Rational r0, Rational r1) { return new Rational(Powers.Div(r0.pows, r1.pows)); }
        public static Rational operator *(Rational r, Long n) { return r * new Rational(n); }
        public static Rational operator /(Rational r, Long n) { return r / new Rational(n); }
        //!!! troubles with null != null
        //private static bool SomeNull(Rational r0, Rational r1) { return object.ReferenceEquals(r0, null) || object.ReferenceEquals(r1, null); }
        //public static bool operator ==(Rational r0, Rational r1) { return SomeNull(r0, r1) ? false : Powers.Equal(r0.pows, r1.pows); }
        //public static bool operator !=(Rational r0, Rational r1) { return SomeNull(r0, r1) ? true : !Powers.Equal(r0.pows, r1.pows); }
        public static bool operator < (Rational r0, Rational r1) { return r0.CompareTo(r1) <  0; }
        public static bool operator > (Rational r0, Rational r1) { return r0.CompareTo(r1) >  0; }
        public static bool operator <=(Rational r0, Rational r1) { return r0.CompareTo(r1) <= 0; }
        public static bool operator >=(Rational r0, Rational r1) { return r0.CompareTo(r1) >= 0; }

        public int CompareTo(Rational other) {
            return this.IsZero()
                ? (other.IsZero() ? 0 : -1)
                : (other.IsZero() ? 1 : 
                    this.IsInfinity()
                        ? (other.IsInfinity() ?  0 : 1)
                        : (other.IsInfinity() ? -1 : Powers.Compare(this.pows, other.pows))
                );
        }

        public Rational Power(int e) {
            return new Rational(Powers.Power(pows, e));
        }

        static public Rational Prime(int primeIndex) {
            var pows = new Pow[primeIndex + 1];
            pows[primeIndex] = (Pow)1;
            return new Rational(pows);
        }
        static public Rational[] Primes(int primeCount) {
            var primes = new Rational[primeCount];
            for (int i = 0; i < primeCount; ++i) {
                primes[i] = Rational.Prime(i);
            }
            return primes;
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

        public static string FormatRationals(Rational[] rs, string separator = ".") {
            if (rs == null) return "";
            return String.Join(separator, rs.Select(r => r.FormatFraction()));
        }

        #region Parse
        public static Rational Parse(string s) {
            if (String.IsNullOrWhiteSpace(s)) return default(Rational);
            s = s.Trim();
            Long n;
            if (Long.TryParse(s, out n) && n > 0) { // an integer
                try {
                    return new Rational(n);
                } catch {
                    return default(Rational);
                }
            }
            if (s.Contains('/') || s.Contains(':')) { // a fraction "n/d" or "n:d"
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

        //!!! replace others to this
        public static Rational[] ParseRationals(string text, string separator = ".") {
            if (String.IsNullOrWhiteSpace(text)) return null;
            string[] parts = text.Split(new[] { separator }, StringSplitOptions.RemoveEmptyEntries);
            Rational[] result = new Rational[parts.Length];
            for (int i = 0; i < parts.Length; ++i) {
                result[i] = Rational.Parse(parts[i]);
                if (result[i].IsDefault()) return null; // null if invalid
            }
            return result;
        }
        #endregion
    }


    public struct RationalX { // extended rational with a sign +/-
        public int sign;
        public Rational rational;
        //
        public RationalX(Long nominator, Long denominator) {
            if (nominator == 0) {
                sign = 0;
                rational = default(Rational);
            } else {
                sign =
                    Utils.Sign(nominator) ==
                    Utils.Sign(denominator) ? 1 : -1;
                rational = new Rational(
                    LongMath.Abs(nominator),
                    LongMath.Abs(denominator)
                );
            }
        }
        public string FormatFraction() {
            if (sign == 0) return "0";
            return (sign < 0 ? "-" : "") + rational.FormatFraction();
        }
    }


    public class EqualDivision { // class used for naming only
        int _stepCount;
        double _stepCents;
        string[] _noteNames = null;
        public EqualDivision(int stepCount, Rational basis) {
            _stepCount = stepCount;
            _stepCents = basis.ToCents() / stepCount;
            if (_stepCents == 12 && basis.Equals(Rational.Two)) {
                _noteNames = "C C# D D# E F F# G G# A A# B B#".Split(' ');
            }
        }
        public string FormatRational(Rational r) {
            return FormatCents(r.ToCents());
        }
        public string FormatCents(double cents) {
            int tone = (int)Math.Round(cents / _stepCents);
            double shift = cents - tone * _stepCents;
            int octave = tone / _stepCount;
            tone = tone % _stepCount;
            return string.Format("{0}{1}{2:+0;-0;+0}c",
                octave == 0 ? "" : String.Format("{0}_", octave),
                _noteNames != null ? _noteNames[tone] : tone.ToString(),
                shift
            );
        }
    }

}
