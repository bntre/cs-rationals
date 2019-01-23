using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

// Rewrite: https://bitbucket.org/bntr/harmony/src/default/rationals.py

namespace Rationals
{
    public static class Utils 
    {
        // Math

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
        }

        public static long Pow(long n, long e) {
            if (e < 0) throw new Exception("Negative power");
            if (e == 0) return 1;
            if (e == 1) return n;
            return n * Pow(n, --e); //!!! optimize
        }
    }



    // Raw int[] powers utils
    public static class Powers
    {
        private static int SafeAt(int[] pows, int i) {
            return i < pows.Length ? pows[i] : 0;
        }
        private static int[] MaxLength(int[] p0, int[] p1) {
            return new int[Math.Max(p0.Length, p1.Length)];
        }

        public static int[] Clone(int[] p) {
            int[] r = new int[p.Length];
            p.CopyTo(r, 0);
            return r;
        }

        public static string ToString(int[] pows, string brackets = "{}") {
            string s = brackets.Substring(0, 1);
            for (int i = 0; i < pows.Length; ++i) {
                //if (i != 0) s += ",";
                //s += pows[i].ToString();
                s += pows[i].ToString("+0;-0");
            }
            s += brackets.Substring(1);
            return s;
        }

        //!!! optimize avoiding trailing zeros {xxx,0,0,0,0,0,0,0,0}

        public static int[] Mul(int[] p0, int[] p1) {
            int[] pows = MaxLength(p0, p1);
            for (int i = 0; i < pows.Length; ++i) {
                pows[i] = SafeAt(p0, i) + SafeAt(p1, i);
            }
            return pows;
        }

        public static int[] Div(int[] p0, int[] p1) {
            int[] pows = MaxLength(p0, p1);
            for (int i = 0; i < pows.Length; ++i) {
                pows[i] = SafeAt(p0, i) - SafeAt(p1, i);
            }
            return pows;
        }

        public static int[] Pow(int[] p, int e) {
            int[] pows = new int[p.Length];
            for (int i = 0; i < pows.Length; ++i) {
                pows[i] = p[i] * e;
            }
            return pows;
        }

        public static bool Equal(int[] p0, int[] p1) {
            int l = Math.Max(p0.Length, p1.Length);
            for (int i = 0; i < l; ++i) {
                if (SafeAt(p0, i) != SafeAt(p1, i)) return false;
            }
            return true;
        }
        public static int Compare(int[] p0, int[] p1) {
            long n, d;
            ToFraction(Div(p0, p1), out n, out d);
            return n.CompareTo(d);
        }

        public static int[] FromFraction(long n, long d) {
            return Div(FromInt(n), FromInt(d));
        }

        public static void ToFraction(int[] pows, out long n, out long d) {
            int[] ns = new int[pows.Length];
            int[] ds = new int[pows.Length];
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                ns[i] = e > 0 ?  e : 0;
                ds[i] = e < 0 ? -e : 0;
            }
            n = ToInt(ns);
            d = ToInt(ds);
        }

        public static int[] FromInt(long n) {
            if (n <= 0) throw new ArgumentException();
            var pows = new List<int>();
            for (int i = 0; n != 1; ++i) {
                int p = Utils.GetPrime(i);
                int e = 0;
                while (n % p == 0) { //!!! use DivRem
                    n /= p;
                    e += 1;
                }
                pows.Add(e);
            }
            return pows.ToArray();
        }

        public static long ToInt(int[] pows) {
            long n = 1;
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                if (e == 0) continue;
                if (e < 0) throw new Exception("Negative powers - this rational is not an integer");
                n *= Utils.Pow(Utils.GetPrime(i), e);
            }
            return n;
        }

        public static double ToDouble(int[] pows) {
            double d = 1.0;
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                if (e == 0) continue;
                d *= Math.Pow(Utils.GetPrime(i), e);
            }
            return d;
        }

    }



    public class Rational
    {
        private int[] pows;

        public Rational(int nominator, int denominator = 1) {
            this.pows = Powers.FromFraction(nominator, denominator);
        }
        public Rational(int[] primePowers) {
            this.pows = primePowers;
        }
        public Rational(Rational r) {
            this.pows = Powers.Clone(r.pows);
        }

        public Rational Clone() {
            return new Rational(pows);
        }

        public int[] GetPrimePowers() {
            return pows;
        }

        public string FormatFraction() {
            long n, d;
            Powers.ToFraction(pows, out n, out d);
            string s = n.ToString();
            if (d != 1) s += "/" + d.ToString();
            return s;
        }

        public override string ToString() {
            return FormatFraction();
        }

        public string PowersToString() {
            return Powers.ToString(pows);
        }

        public double ToCents() {
            return Math.Log(Powers.ToDouble(pows), 2) * 1200.0;
        }


        // Operators
        public static Rational operator *(Rational r0, Rational r1) { return new Rational(Powers.Mul(r0.pows, r1.pows)); }
        public static Rational operator /(Rational r0, Rational r1) { return new Rational(Powers.Div(r0.pows, r1.pows)); }
        //!!! troubles with null != null
        //private static bool SomeNull(Rational r0, Rational r1) { return object.ReferenceEquals(r0, null) || object.ReferenceEquals(r1, null); }
        //public static bool operator ==(Rational r0, Rational r1) { return SomeNull(r0, r1) ? false : Powers.Equal(r0.pows, r1.pows); }
        //public static bool operator !=(Rational r0, Rational r1) { return SomeNull(r0, r1) ? true : !Powers.Equal(r0.pows, r1.pows); }
        public static bool operator < (Rational r0, Rational r1) { return Powers.Compare(r0.pows, r1.pows) <  0; }
        public static bool operator > (Rational r0, Rational r1) { return Powers.Compare(r0.pows, r1.pows) >  0; }
        public static bool operator <=(Rational r0, Rational r1) { return Powers.Compare(r0.pows, r1.pows) <= 0; }
        public static bool operator >=(Rational r0, Rational r1) { return Powers.Compare(r0.pows, r1.pows) >= 0; }

        public Rational Pow(int e) {
            return new Rational(Powers.Pow(pows, e));
        }

        static public Rational Prime(int primeIndex) {
            var pows = new int[primeIndex + 1];
            pows[primeIndex] = 1;
            return new Rational(pows);
        }

        #region Epimorics
        // We use p/(p-1) ratios (p is prime). See:
        //  https://en.wikipedia.org/wiki/Superparticular_ratio
        //  https://en.wikipedia.org/wiki/Leibniz_formula_for_%CF%80#Euler_product

        public int[] GetEpimoricPowers() {
            int len = pows.Length;
            int[] res = new int[len];
            var r = this.Clone();
            for (int i = len - 1; i >= 0; --i) {
                int p = Utils.GetPrime(i);
                int e = r.GetPrimePowers()[i];
                res[i] = e;
                if (e != 0) r /= new Rational(p, p-1).Pow(e);
            }
            return res;
        }


        #endregion
    }



    public class Temperament {
        int _equalSteps;
        double _stepCents;
        string[] _noteNames = null;
        public Temperament(int equalSteps) {
            _equalSteps = equalSteps;
            _stepCents = 1200.0 / equalSteps;
            if (_stepCents == 12) {
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

}
