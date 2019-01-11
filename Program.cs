using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Diagnostics;


namespace Rationals {

    class RationalPrinter : IRationalHandler {
        IHarmonicity _harmonicity;
        Temperament _temperament;
        public RationalPrinter(IHarmonicity harmonicity) {
            _harmonicity = harmonicity;
            _temperament = new Temperament(12);
        }
        public Rational HandleRational(Rational r) {
            double distance = _harmonicity.GetDistance(r);
            Debug.WriteLine("{0,7} {1,-12} {2,7} {3,10:F2} {4,15}",
                r,
                r.PowersToString(),
                distance,
                r.ToCents(),
                _temperament.FormatRational(r)
            );
            return null;
        }
    }


    class Program {

        static void Test1() {
            var r0 = new Rational(4, 5);
            var r1 = new Rational(6, 4);
            Debug.WriteLine("{0} * {1} -> {2}", r0, r1, r0 * r1);
            Debug.WriteLine("{0} / {1} -> {2}", r0, r1, r0 / r1);
        }

        static void Test2() {
            //var harmonicity = new BarlowHarmonicity();
            var harmonicity = new SimpleHarmonicity(2.0);
            double distanceLimit = harmonicity.GetDistance(new Rational(11, 10));
            int primeIndexLimit = 3;

            var r0 = new Rational(1);
            var r1 = new Rational(2);
            var handler = new RationalHandlerPipe(
                new RangeRationalHandler(r0, r1),
                new RationalPrinter(harmonicity)
            );

            Debug.WriteLine("Iterate {0} range {1}-{2} distanceLimit {3}", harmonicity.GetType().Name, r0, r1, distanceLimit);

            RationalIterator.Iterate(harmonicity, primeIndexLimit, distanceLimit, handler);
        }

        static void Main(string[] args) {
            //Test1();
            //Test2();
            Midi.Utils.Test();
        }

    }
}
