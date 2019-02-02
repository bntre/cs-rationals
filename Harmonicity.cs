using System;
using System.Collections.Generic;
using System.Linq;
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
            return Math.Log(f.N * f.D);
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

    public class RationalIterator : Grid.IGridNodeHandler, IIterator<RationalInfo> {
        private IHarmonicity _harmonicity;
        private int _countLimit;
        private int _levelLimit;
        private IHandler<RationalInfo> _handler;
        //
        public RationalIterator(IHarmonicity harmonicity, int countLimit = -1, int levelLimit = -1) {
            _harmonicity = harmonicity;
            _countLimit = countLimit;
            _levelLimit = levelLimit;
        }
        public double HandleGridNode(int[] node) {
            // stop growing grid if limits reached
            if (_countLimit != -1 && _countLimit == 0) return -1;
            if (_levelLimit != -1 && node.Length > _levelLimit) return -1;

            Rational r = new Rational(node);
            double d = _harmonicity.GetDistance(r);

            var info = new RationalInfo { rational = r, distance = d };
            int result = _handler.Handle(info); // -1, 0 ,1
            if (result == -1) return -1; // stop
            if (result == 1) { // node accepted
                if (_countLimit != -1) _countLimit -= 1;
            }
            return d;
        }
        public void Iterate(IHandler<RationalInfo> handler) {
            _handler = handler; //!!! make it thread safe
            Grid.Iterate(this);
        }
    }

    public class RangeRationalHandler : IHandler<RationalInfo> {
        private Rational _r0;
        private Rational _r1;
        public RangeRationalHandler(Rational r0, Rational r1) {
            _r0 = r0;
            _r1 = r1;
        }
        public int Handle(RationalInfo r) {
            return (_r0 <= r.rational && r.rational <= _r1) ? 1 : 0;
        }
    }

}
