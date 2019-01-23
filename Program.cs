using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Drawing;

using Svg;


namespace Rationals {

    class RationalPrinter : IHandler<Rational> {
        IHarmonicity _harmonicity;
        Temperament _temperament;
        int _counter;
        const string _format = "{0,3}. {1,14} {2,-14} {3,-14} {4,7} {5,10:F2} {6,15}";
        public RationalPrinter(IHarmonicity harmonicity) {
            _harmonicity = harmonicity;
            _temperament = new Temperament(12);
            _counter = 0;
        }
        public string Format(Rational r) {
            double distance = _harmonicity.GetDistance(r);
            return String.Format(_format,
                ++_counter,
                r,
                r.PowersToString(),
                Powers.ToString(r.GetEpimoricPowers(), "[]"),
                distance,
                r.ToCents(),
                _temperament.FormatRational(r)
            );
        }
        public Rational Handle(Rational r) {
            if (_counter == 0) { // Write header
                Debug.WriteLine(_format, "No", "R", "Powers", "Epimorics", "Dist", "Cents", "12TET");
            }
            Debug.WriteLine(Format(r));
            return r;
        }
    }


    class RationalPlotter : IHandler<Rational> {
        Svg.Image _svg;
        IHarmonicity _harmonicity;
        Temperament _temperament;
        //
        public RationalPlotter(Svg.Image svg, IHarmonicity harmonicity) {
            _svg = svg;
            _harmonicity = harmonicity;
            _temperament = new Temperament(12);
        }
        public Rational Handle(Rational r) {
            //!!! optimize to transfer distance through the pipe

            float cents = (float)r.ToCents();
            float distance = (float)_harmonicity.GetDistance(r); 
            float harm = 1f / (float)distance; // harmonicity: 0..1

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

            return r;
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
            var handler = new HandlerPipe<Rational>(
                new RangeRationalHandler(r0, r1),
                new RationalPrinter(harmonicity)
            );

            Debug.WriteLine("Iterate {0} range {1}-{2}", harmonicity.GetType().Name, r0, r1);

            new RationalIterator(harmonicity, handler, 20, 3).Iterate();
        }

        static void Test3() {

            var harmonicity = new SimpleHarmonicity(2.0);

            var coordinates = new Svg.Coordinates(0,1200, 1,-1, size: new Svg.Point(1200, 600));
            var svg = new Svg.Image(coordinates);

            var r0 = new Rational(1);
            var r1 = new Rational(2);
            var handler = new HandlerPipe<Rational>(
                new RangeRationalHandler(r0, r1),
                new RationalPrinter(harmonicity),
                new RationalPlotter(svg, harmonicity)
            );

            Debug.WriteLine("Iterate {0} range {1}-{2}", harmonicity.GetType().Name, r0, r1);

            new RationalIterator(harmonicity, handler, 20).Iterate();

            svg.Show();
        }

        static void Test4() {

            var harmonicity = new SimpleHarmonicity(2.0);

            var coordinates = new Svg.Coordinates(0, 1200, 1, -1, size: new Svg.Point(1200, 600));
            var svg = new Svg.Image(coordinates);

            var r0 = new Rational(1);
            var r1 = new Rational(2);
            var handler = new HandlerPipe<Rational>(
                new RangeRationalHandler(r0, r1),
                new RationalPrinter(harmonicity),
                new RationalPlotter(svg, harmonicity)
            );

            Debug.WriteLine("Iterate {0} range {1}-{2}", harmonicity.GetType().Name, r0, r1);

            new RationalIterator(harmonicity, handler, 20).Iterate();

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
