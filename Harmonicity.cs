using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rationals
{
    // Interfaces

    public interface IHarmonicity {
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

    // Barlow
    // http://www.musikwissenschaft.uni-mainz.de/Musikinformatik/schriftenreihe/nr45/scale.pdf "Musical scale rationalization – a graph-theoretic approach" by Albert Gräf
    public class BarlowHarmonicity : IHarmonicity {
        public BarlowHarmonicity() { }
        public double GetDistance(Rational r) {
            double d = 0.0;
            int[] pows = r.GetPrimePowers();
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                int p = Utils.GetPrime(i);
                d += Math.Abs(e) * 2.0*(p-1)*(p-1)/p;
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
            return Math.Log((double)(f.N * f.D));
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

    public static partial class Utils {
        public static string[] HarmonicityNames = new[] {
            "Barlow",
            "Euler",
            "Tenney",
        };
        public static IHarmonicity CreateHarmonicity(string name) {
            switch (name) {
                case null:
                case "Barlow": return new BarlowHarmonicity(); // also default
                case "Euler":  return new EulerHarmonicity();
                case "Tenney": return new TenneyHarmonicity();
                default: throw new Exception("Unknown Harmonicity: " + name);
            }
        }
    }

    public class HarmonicityNormalizer : IHarmonicity {
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

    public class RationalIterator : Coordinates.ICoordinateHandler, IIterator<RationalInfo> {
        private IHarmonicity _harmonicity;
        private int _rationalCountLimit;
        private int _dimensionCountLimit;
        private double _distanceLimit;
        private Rational[] _subgroup;
        private IHandler<RationalInfo> _handler;
        //
        public RationalIterator(IHarmonicity harmonicity, int rationalCountLimit = -1, double distanceLimit = -1, int dimensionCountLimit = -1, Rational[] subgroup = null) {
            _harmonicity = harmonicity;
            _rationalCountLimit = rationalCountLimit;
            _distanceLimit = distanceLimit;
            _subgroup = subgroup;
            _dimensionCountLimit = dimensionCountLimit;
        }

        private Rational MakeRational(int[] coordinates) {
            if (_subgroup == null) {
                return new Rational(coordinates);
            } else {
                var r = new Rational(1);
                int len = Math.Min(coordinates.Length, _subgroup.Length);
                for (int i = 0; i < len; ++i) {
                    r *= _subgroup[i].Pow(coordinates[i]);
                }
                return r;
            }
        }

        public double HandleCoordinates(int[] coordinates) {
            // return positive distance or -1 to stop growing the branch

            // stop growing grid if limits reached
            if (_rationalCountLimit != -1 && _rationalCountLimit == 0) return -1; // stop the branch
            if (_dimensionCountLimit != -1 && coordinates.Length > _dimensionCountLimit) return -1; // stop the branch

            Rational r = MakeRational(coordinates);
            double d = _harmonicity.GetDistance(r);

            if (_distanceLimit >= 0 && d > _distanceLimit) return -1; // stop the branch -- !!! can we be sure there are no closer distance rationals in this branch?

            var info = new RationalInfo { rational = r, distance = d };
            int result = _handler.Handle(info); // -1, 0 ,1
            if (result == -1) return -1; // stop the branch
            if (result == 1) { // node accepted
                if (_rationalCountLimit != -1) _rationalCountLimit -= 1;
            }
            return d;
        }
        public void Iterate(IHandler<RationalInfo> handler) {
            _handler = handler; //!!! make it thread safe
            Coordinates.Iterate(this);
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
                return 0;
            }
            return 1;
        }
    }

}
