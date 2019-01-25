using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Drawing;

using Svg;


namespace Rationals {

    class RationalPrinter : IHandler<RationalInfo> {
        Temperament _temperament;
        int _counter;
        public RationalPrinter() {
            _temperament = new Temperament(12);
            _counter = 0;
        }
        public object[] GetParams(RationalInfo info) {
            var r = info.rational;
            if (r == null) {
                return new[] {"No", "R", "Powers", "Epimorics", "Dist", "Cents", "12TET", "Name"};
            }
            return new object[] {
                ++_counter,
                r,
                r.PowersToString(),
                Powers.ToString(r.GetEpimoricPowers(), "[]"),
                info.distance,
                r.ToCents(),
                _temperament.FormatRational(r),
                Library.GetName(r)
            };
        }
        const string _format = "{0,3}. {1,14} {2,-14} {3,-14} {4,7} {5,10:F2} {6,15} {7}";
        public string Format(RationalInfo r) {
            return String.Format(_format, GetParams(r));
        }
        public bool Handle(RationalInfo r) {
            if (_counter == 0) { // Write header
                Debug.WriteLine(Format(default(RationalInfo)));
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

            Debug.WriteLine("Sort by distance");
            collector.Iterate(RationalInfo.CompareDistances, new RationalPrinter());

            Debug.WriteLine("Sort by value");
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

        static void Test4() {

            var harmonicity = new SimpleHarmonicity(2.0);

            var coordinates = new Svg.Coordinates(0, 1200, 1, -1, size: new Svg.Point(1200, 600));
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

        static void Main(string[] args) {
            //Test1();
            Test2();
            //Midi.Utils.Test();
            //Svg.Utils.Test();
            //Test3();
            //Test4();
        }

    }
}
