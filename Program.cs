using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Diagnostics;


namespace Rationals {

    class RationalPrinter : IRationalHandler {
        public void HandleRational(Rational r, double distance) {
            Debug.WriteLine("Got: {0} (distance {1})", r, distance);
        }
    }


    class Program {

        static void Main(string[] args)
        {
            var r0 = new Rational(4, 5);
            var r1 = new Rational(6, 4);

            Debug.WriteLine("{0} * {1} -> {2}", r0, r1, r0 * r1);
            Debug.WriteLine("{0} / {1} -> {2}", r0, r1, r0 / r1);

            //var harmonicity = new BarlowHarmonicity();
            var harmonicity = new SimpleHarmonicity(2.0);
            double distanceLimit = harmonicity.GetDistance(new Rational(10, 9));

            Debug.WriteLine("Iterate {0} distanceLimit {1}", harmonicity.GetType().Name, distanceLimit);

            RationalIterator.Iterate(harmonicity, 3, distanceLimit, new RationalPrinter());
        }
    }
}
