using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;

namespace Rationals {

    using Torec.Drawing;

    class RationalPrinter : IHandler<RationalInfo> {
        string _label;
        Temperament _temperament;
        int _counter;
        public RationalPrinter(string label = null) {
            _label = label;
            _temperament = new Temperament(12, Rational.Two);
            _counter = 0;
        }
        const string _format = "{0} {1,3}.{2,14} {3,-14} {4,-14} {5,7:0.000} {6,10:F2} {7,15} {8} {9}";
        private object[] GetFormatParams(RationalInfo info) {
            if (info == null) {
                return new[] {"", "No", "R", "Powers", "Epimorics", "Dist", "Cents", "12TET", "", ""};
            }
            var r = info.rational;
            return new object[] {
                _label,
                ++_counter,
                r,
                r.FormatMonzo(),
                Powers.ToString(r.GetEpimoricPowers(), "[]"),
                info.distance,
                r.ToCents(),
                _temperament.FormatRational(r),
                Library.Find(r),
                info.additionalData
            };
        }
        public string Format(RationalInfo r) {
            return String.Format(_format, GetFormatParams(r));
        }
        public int Handle(RationalInfo r) {
            if (_counter == 0) { // Write header
                Debug.WriteLine(Format(null));
            }
            Debug.WriteLine(Format(r));
            return 1;
        }
    }

    // Splits input into primary and product Rationals
    public class RationalOrganizer : IHandler<RationalInfo> {
        private Rational[] _basis = null;
        private int _vectorLength;
        //
        public RationalOrganizer(int vectorLength) {
            _vectorLength = vectorLength;
        }
        //

        public class VectorInfo {
            public RationalOrganizer organizer;
            public int[] coords;
            public override string ToString() {
                Rational[] basis = organizer._basis;
                string s = "";
                for (int i = 0; i < coords.Length; ++i) {
                    int c = coords[i];
                    if (c == 0) continue;
                    if (s != "") s += " * ";
                    s += String.Format("({0})^{1}", basis[i].FormatFraction(), c);
                }
                return Powers.ToString(coords, "|]") + " " + s;
            }
        }
        //
        public int Handle(RationalInfo r) {
            if (_basis == null) {
                _basis = new[] { r.rational };
            } else {
                int[] coords = Vectors.FindCoordinates(_basis, r.rational, _vectorLength);
                if (coords == null) {
                    Array.Resize(ref _basis, _basis.Length + 1);
                    _basis[_basis.Length - 1] = r.rational;
                } else {
                    r.additionalData = new VectorInfo { organizer = this, coords = coords };
                }
            }
            //
            return 1;
        }
    }


    class Program {

        static void Test1() {
            var r0 = new Rational(4, 5);
            var r1 = new Rational(6, 4);
            Debug.WriteLine("{0} * {1} -> {2}", r0, r1, r0 * r1);
            Debug.WriteLine("{0} / {1} -> {2}", r0, r1, r0 / r1);

            //var r2 = new Rational(17, 6);
            //Debug.WriteLine("{0} epimoric powers: {1}", r2, Powers.ToString(r2.GetEpimoricPowers()));

            var r3 = new Rational(81, 80);
            Debug.WriteLine("{0} -> {1} {2}", r3, r3.FormatMonzo(), r3.FormatNarrows());
        }

        static void Test2() {
            //var harmonicity = new BarlowHarmonicity();
            //var harmonicity = new SimpleHarmonicity(2.0);
            var harmonicity = new EpimoricHarmonicity(2.0);

            var r0 = new Rational(1);
            var r1 = new Rational(25, 24);

            Debug.WriteLine("Iterate {0} range {1}-{2}", harmonicity.GetType().Name, r0, r1);

            var collector = new Collector<RationalInfo>();
            var handler = new HandlerPipe<RationalInfo>(
                new RangeRationalHandler(r0, r1),
                new RationalPrinter(),
                collector
            );
            var limits = new RationalGenerator.Limits { dimensionCount = 3, rationalCount = 20 };
            new RationalIterator(harmonicity, limits, null, handler).Iterate();

            Debug.WriteLine("-------------------\n Sort by distance");
            collector.Iterate(RationalInfo.CompareDistances, new RationalPrinter());

            Debug.WriteLine("-------------------\n Sort by value");
            collector.Iterate(RationalInfo.CompareValues, new RationalPrinter());
        }

        static void Test3() {

            var harmonicity = new SimpleHarmonicity(2.0);

            var viewport = new Torec.Drawing.Viewport(1200,600, 0,1200, 1,-1);
            var image = new Torec.Drawing.Image(viewport);

            var r0 = new Rational(1);
            var r1 = new Rational(2);
            var handler = new HandlerPipe<RationalInfo>(
                new RangeRationalHandler(r0, r1, false, true),
                new RationalPrinter(),
                new Drawing.RationalPlotter(image, harmonicity)
            );

            Debug.WriteLine("Iterate {0} range {1}-{2}", harmonicity.GetType().Name, r0, r1);

            var limits = new RationalGenerator.Limits { dimensionCount = 7, rationalCount = 200, distance = -1 };
            new RationalIterator(harmonicity, limits, null, handler).Iterate();

            image.Show();
        }


        /*
          No.             R Powers         Epimorics         Dist      Cents           12TET  
           1.         25/24 |-3 -1 2>      [0 -1 2]        18.467      70.67           1-29c Chroma, Chromatic semitone 
           2.         81/80 |-4 4 -1>      [-2 4 -1]       21.067      21.51           0+22c Syntonic comma 
           3.       128/125 |7 0 -3>       [1 0 -3]        26.200      41.06           0+41c Enharmonic diesis, Lesser diesis 
           4.       250/243 |1 -5 3>       [2 -5 3]        33.533      49.17           0+49c Porcupine comma, Maximal diesis, Major diesis |1 -1 0] (25/24)^1 * (81/80)^-1
           5.     2048/2025 |11 -4 -2>     [3 -4 -2]       34.467      19.55           0+20c Diaschisma |0 -1 1] (81/80)^-1 * (128/125)^1
           6.       648/625 |3 4 -4>       [-1 4 -4]       39.267      62.57           1-37c Diminished comma, Major diesis, Greater diesis |0 1 1] (81/80)^1 * (128/125)^1
           7.     6561/6400 |-8 8 -2>      [-4 8 -2]       42.133      43.01           0+43c  |0 2 0] (81/80)^2
           8.   20480/19683 |12 -9 1>      [5 -9 1]        42.400      68.72           1-31c  |1 -2 1] (25/24)^1 * (81/80)^-2 * (128/125)^1
           9.   32805/32768 |-15 8 1>      [-5 8 1]        42.733       1.95            0+2c Schisma |0 2 -1] (81/80)^2 * (128/125)^-1
          10.     3125/3072 |-10 -1 5>     [-1 -1 5]       44.667      29.61           0+30c  |1 0 -1] (25/24)^1 * (128/125)^-1
          11.   16875/16384 |-14 3 4>      [-3 3 4]        47.600      51.12           1-49c Negri comma, Double augmentation diesis |1 1 -1] (25/24)^1 * (81/80)^1 * (128/125)^-1
          12. 531441/524288 |-19 12>       [-7 12]         51.000      23.46           0+23c  |0 3 -1] (81/80)^3 * (128/125)^-1
          13.   20000/19683 |5 -9 4>       [4 -9 4]        54.600      27.66           0+28c  |1 -2 0] (25/24)^1 * (81/80)^-2
          14.   15625/15552 |-6 -5 6>      [1 -5 6]        57.733       8.11            0+8c  |1 -1 -1] (25/24)^1 * (81/80)^-1 * (128/125)^-1
          15. 262144/253125 |18 -4 -5>     [4 -4 -5]       60.667      60.61           1-39c  |0 -1 2] (81/80)^-1 * (128/125)^2
          16. 531441/512000 |-12 12 -3>    [-6 12 -3]      63.200      64.52           1-35c  |0 3 0] (81/80)^3
          17.1638400/1594323|16 -13 2>     [7 -13 2]       63.467      47.21           0+47c  |1 -3 1] (25/24)^1 * (81/80)^-3 * (128/125)^1
          18.4194304/4100625|22 -8 -4>     [6 -8 -4]       68.933      39.11           0+39c  |0 -2 2] (81/80)^-2 * (128/125)^2
          19.   78732/78125 |2 9 -7>       [-3 9 -7]       70.800      13.40           0+13c  |-1 2 1] (25/24)^-1 * (81/80)^2 * (128/125)^1
          20. 393216/390625 |17 1 -8>      [2 1 -8]        70.867      11.45           0+11c  |-1 0 2] (25/24)^-1 * (128/125)^2
        */
        static void Test4_FindCommas() {
            //var harmonicity = new EulerHarmonicity();
            var harmonicity = new BarlowHarmonicity();
            //var harmonicity = new SimpleHarmonicity(2.0);
            //var harmonicity = new EpimoricHarmonicity(2.0);

            Rational r0 = new Rational(1);
            Rational r1 = new Rational(25, 24);
            int limit = 3;
            int count = 20;

            Debug.WriteLine("Iterate {0} range {1}-{2}", harmonicity.GetType().Name, r0, r1);

            var collector = new Collector<RationalInfo>();
            var handler = new HandlerPipe<RationalInfo>(
                new RangeRationalHandler(r0, r1, false, true),
                //new RationalPrinter(),
                collector
            );
            var limits = new RationalGenerator.Limits { dimensionCount = limit, rationalCount = count };
            new RationalIterator(harmonicity, limits, null, handler).Iterate();

            Debug.WriteLine("-------------------\n Sort by distance");
            collector.Iterate(RationalInfo.CompareDistances, new RationalPrinter());

            //Debug.WriteLine("-------------------\n Sort by value");
            //collector.Iterate(RationalInfo.CompareValues, new RationalPrinter());

            // Organize commas
            Debug.WriteLine("-------------------\n Organize");
            collector.Iterate(RationalInfo.CompareDistances, 
                new HandlerPipe<RationalInfo>(
                    new RationalOrganizer(limit),
                    new RationalPrinter()
                )
            );
        }

        static void Test5_ParseRationals() {
            string[] ss = new string[] { " 81 / 80 \n", " | 7 \t 0 -3> " };
            for (int i = 0; i < ss.Length; ++i) {
                Rational r = Rational.Parse(ss[i]);
                if (r.IsDefault()) {
                    Debug.WriteLine(String.Format("'{0}' -> can't parse", ss[i]));
                } else {
                    Debug.WriteLine(String.Format("'{0}' -> {1} {2}", ss[i], r.FormatFraction(), r.FormatMonzo()));
                }
            }
        }

        static void Test6_IntervalTree() {
            var tree = new IntervalTree<Rational, double>(r => r.ToCents());
            tree.Add(Rational.One);
            tree.Add(new Rational(1, 2));
        }

        [STAThread] // e.g. for FileDialog
        static void Main(string[] args) {
            //Test1();
            //Test2();
            //Midi.Utils.Test();
            //Torec.Drawing.Tests.Test();
            //Test3();
            //Test4_FindCommas();
            //Drawing.Tests.DrawGrid();

            Forms.Utils.RunForm();

            //Test5_ParseRationals();
            //Vectors.Test();
            //Test6_IntervalTree();
            //Rationals.Libs.Tests.Test();
        }

    }
}
