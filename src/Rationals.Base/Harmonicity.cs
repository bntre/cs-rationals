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
        double GetHarmonicity(Rational r);
    }

    public abstract class HarmonicityBase : IHarmonicity {
        public abstract double GetDistance(Rational r);
        public double GetHarmonicity(Rational r) {
            double distance = GetDistance(r);
            return HarmonicityUtils.GetHarmonicity(distance);
        }
    }

    // Euler
    // https://en.wikipedia.org/wiki/Leonhard_Euler#Music "Gradus suavitatis"
    //  Euler further used the principle of the "exponent" to propose a derivation of the gradus suavitatis (degree of suavity, of agreeableness) of intervals and chords from their prime factors
    public class EulerHarmonicity : HarmonicityBase {
        public override double GetDistance(Rational r) {
            double d = 1.0;
            int[] pows = r.GetPrimePowers();
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                if (e != 0) {
                    int p = Utils.GetPrime(i);
                    d +=
                        (double)Math.Abs(e) 
                        //    * Math.Exp(-e * 0.0001) // make a bit non-commutative to fix the order (avoiding blinking on count increase) - seems needed only if use "overage" in GridDrawer.GenerateItems
                        // / (p-1); -- backward..
                        * (p-1);
                }
            }
            return d;
        }
    }

    // Clarence Barlow
    // http://www.musikinformatik.uni-mainz.de/schriftenreihe/nr45/scale.pdf
    //  "Musical scale rationalization – a graph-theoretic approach" by Albert Gräf
    //   -> C. Barlow. "On the quantification of harmony and metre". In C. Barlow, editor, The Ratio Book, Feedback Papers 43, pages 2–23. Feedback Publishing Company, Cologne, 2001.
    //      https://www.mat.ucsb.edu/Publications/Quantification_of_Harmony_and_Metre.pdf

    public class BarlowHarmonicity : HarmonicityBase {
        public override double GetDistance(Rational r) {
            double d = 0.0;
            int[] pows = r.GetPrimePowers();
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                if (e != 0) {
                    int p = Utils.GetPrime(i);
                    d += Math.Abs(e) * 2.0*(p-1)*(p-1)/p;
                }
            }
            return d;
        }
    }

    // James Tenney
    // https://en.xen.wiki/w/Tenney_Height
    // http://www.plainsound.org/pdfs/JC&ToH.pdf John Cage and the Theory of Harmony by James Tenney, 1983
    public class TenneyHarmonicity : HarmonicityBase {
        public override double GetDistance(Rational r) {
            var f = r.ToFraction();
            //return Math.Log((double)(f.N * f.D));
            return Math.Log((double)f.N * (double)f.D);
            // = log(2^|e2| * 3^|e3| * ... * p^|ep|)
        }
    }

    /*
    public class NarrowHarmonicity : HarmonicityBase {
        public static Rational[] Narrows = null; //!!! temporal - here should be some general harmonicity context to create
        // IHarmonicity
        public override double GetDistance(Rational r) {
            double d = 0.0;
            int[] npows = r.GetNarrowPowers(Narrows);
            for (int i = 0; i < npows.Length; ++i) {
                int e = npows[i];
                if (e != 0) {
                    int p = Utils.GetPrime(i);
                    d += (double)Math.Abs(e) / (p-1); // like Euler
                    //d += Math.Abs(e) * 2.0*(p-1)*(p-1)/p; // like Barlow
                }
            }
            return d;
        }
    }
    */

    /*
    public class BackwardHarmonicity : HarmonicityBase {
        public override double GetDistance(Rational r) {
            double d = 1.0;
            int[] pows = r.GetPrimePowers();
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                int p = Utils.GetPrime(i);
                d += Math.Abs(e) / (p-1);
            }
            return d;
        }
    }

    // https://en.wikipedia.org/wiki/Taxicab_geometry
    public class ManhattanHarmonicity : HarmonicityBase {
        public override double GetDistance(Rational r) {
            double d = 0.0;
            int[] pows = r.GetPrimePowers();
            for (int i = 0; i < pows.Length; ++i) {
                d += Math.Abs(pows[i]);
            }
            return d;
        }
    }
    */

    public class EuclideanHarmonicity : HarmonicityBase {
        public override double GetDistance(Rational r) {
            double[] coefs = new double[] { 
                //1.0, 1.1, 1.2 
                //1.0, 0.9, 0.8
            };
            double d = 0.0;
            //int[] pows = r.GetPrimePowers();
            int[] pows = r.GetNarrowPowers();
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                double c = i < coefs.Length ? coefs[i] : 1.0;
                d += 
                    e*e
                    * Math.Exp(-e * 0.01) // make a bit non-commutative to fix the order (avoiding blinking on count increase) - seems needed only if use "overage" in GridDrawer.GenerateItems
                    * Math.Exp( i * 0.011)
                    * c;
            }
            return Math.Sqrt(d);
        }
    }



    // https://en.xen.wiki/w/Hahn_distance



#if false // dead code ?
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

    /*
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
    */
#endif

    public class HarmonicityNormalizer : HarmonicityBase {
        private IHarmonicity _harmonicity;
        private double _distanceFactor;
        private static Rational _defaultSample = new Rational(81, 80);
        public HarmonicityNormalizer(IHarmonicity harmonicity, Rational sample = default(Rational)) {
            _harmonicity = harmonicity;
            if (sample.IsDefault()) sample = _defaultSample;
            _distanceFactor = 1.0 / _harmonicity.GetDistance(sample);
        }
        public override double GetDistance(Rational r) {
            return _harmonicity.GetDistance(r) * _distanceFactor;
        }
    }

    public static class HarmonicityUtils {
        public static string[] HarmonicityNames = new[] {
            "Barlow",
            "Euler",
            "Tenney",
            "Euclidean",
            //"Manhattan",
            //"Backward",
            //"Narrow",
        };
        public static IHarmonicity CreateHarmonicity(string name, bool normalize = false) {
            IHarmonicity h = null;
            switch (name) {
                case null:
                case "":
                case "Barlow":    h = new BarlowHarmonicity();    break; // also default
                case "Euler":     h = new EulerHarmonicity();     break;
                case "Tenney":    h = new TenneyHarmonicity();    break;
                case "Euclidean": h = new EuclideanHarmonicity(); break;
                //case "Manhattan": h = new ManhattanHarmonicity(); break;
                //case "Backward":  h = new BackwardHarmonicity();  break;
                //case "Narrow": h = new NarrowHarmonicity();       break;
                default:
                    //throw new Exception("Unknown Harmonicity: " + name);
                    return CreateHarmonicity(null, normalize); // create default
            }
            if (normalize) {
                h = new HarmonicityNormalizer(h);
            }
            return h;
        }
        public static double GetHarmonicity(double distance) { //!!! make configurable?
            return Math.Exp(-distance * 1.2); // 0..1
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
        private Pow[][] _customBasis = null; // e.g. subgroup or narrow primes
        private int _rationalCounter;
        private HandleRational _handleRational = null;
        //
        public struct Limits {
            public int rationalCount;
            public int dimensionCount;
            public double distance;
        }
        //
        public RationalGenerator(IHarmonicity harmonicity, Limits limits, Rational[] customBasis = null) {
            _harmonicity = harmonicity;
            _limits = limits;
            if (customBasis != null) {
                _customBasis = new Pow[customBasis.Length][];
                for (int i = 0; i < customBasis.Length; ++i) {
                    _customBasis[i] = Powers.Clone(customBasis[i].GetPrimePowers());
                }
            }
        }

        public void Iterate(HandleRational handleRational) {
            _handleRational = handleRational;
            Coordinates.Iterate(this.HandleCoordinates);
            _handleRational = null;
        }

        private Rational MakeRational(int[] coordinates) {
            if (_customBasis == null) {
                return new Rational(coordinates);
            } else {
                Pow[] r = new Pow[] { };
                int len = Math.Min(coordinates.Length, _customBasis.Length);
                for (int i = 0; i < len; ++i) {
                    r = Powers.Mul(r, Powers.Power(_customBasis[i], coordinates[i]));
                }
                return new Rational(r);
            }
        }

        private double HandleCoordinates(int[] coordinates) {
            // return positive distance or -1 to stop growing the branch

            // stop growing grid if limits reached
            if (_limits.rationalCount != -1 && _rationalCounter >= _limits.rationalCount) return -1; // stop the branch
            if (_limits.dimensionCount != -1 && coordinates.Length > _limits.dimensionCount) return -1; // stop the branch

            Rational r = MakeRational(coordinates); // e.g. by customBasis
            double d = _harmonicity.GetDistance(r);

            if (_limits.distance >= 0 && d > _limits.distance) return -1; // stop the branch -- !!! can we be sure there are no closer distance rationals in this branch?

            int result = _handleRational(r, d); // 1, 0, -1
            if (result == -1) return -1; // stop this branch
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
