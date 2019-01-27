using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Drawing;

using Svg;


namespace Rationals {

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
                r.PowersToString(),
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
        public bool Handle(RationalInfo r) {
            if (_counter == 0) { // Write header
                Debug.WriteLine(Format(null));
            }
            Debug.WriteLine(Format(r));
            return true;
        }
    }


    class RationalPlotter : IHandler<RationalInfo> {
        Svg.Image _svg;
        IHarmonicity _harmonicity;
        Temperament _temperament;
        //
        public RationalPlotter(Svg.Image svg, IHarmonicity harmonicity) {
            _svg = svg;
            _harmonicity = harmonicity;
            _temperament = new Temperament(12);
        }
        public bool Handle(RationalInfo info) {
            Rational r = info.rational;
            float cents = (float)r.ToCents();
            float distance = (float)info.distance;
            float harm = 1f / distance; // harmonicity: 0..1

            float x = cents; // 0..1200

            string id = String.Format("{0} {1} {2} {3:F2} {4}",
                r.ToString(),
                r.PowersToString(),
                distance,
                r.ToCents(),
                _temperament.FormatRational(r)
            );

            _svg.Line(Svg.Point.Points(x, 0, x, harm * 3))
                .Add(id: id)
                .FillStroke(null, Color.LightGray, harm * 200);

            string fraction = r.FormatFraction().Replace("/", "\n");
            _svg.Text(new Svg.Point(x,0), fraction, harm * 2f, leading: 0.8f, anchor: 2)
                .Add()
                .FillStroke(Color.Black);

            return true;
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
        public bool Handle(RationalInfo r)
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

            return true;
        }
    }

    class Program {

        static void Test1() {
            var r0 = new Rational(4, 5);
            var r1 = new Rational(6, 4);
            Debug.WriteLine("{0} * {1} -> {2}", r0, r1, r0 * r1);
            Debug.WriteLine("{0} / {1} -> {2}", r0, r1, r0 / r1);

            var r2 = new Rational(17, 6);
            Debug.WriteLine("{0} epimoric powers: {1}", r2, Powers.ToString(r2.GetEpimoricPowers()));
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

            var coordinates = new Svg.Coordinates(0,1200, 1,-1, size: new Svg.Point(1200, 600));
            var svg = new Svg.Image(coordinates);

            var r0 = new Rational(1);
            var r1 = new Rational(2);
            var handler = new HandlerPipe<RationalInfo>(
                new RangeRationalHandler(r0, r1),
                new RationalPrinter(),
                new RationalPlotter(svg, harmonicity)
            );

            Debug.WriteLine("Iterate {0} range {1}-{2}", harmonicity.GetType().Name, r0, r1);

            new RationalIterator(harmonicity, 20).Iterate(handler);

            svg.Show();
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


        static void Main(string[] args) {
            //Test1();
            //Test2();
            //Midi.Utils.Test();
            //Svg.Utils.Test();
            //Test3();
            Test4_FindCommas();
        }

    }
}
