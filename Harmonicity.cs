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



    public class RationalIterator : Grid.IGridNodeHandler {
        private IHarmonicity _harmonicity;
        private IHandler<Rational> _handler;
        private int _countLimit;
        private int _levelLimit;
        //
        public RationalIterator(IHarmonicity harmonicity, IHandler<Rational> handler, int countLimit = -1, int levelLimit = -1) {
            _harmonicity = harmonicity;
            _handler = handler;
            _countLimit = countLimit;
            _levelLimit = levelLimit;
        }
        public double HandleGridNode(int[] node) {
            if (_countLimit != -1 && _countLimit == 0) return -1;
            if (_levelLimit != -1 && node.Length > _levelLimit) return -1;
            var r = new Rational(node);
            double distance = _harmonicity.GetDistance(r);
            //if (distance > _distanceLimit) return -1;

            r = _handler.Handle(r);
            if (_countLimit != -1 && r != null) {
                _countLimit -= 1;
            }
            
            return distance;
        }
        public void Iterate() {
            Grid.Iterate(this);
        }
    }

    public class RangeRationalHandler : IHandler<Rational> {
        private Rational _r0;
        private Rational _r1;
        public RangeRationalHandler(Rational r0, Rational r1) {
            _r0 = r0;
            _r1 = r1;
        }
        public Rational Handle(Rational r) {
            return (_r0 <= r && r <= _r1) ? r : null;
        }
    }

}
