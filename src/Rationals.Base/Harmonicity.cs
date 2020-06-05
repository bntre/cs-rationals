using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rationals
{
#if USE_CHAR_POWERS
    using Pow = System.Char;
#else
    using Pow = System.Int32;
#endif

    // Interfaces

    // use integer distance for peformance !!!

    public interface IHarmonicity { //!!! change to base class Harmonicity and move GetHarmonicity here
        double GetDistance(Rational r);
    }

    // Euler
    // https://en.wikipedia.org/wiki/Leonhard_Euler#Music Gradus suavitatis
    //  Euler further used the principle of the "exponent" to propose a derivation of the gradus suavitatis (degree of suavity, of agreeableness) of intervals and chords from their prime factors
    public class EulerHarmonicity : IHarmonicity {
        public double GetDistance(Rational r) {
            double d = 1.0;
            int[] pows = r.GetPrimePowers();
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                int p = Utils.GetPrime(i);
                d += Math.Abs(e) * (p-1);
            }
            return d;
        }
    }

    // Clarence Barlow
    // http://www.musikinformatik.uni-mainz.de/schriftenreihe/nr45/scale.pdf
    //  "Musical scale rationalization – a graph-theoretic approach" by Albert Gräf
    //   -> C. Barlow. "On the quantification of harmony and metre". In C. Barlow, editor, The Ratio Book, Feedback Papers 43, pages 2–23. Feedback Publishing Company, Cologne, 2001.
    //      https://www.mat.ucsb.edu/Publications/Quantification_of_Harmony_and_Metre.pdf

    public class BarlowHarmonicity : IHarmonicity {
        public BarlowHarmonicity() { }
        public double GetDistance(Rational r) {
            double d = 0.0;
            int[] pows = r.GetPrimePowers();
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                int p = Utils.GetPrime(i);
                d += 
                    Math.Abs(e) 
                    //* Math.Exp(-0.04 * e) -- not commutative..
                    * 2.0*(p-1)*(p-1)/p;
            }
            return d;
        }
    }

    // James Tenney
    // https://en.xen.wiki/w/Tenney_Height
    // http://www.plainsound.org/pdfs/JC&ToH.pdf John Cage and the Theory of Harmony by James Tenney, 1983
    public class TenneyHarmonicity : IHarmonicity {
        public TenneyHarmonicity() { }
        public double GetDistance(Rational r) {
            var f = r.ToFraction();
            //return Math.Log((double)(f.N * f.D));
            return Math.Log((double)f.N * (double)f.D);
            // = log(2^|e2| * 3^|e3| * ... * p^|ep|)
        }
    }


    // https://en.xen.wiki/w/Hahn_distance




    public class SimpleHarmonicity : IHarmonicity {
        private double _exp;
        public SimpleHarmonicity(double exp) { _exp = exp; }
        // IHarmonicity
        public double GetDistance(Rational r) {
            double d = 0.0;
            int[] pows = r.GetPrimePowers();
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                int p = Utils.GetPrime(i);
                d += e*e * Math.Pow(p, _exp);
            }
            return d;
        }
    }

    public class EpimoricHarmonicity : IHarmonicity {
        private double _exp;
        public EpimoricHarmonicity(double exp) { _exp = exp; }
        // IHarmonicity
        public double GetDistance(Rational r) {
            double d = 0.0;
            int[] pows = r.GetEpimoricPowers();
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                int p = Utils.GetPrime(i);
                d += e*e * Math.Pow(p, _exp);
            }
            return d;
        }
    }

    /*
    public class NarrowHarmonicity : IHarmonicity {
        private double _exp;
        public NarrowHarmonicity(double exp) {
            _exp = exp;
        }
        // IHarmonicity
        public double GetDistance(Rational r) {
            double d = 0.0;
            int[] pows = r.GetNarrowPowers();
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                if (e != 0) {
                    int p = Utils.GetPrime(i);
                    d += e * e * Math.Pow(p, _exp);
                }
            }
            d = Math.Sqrt(d);
            return d;
        }
    }
    */

    public static partial class Utils {
        public static string[] HarmonicityNames = new[] {
            "Barlow",
            "Euler",
            "Tenney",
        };
        public static IHarmonicity CreateHarmonicity(string name, bool normalize = false) {
            IHarmonicity h = null;
            switch (name) {
                case null:
                case "":
                case "Barlow": h = new BarlowHarmonicity(); break; // also default
                case "Euler":  h = new EulerHarmonicity();  break;
                case "Tenney": h = new TenneyHarmonicity(); break;
                default: throw new Exception("Unknown Harmonicity: " + name);
            }
            if (normalize) {
                h = new HarmonicityNormalizer(h);
            }
            return h;
        }
        public static float GetHarmonicity(double distance) { //!!! make configurable? move to IHarmonicity
            return (float)Math.Exp(-distance * 1.2); // 0..1
        }
    }

    public class HarmonicityNormalizer : IHarmonicity { //!!! slow - get rid of this
        private IHarmonicity _harmonicity;
        private double _distanceFactor;
        public HarmonicityNormalizer(IHarmonicity harmonicity) {
            _harmonicity = harmonicity;
            _distanceFactor = 1.0 / _harmonicity.GetDistance(new Rational(81, 80));
        }
        public double GetDistance(Rational r) {
            return _harmonicity.GetDistance(r) * _distanceFactor;
        }
    }



    public class RationalInfo { //!!! make this struct for performance?
        public Rational rational;
        public double distance;
        public object additionalData;
        //
        public static int CompareDistances(RationalInfo r0, RationalInfo r1) {
            return r0.distance.CompareTo(r1.distance);
        }
        public static int CompareValues(RationalInfo r0, RationalInfo r1) {
            return Powers.Compare(
                r0.rational.GetPrimePowers(), 
                r1.rational.GetPrimePowers()
            );
        }
    }

    public class RationalGenerator
    {
        // 1 - accepted, 0 - rejected (don't count it), -1 - stop the branch
        public delegate int HandleRational(Rational r, double distance);

        private IHarmonicity _harmonicity;
        private Limits _limits;
        private Pow[][] _subgroup = null;
        private int _rationalCounter;
        private HandleRational _handleRational = null;
        //
        public struct Limits {
            public int rationalCount;
            public int dimensionCount;
            public double distance;
        }
        //
        public RationalGenerator(IHarmonicity harmonicity, Limits limits, Rational[] subgroup = null) {
            _harmonicity = harmonicity;
            _limits = limits;
            if (subgroup != null) {
                _subgroup = new Pow[subgroup.Length][];
                for (int i = 0; i < subgroup.Length; ++i) {
                    _subgroup[i] = Powers.Clone(subgroup[i].GetPrimePowers());
                }
            }
        }

        public void Iterate(HandleRational handleRational) {
            _handleRational = handleRational;
            Coordinates.Iterate(this.HandleCoordinates);
            _handleRational = null;
        }

        private Rational MakeRational(int[] coordinates) {
            if (_subgroup == null) {
                return new Rational(coordinates);
            } else {
                Pow[] r = new Pow[] { };
                int len = Math.Min(coordinates.Length, _subgroup.Length);
                for (int i = 0; i < len; ++i) {
                    r = Powers.Mul(r, Powers.Power(_subgroup[i], coordinates[i]));
                }
                return new Rational(r);
            }
        }

        private double HandleCoordinates(int[] coordinates) {
            // return positive distance or -1 to stop growing the branch

            // stop growing grid if limits reached
            if (_limits.rationalCount != -1 && _rationalCounter >= _limits.rationalCount) return -1; // stop the branch
            if (_limits.dimensionCount != -1 && coordinates.Length > _limits.dimensionCount) return -1; // stop the branch

            Rational r = MakeRational(coordinates);
            double d = _harmonicity.GetDistance(r);

            if (_limits.distance >= 0 && d > _limits.distance) return -1; // stop the branch -- !!! can we be sure there are no closer distance rationals in this branch?

            int result = _handleRational(r, d); // 1, 0, -1
            if (result == -1) return -1; // stop the branch
            if (result == 1) _rationalCounter++; // node accepted
            return d;
        }
    }

    public class RationalIterator : RationalGenerator { //!!! ugly wrapper
        private IHandler<RationalInfo> _handler;
        //
        public RationalIterator(IHarmonicity harmonicity, Limits limits, Rational[] subgroup, IHandler<RationalInfo> handler) 
            : base(harmonicity, limits, subgroup)
        {
            _handler = handler;
        }
        //
        protected int Handle(Rational r, double distance) {
            return _handler.Handle(
                new RationalInfo {
                    rational = r,
                    distance = distance,
                }
            );
        }

        public void Iterate() {
            Iterate(this.Handle);
        }
    }

    //!!! move out
    public class RangeRationalHandler : IHandler<RationalInfo> {
        private Rational _r0;
        private Rational _r1;
        private bool _inc0;
        private bool _inc1;
        public RangeRationalHandler(Rational r0, Rational r1, bool inc0 = true, bool inc1 = true) {
            _r0 = r0;
            _r1 = r1;
            _inc0 = inc0;
            _inc1 = inc1;
        }
        public int Handle(RationalInfo r) {
            try {
                if (_inc0) {
                    if (r.rational < _r0) return 0;
                } else {
                    if (r.rational <= _r0) return 0;
                }
                if (_inc1) {
                    if (_r1 < r.rational) return 0;
                } else {
                    if (_r1 <= r.rational) return 0;
                }
            } catch (OverflowException) {
                // overflow on comparison rationals
                // probably the rational out of range - we are losing it !!!
                // "#define USE_BIGINTEGER" to avoid this
                return 0;
            }
            return 1;
        }
    }

}
