using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Diagnostics;


namespace Rationals {

    class Program {

        static void Main(string[] args)
        {
            var r0 = new Rational(4, 5);
            var r1 = new Rational(6, 4);

            Debug.WriteLine("{0} * {1} -> {2}", r0, r1, r0 * r1);
            Debug.WriteLine("{0} / {1} -> {2}", r0, r1, r0 / r1);

        }
    }
}
