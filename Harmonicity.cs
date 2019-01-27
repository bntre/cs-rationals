using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

// Harmonic distance

// Based on https://bitbucket.org/bntr/harmony/src/default/harmonicity.py


namespace Rationals
{
    // Interfaces

    public interface IHarmonicity {
        double GetDistance(Rational r);
    }

    public class EulerHarmonicity : IHarmonicity {
        public EulerHarmonicity() { }
        // IHarmonicity
        public double GetDistance(Rational r) {
            return 1.0;
        }
    }

    // Tenney
    // http://www.marcsabat.com/pdfs/MM.pdf
    // HD = log2(ab) ??
    // https://en.xen.wiki/w/Tenney_Height ?

    // Wiseman
    // https://gist.github.com/endolith/118429
    //   A Mathematical Theory of Sensory Harmonics by Gus Wiseman
    //      http://web.archive.org/web/20170212112934/http://www.nafindix.com/math/sensory.pdf
    // Same as https://en.xen.wiki/w/Benedetti_height ?

    // https://en.xen.wiki/w/Hahn_distance

    public class BarlowHarmonicity : IHarmonicity {
        // See: "Musical scale rationalization – a graph-theoretic approach" by Albert Gräf
        //   http://www.musikwissenschaft.uni-mainz.de/Musikinformatik/schriftenreihe/nr45/scale.pdf
        public BarlowHarmonicity() { }
        // IHarmonicity
        public double GetDistance(Rational r) {
            double d = 0.0;
            int[] pows = r.GetPrimePowers();
            for (int i = 0; i < pows.Length; ++i) {
                int e = pows[i];
                int p = Utils.GetPrime(i);
                d += (double)Math.Abs(e) * 2 * Utils.Pow(p - 1, 2) / p;
            }
            return d;
        }
    }

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

            var r = new Rational(node);
            double d = _harmonicity.GetDistance(r);

            var info = new RationalInfo { rational = r, distance = d };
            bool accepted = _handler.Handle(info);
            if (_countLimit != -1 && accepted) {
                _countLimit -= 1;
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
        public bool Handle(RationalInfo r) {
            return _r0 <= r.rational && r.rational <= _r1;
        }
    }

}
