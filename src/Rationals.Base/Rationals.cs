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
            // https://gist.github.com/davidjpfeiffer/155112b11ee243b9b536c6ac70cfcf49
            2,3,5,7,11,13,17,19,23,29,31,37,41,43,47,53,59,61,67,71,73,79,83,89,97,
            101,103,107,109,113,127,131,137,139,149,151,157,163,167,173,179,181,191,193,197,199,
            211,223,227,229,233,239,241,251,257,263,269,271,277,281,283,293,
            307,311,313,317,331,337,347,349,353,359,367,373,379,383,389,397,
            401,409,419,421,431,433,439,443,449,457,461,463,467,479,487,491,499,
            503,509,521,523,541,547,557,563,569,571,577,587,593,599,
            601,607,613,617,619,631,641,643,647,653,659,661,673,677,683,691,
            701,709,719,727,733,739,743,751,757,761,769,773,787,797,
            809,811,821,823,827,829,839,853,857,859,863,877,881,883,887,
            907,911,919,929,937,941,947,953,967,971,977,983,991,997,
            1009,1013,1019,1021,1031,1033,1039,1049,1051,1061,1063,1069,1087,1091,1093,1097,
            1103,1109,1117,1123,1129,1151,1153,1163,1171,1181,1187,1193,
            1201,1213,1217,1223,1229,1231,1237,1249,1259,1277,1279,1283,1289,1291,1297,
            1301,1303,1307,1319,1321,1327,1361,1367,1373,1381,1399,
            1409,1423,1427,1429,1433,1439,1447,1451,1453,1459,1471,1481,1483,1487,1489,1493,1499,
            1511,1523,1531,1543,1549,1553,1559,1567,1571,1579,1583,1597,
            1601,1607,1609,1613,1619,1621,1627,1637,1657,1663,1667,1669,1693,1697,1699,
            1709,1721,1723,1733,1741,1747,1753,1759,1777,1783,1787,1789,
            1801,1811,1823,1831,1847,1861,1867,1871,1873,1877,1879,1889,
            1901,1907,1913,1931,1933,1949,1951,1973,1979,1987,1993,1997,1999,
            2003,2011,2017,2027,2029,2039,2053,2063,2069,2081,2083,2087,2089,2099,
            2111,2113,2129,2131,2137,2141,2143,2153,2161,2179,
            2203,2207,2213,2221,2237,2239,2243,2251,2267,2269,2273,2281,2287,2293,2297,
            2309,2311,2333,2339,2341,2347,2351,2357,2371,2377,2381,2383,2389,2393,2399,
            2411,2417,2423,2437,2441,2447,2459,2467,2473,2477,
            2503,2521,2531,2539,2543,2549,2551,2557,2579,2591,2593,
            2609,2617,2621,2633,2647,2657,2659,2663,2671,2677,2683,2687,2689,2693,2699,
            2707,2711,2713,2719,2729,2731,2741,2749,2753,2767,2777,2789,2791,2797,
            2801,2803,2819,2833,2837,2843,2851,2857,2861,2879,2887,2897,
            2903,2909,2917,2927,2939,2953,2957,2963,2969,2971,2999,
            3001,3011,3019,3023,3037,3041,3049,3061,3067,3079,3083,3089,
            3109,3119,3121,3137,3163,3167,3169,3181,3187,3191,
            3203,3209,3217,3221,3229,3251,3253,3257,3259,3271,3299,
            3301,3307,3313,3319,3323,3329,3331,3343,3347,3359,3361,3371,3373,3389,3391,
            3407,3413,3433,3449,3457,3461,3463,3467,3469,3491,3499,
            3511,3517,3527,3529,3533,3539,3541,3547,3557,3559,3571,3581,3583,3593,
            3607,3613,3617,3623,3631,3637,3643,3659,3671,3673,3677,3691,3697,
            3701,3709,3719,3727,3733,3739,3761,3767,3769,3779,3793,3797,
            3803,3821,3823,3833,3847,3851,3853,3863,3877,3881,3889,
            3907,3911,3917,3919,3923,3929,3931,3943,3947,3967,3989,
            4001,4003,4007,4013,4019,4021,4027,4049,4051,4057,4073,4079,4091,4093,4099,
            4111,4127,4129,4133,4139,4153,4157,4159,4177,
            4201,4211,4217,4219,4229,4231,4241,4243,4253,4259,4261,4271,4273,4283,4289,4297,
            4327,4337,4339,4349,4357,4363,4373,4391,4397,
            4409,4421,4423,4441,4447,4451,4457,4463,4481,4483,4493,
            4507,4513,4517,4519,4523,4547,4549,4561,4567,4583,4591,4597,
            4603,4621,4637,4639,4643,4649,4651,4657,4663,4673,4679,4691,
            4703,4721,4723,4729,4733,4751,4759,4783,4787,4789,4793,4799,
            4801,4813,4817,4831,4861,4871,4877,4889,
            4903,4909,4919,4931,4933,4937,4943,4951,4957,4967,4969,4973,4987,4993,4999,
            5003,5009,5011,5021,5023,5039,5051,5059,5077,5081,5087,5099,
            5101,5107,5113,5119,5147,5153,5167,5171,5179,5189,5197,
            5209,5227,5231,5233,5237,5261,5273,5279,5281,5297,
            5303,5309,5323,5333,5347,5351,5381,5387,5393,5399,
            5407,5413,5417,5419,5431,5437,5441,5443,5449,5471,5477,5479,5483,
            5501,5503,5507,5519,5521,5527,5531,5557,5563,5569,5573,5581,5591,
            5623,5639,5641,5647,5651,5653,5657,5659,5669,5683,5689,5693,
            5701,5711,5717,5737,5741,5743,5749,5779,5783,5791,
            5801,5807,5813,5821,5827,5839,5843,5849,5851,5857,5861,5867,5869,5879,5881,5897,
            5903,5923,5927,5939,5953,5981,5987,
            6007,6011,6029,6037,6043,6047,6053,6067,6073,6079,6089,6091,
            6101,6113,6121,6131,6133,6143,6151,6163,6173,6197,6199,
            6203,6211,6217,6221,6229,6247,6257,6263,6269,6271,6277,6287,6299,
            6301,6311,6317,6323,6329,6337,6343,6353,6359,6361,6367,6373,6379,6389,6397,
            6421,6427,6449,6451,6469,6473,6481,6491,
            6521,6529,6547,6551,6553,6563,6569,6571,6577,6581,6599,
            6607,6619,6637,6653,6659,6661,6673,6679,6689,6691,
            6701,6703,6709,6719,6733,6737,6761,6763,6779,6781,6791,6793,
            6803,6823,6827,6829,6833,6841,6857,6863,6869,6871,6883,6899,
            6907,6911,6917,6947,6949,6959,6961,6967,6971,6977,6983,6991,6997,
            7001,7013,7019,7027,7039,7043,7057,7069,7079,
            7103,7109,7121,7127,7129,7151,7159,7177,7187,7193,
            7207,7211,7213,7219,7229,7237,7243,7247,7253,7283,7297,
            7307,7309,7321,7331,7333,7349,7351,7369,7393,
            7411,7417,7433,7451,7457,7459,7477,7481,7487,7489,7499,
            7507,7517,7523,7529,7537,7541,7547,7549,7559,7561,7573,7577,7583,7589,7591,
            7603,7607,7621,7639,7643,7649,7669,7673,7681,7687,7691,7699,
            7703,7717,7723,7727,7741,7753,7757,7759,7789,7793,
            7817,7823,7829,7841,7853,7867,7873,7877,7879,7883,
            7901,7907,7919
        };

        //!!! add a generator

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

        public Long ToInt() {
            return Powers.ToInt(pows);
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

        public static string FormatRationals(Rational[] rs, string separator = ".", string invalidItem = "") {
            if (rs == null) return "";
            return String.Join(separator, rs.Select(r => r.FormatFraction() ?? invalidItem));
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
