using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Diagnostics;

namespace Rationals {

    using Svg;
    using Color = System.Drawing.Color;

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

            _svg.Line(Point.Points(x, 0, x, harm * 3))
                .Add(id: id)
                .FillStroke(null, Color.LightGray, harm * 200);

            string fraction = r.FormatFraction().Replace("/", "\n");
            _svg.Text(new Point(x,0), fraction, harm * 2f, lineLeading: 0.8f, anchorH: 2)
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


    public class GridDrawer : IHandler<RationalInfo> {
        private IHarmonicity _harmonicity;
        private int _levelLimit;
        private int _countLimit;

        private Image _svg;
        private Point _imageSize; // in px
        private Point[] _bounds; // e.g. [(-2,-2),(2,2)]

        private Point[] _basis;

        private Element _groupLines;
        private Element _groupPoints;
        private Element _groupText;

        public GridDrawer(IHarmonicity harmonicity, int levelLimit, int countLimit) {
            _harmonicity = harmonicity;
            _levelLimit = levelLimit;
            _countLimit = countLimit;
            //
            _imageSize = new Point(1000, 1000);
            _bounds = new[] {
                new Point(-2, -2),
                new Point(2, 2)
            };
            // set basis
            _basis = new Point[_levelLimit];
            for (int i = 0; i < _levelLimit; ++i) {
                Rational p = Rational.GetNarrowPrime(i); // 2/1, 3/2, 5/4, 7/4, 11/8,..
                _basis[i] = MakeBasisVector(p);
            }
        }

        private static Point MakeBasisVector(Rational r) {
            double d = r.ToCents() / 1200; // 0..1
            double x = d*7 - Math.Round(d*7 / 2) * 2;
            double y = d;
            return new Point((float)x, (float)y);
        }

        private Point GetPoint(Rational r) {
            int[] c = r.GetNarrowPowers();
            var p = new Point(0, 0);
            for (int i = 0; i < c.Length; ++i) {
                if (c[i] != 0) {
                    p += _basis[i] * c[i];
                }
            }
            return p;
        }

        private bool InBounds(Point p, float r) {
            if (p.X + r < _bounds[0].X) return false;
            if (p.X - r > _bounds[1].X) return false;
            if (p.Y + r < _bounds[0].Y) return false;
            if (p.Y - r > _bounds[1].Y) return false;
            return true;
        }

        private Rational GetParent(Rational r) {
            int[] n = r.GetNarrowPowers();
            int len = n.Length;
            if (len == 0) return default(Rational);
            int i = len - 1; // last level
            Rational step = Rational.GetNarrowPrime(i); // last level step
            int last = n[i]; // last level coordinate
            if (last > 0) {
                return r / step;
            } else if (last < 0) {
                return r * step;
            } else {
                return default(Rational);
            }
        }

        public Image DrawGrid() {
            var coordinates = new Coordinates(
                _bounds[0].X, _bounds[1].X,
                _bounds[1].Y, _bounds[0].Y, // Y axis - up
                _imageSize
            );
            _svg = new Image(coordinates);

            _groupLines  = _svg.Group().Add(id: "groupLines");
            _groupPoints = _svg.Group().Add(id: "groupPoints");
            _groupText   = _svg.Group().Add(id: "groupText");

            var iterator = new RationalIterator(_harmonicity, _countLimit, _levelLimit);
            iterator.Iterate(this);

            return _svg;
        }

        private static double Interp(double f0, double f1, float k) {
            return f0 + (f1 - f0) * k;
        }

        public bool Handle(RationalInfo r)
        {
            Point pos = GetPoint(r.rational);

            float h = (float)Math.Exp(-r.distance / 30); // harmonicity 0..1
            float radius = (float)Interp(0.02, 0.1, h);

            if (!InBounds(pos, radius)) return false; // reject the point - so RationalIterator doesn't count it

            Color colorPoint = Utils.HsvToRgb(0, 0, Interp(0.95, 0.4, h));
            Color colorText  = Utils.HsvToRgb(0, 0, Interp(0.3, 0, h));

            string id = r.rational.FormatFraction() + " " + r.rational.PowersToString();

            _svg.Circle(pos, radius)
                .Add(_groupPoints, front: false, id: "c"+ id)
                .FillStroke(colorPoint);

            string t = r.rational.FormatFraction().Replace('/', '\n');
            _svg.Text(pos, t, fontSize: radius, lineLeading: 0.8f, anchorH: 2, centerV: true)
                .Add(_groupText, front: false, id: "t" + id)
                .FillStroke(colorText);

            if (r.rational.GetLevel() > 1) { // don't connect octaves (1/4 - 1/2 - 1 - 2 - 4)
                Rational parent = GetParent(r.rational);
                Point parentPos = GetPoint(parent);
                _svg.Line(new[] { parentPos, pos })
                    .Add(_groupLines, front: false, id: "l" + id)
                    .FillStroke(null, colorPoint, radius * 0.5f);
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

            var coordinates = new Svg.Coordinates(0,1200, 1,-1, size: new Point(1200, 600));
            var svg = new Svg.Image(coordinates);

            var r0 = new Rational(1);
            var r1 = new Rational(2);
            var handler = new HandlerPipe<RationalInfo>(
                new RangeRationalHandler(r0, r1),
                new RationalPrinter(),
                new RationalPlotter(svg, harmonicity)
            );

            Debug.WriteLine("Iterate {0} range {1}-{2}", harmonicity.GetType().Name, r0, r1);

            new RationalIterator(harmonicity, 200, 7).Iterate(handler);

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

        static void Test5_DrawGrid() {
            var harmonicity = new SimpleHarmonicity(2.0);
            var drawer = new GridDrawer(harmonicity, 4, 100);
            var svg = drawer.DrawGrid();
            svg.Show();
        }

        static void Main(string[] args) {
            //Test1();
            //Test2();
            //Midi.Utils.Test();
            //Svg.Utils.Test();
            //Test3();
            //Test4_FindCommas();
            Test5_DrawGrid();
        }

    }
}
