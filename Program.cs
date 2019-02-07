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
            _temperament = new Temperament(12);
            _counter = 0;
        }
        const string _format = "{0} {1,3}.{2,14} {3,-14} {4,-14} {5,7} {6,10:F2} {7,15} {8} {9}";
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
    public class RationalOrganizer : IHandler<RationalInfo>
    {
        private HashSet<Rational> _primarySet = new HashSet<Rational>();
        private List<Rational> _knownList = new List<Rational>();
        //
        public RationalOrganizer() {}
        //
        public class ProductInfo {
            public Rational R0;
            public Rational R1;
            public bool Multiple; // multiple or divide
            public override string ToString() {
                return String.Format("({0} {2} {1})", R0, R1, Multiple ? "*" : "/");
            }
        }
        //
        public int Handle(RationalInfo r)
        {
            bool isPrimary = true;
            for (int i = 0; i < _knownList.Count; ++i) {
                Rational k = _knownList[i];
                Rational p;
                p = r.rational / k;
                if (_primarySet.Contains(p)) {
                    r.additionalData = new ProductInfo { R0 = p, R1 = k, Multiple = true };
                    isPrimary = false;
                    break;
                }
                p = r.rational * k;
                if (_primarySet.Contains(p)) {
                    r.additionalData = new ProductInfo { R0 = p, R1 = k, Multiple = false };
                    isPrimary = false;
                    break;
                }
            }

            _knownList.Add(r.rational);
            if (isPrimary) {
                _primarySet.Add(r.rational);
            }

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
            Debug.WriteLine("{0} -> {1} {2}", r3, r3.FormatMonzo(), Powers.ToString(r3.GetNarrowPowers(), "|}"));
        }

        static void Test2() {
            //var harmonicity = new BarlowHarmonicity();
            //var harmonicity = new SimpleHarmonicity(2.0);
            var harmonicity = new EpimoricHarmonicity(2.0);

            var r0 = new Rational(1);
            var r1 = new Rational(25, 24);

            Debug.WriteLine("Iterate {0} range {1}-{2}", harmonicity.GetType().Name, r0, r1);

            var collector = new Collector<RationalInfo>();
            new RationalIterator(harmonicity, 20, 3).Iterate(
                new HandlerPipe<RationalInfo>(
                    new RangeRationalHandler(r0, r1),
                    new RationalPrinter(),
                    collector
                )
            );

            Debug.WriteLine("-------------------\n Sort by distance");
            collector.Iterate(RationalInfo.CompareDistances, new RationalPrinter());

            Debug.WriteLine("-------------------\n Sort by value");
            collector.Iterate(RationalInfo.CompareValues, new RationalPrinter());
        }

        static void Test3() {

            var harmonicity = new SimpleHarmonicity(2.0);

            var viewport = new Torec.Drawing.Viewport(1200,600, 0,1200, 1,-1);
            var image = new Torec.Drawing.Svg.Image(viewport);

            var r0 = new Rational(1);
            var r1 = new Rational(2);
            var handler = new HandlerPipe<RationalInfo>(
                new RangeRationalHandler(r0, r1),
                new RationalPrinter(),
                new Drawing.RationalPlotter(image, harmonicity)
            );

            Debug.WriteLine("Iterate {0} range {1}-{2}", harmonicity.GetType().Name, r0, r1);

            new RationalIterator(harmonicity, 200, 7).Iterate(handler);

            image.Show();
        }

        static void Test4_FindCommas() {
            //var harmonicity = new BarlowHarmonicity();
            //var harmonicity = new SimpleHarmonicity(2.0);
            var harmonicity = new EpimoricHarmonicity(2.0);

            var r0 = new Rational(1);
            var r1 = new Rational(25, 24);

            Debug.WriteLine("Iterate {0} range {1}-{2}", harmonicity.GetType().Name, r0, r1);

            var collector = new Collector<RationalInfo>();
            new RationalIterator(harmonicity, 20, 3).Iterate(
                new HandlerPipe<RationalInfo>(
                    new RangeRationalHandler(r0, r1),
                    //new RationalPrinter(),
                    collector
                )
            );

            Debug.WriteLine("-------------------\n Sort by distance");
            collector.Iterate(RationalInfo.CompareDistances, new RationalPrinter());

            //Debug.WriteLine("-------------------\n Sort by value");
            //collector.Iterate(RationalInfo.CompareValues, new RationalPrinter());

            // Organize commas
            Debug.WriteLine("-------------------\n Organize");
            collector.Iterate(RationalInfo.CompareDistances, 
                new HandlerPipe<RationalInfo>(
                    new RationalOrganizer(),
                    new RationalPrinter()
                )
            );
        }

        //[STAThread] // for Forms?
        static void Main(string[] args) {
            //Test1();
            //Test2();
            //Midi.Utils.Test();
            //Torec.Drawing.Svg.Tests.Test3();
            //Test3();
            //Test4_FindCommas();
            Drawing.Tests.DrawGrid();
            //Forms.Utils.RunForm();
        }

    }
}
