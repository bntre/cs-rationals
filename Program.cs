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
        public int Handle(RationalInfo info) {
            Rational r = info.rational;
            float cents = (float)r.ToCents();
            float distance = (float)info.distance;
            float harm = 1f / distance; // harmonicity: 0..1

            float x = cents; // 0..1200

            string id = String.Format("{0} {1} {2} {3:F2} {4}",
                r.ToString(),
                r.FormatMonzo(),
                distance,
                r.ToCents(),
                _temperament.FormatRational(r)
            );

            _svg.Line(Point.Points(x, 0, x, harm * 3))
                .Add(id: id)
                .FillStroke(null, Color.LightGray, harm * 200);

            string fraction = r.FormatFraction("\n");
            _svg.Text(new Point(x,0), fraction, harm * 2f, lineLeading: 0.8f, anchorH: 2)
                .Add()
                .FillStroke(Color.Black);

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


    public class GridDrawer : IHandler<RationalInfo> {
        private IHarmonicity _harmonicity;
        private int _levelLimit;
        private int _countLimit;

        private Point _imageSize; // in px
        private Point[] _bounds; // e.g. [(-2,-2),(2,2)]

        private Point[] _basis;

        private class Item {
            public Rational rational;
            //public float distance;
            public float harmonicity;
            public Point pos;
            public float radius;
            public bool visible;
            public Rational parent;
        }

        private List<Item> _items;


        // Render the image
        private Image _svg;
        private Element _groupLines;
        private Element _groupPoints;
        private Element _groupText;

        public GridDrawer(IHarmonicity harmonicity, int levelLimit, int countLimit) {
            _harmonicity = new HarmonicityNormalizer(harmonicity);
            _levelLimit = levelLimit;
            _countLimit = countLimit;
            //
            _imageSize = new Point(1000, 1000);
            _bounds = new[] {
                new Point(-2, -3),
                new Point(2, 3)
            };
            // set basis
            _basis = new Point[_levelLimit];
            for (int i = 0; i < _levelLimit; ++i) {
                _basis[i] = MakeBasisVector_Narrow(i);
            }
        }

        private static Point MakeBasisVector_Narrow(int i) {
            Rational r = Rational.GetNarrowPrime(i); // 2/1, 3/2, 5/4, 7/4, 11/8,..
            return MakeBasisVector(r.ToCents());
        }
        private static Point MakeBasisVector(double cents) {
            double d = cents / 1200; // 0..1
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

        private float _maxPointRadius = 0.1f;
        private float GetPointRadius(float harmonicity) {
            return (float)Interp(0.01, _maxPointRadius, harmonicity);
        }
        private bool IsVisible(float posY) {
            return (_bounds[0].Y <= posY + _maxPointRadius) && (posY - _maxPointRadius <= _bounds[1].Y);
        }
        private void GetVisibleRange(float posX, out int i0, out int i1) {
            i0 = -(int)Math.Floor(((posX + _maxPointRadius) - _bounds[0].X) / 2f);
            i1 =  (int)Math.Floor((_bounds[1].X - (posX - _maxPointRadius)) / 2f);
        }

        private static double Interp(double f0, double f1, float k) { // Move out
            return f0 + (f1 - f0) * k;
        }

        private static Rational GetParent(Rational r) {
            int[] n = r.GetNarrowPowers();
            int level = Powers.GetLevel(n); // ignore trailing zeros
            if (level == 0) return default(Rational);
            int i = level - 1; // last level
            Rational step = Rational.GetNarrowPrime(i); // last level step
            int last = n[i]; // last level coordinate
            if (last > 0) {
                return r / step;
            } else {
                return r * step;
            }
        }

        private Item FindItem(Rational rational) {
            for (int i = 0; i < _items.Count; ++i) {
                if (_items[i].rational.Equals(rational)) return _items[i];
            }
            return null;
        }

        private Item AddItem(Rational rational, double distance = -1) {
            if (distance < 0) { // unknown distance
                distance = _harmonicity.GetDistance(rational);
            }
            //
            //float harmonicity = (float)Math.Exp(-distance / 30); // 0..1
            float harmonicity = (float)Math.Exp(-distance * 1.2); // 0..1
            //
            Point pos = GetPoint(rational);
            float radius = GetPointRadius(harmonicity);
            bool visible = IsVisible(pos.Y);
            bool hasParent = rational.GetLevel() > 1; // don't link octaves (1/4 - 1/2 - 1 - 2 - 4)
            Rational parent = hasParent ? GetParent(rational) : default(Rational);

            Item item = new Item {
                rational = rational,
                harmonicity = harmonicity,
                pos = pos,
                radius = radius,
                visible = visible,
                parent = parent,
            };
            _items.Add(item);

            if (hasParent && FindItem(parent) == null) { // the parent probably not iterated by RationalIterator yet
                AddItem(parent);
            }

            return item;
        }

        // IHandler
        public int Handle(RationalInfo r) {
            Item item = FindItem(r.rational); // probably this rational was already added as some parent
            if (item == null) {
                item = AddItem(r.rational, r.distance);
            }
            return item.visible ? 1 : 0; // count only visible rationals
        }

        public Image DrawGrid()
        {
            // Collect rationals within the Y range
            _items = new List<Item>();
            new RationalIterator(_harmonicity, _countLimit, _levelLimit)
                .Iterate(this);

            // create Svg
            var coordinates = new Coordinates(
                _bounds[0].X, _bounds[1].X,
                _bounds[1].Y, _bounds[0].Y, // Y axis - up
                _imageSize
            );

            _svg = new Image(coordinates, viewBox: false);
            //Image.IndentSvg = true; // for Debug

            _groupLines  = _svg.Group().Add(id: "groupLines");
            _groupPoints = _svg.Group().Add(id: "groupPoints");
            _groupText   = _svg.Group().Add(id: "groupText");

            for (int i = 0; i < _items.Count; ++i) {
                DrawItem(_items[i]);
            }

            return _svg;
        }

        private void DrawItem(Item item)
        {
            // id for image elements
            string id = String.Format("{0} {1} {2}",
                item.rational.FormatFraction(),
                item.rational.FormatMonzo(),
                item.harmonicity
            );

            Color colorPoint = Utils.HsvToRgb(0, 0, Interp(0.999, 0.4, item.harmonicity));
            Color colorText  = Utils.HsvToRgb(0, 0, Interp(0.4, 0, item.harmonicity));

            int i0, i1;
            GetVisibleRange(item.pos.X, out i0, out i1);

            // Point & Text
            if (item.visible)
            {
                for (int i = i0; i <= i1; ++i)
                {
                    Point p = item.pos + new Point(2f, 0) * i;
                    string id_i = id + "_" + i.ToString();

                    _svg.Circle(p, item.radius)
                        .Add(_groupPoints, front: false, id: "c" + id_i)
                        .FillStroke(colorPoint);

                    string t = item.rational.FormatFraction("\n");
                    //string t = item.rational.FormatMonzo();
                    _svg.Text(p, t, fontSize: item.radius, lineLeading: 0.8f, anchorH: 2, centerV: true)
                        .Add(_groupText, front: false, id: "t" + id_i)
                        .FillStroke(colorText);
                }
            }

            // Line to parent
            if (!item.parent.IsDefault())
            {
                Item parent = FindItem(item.parent);

                if (item.visible || parent.visible)
                {
                    int pi0, pi1;
                    GetVisibleRange(parent.pos.X, out pi0, out pi1);
                    pi0 = Math.Min(pi0, i0);
                    pi1 = Math.Max(pi1, i1);

                    for (int i = pi0; i <= pi1; ++i)
                    {
                        Point p  = item.pos   + new Point(2f, 0) * i;
                        Point pp = parent.pos + new Point(2f, 0) * i;

                        string id_i = id + "_" + i.ToString();

                        _svg.Line(p, pp, item.radius * 0.5f, parent.radius * 0.5f)
                            .Add(_groupLines, front: false, id: "l" + id_i)
                            .FillStroke(colorPoint);
                    }
                }
            }
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
            var harmonicity =
                new EulerHarmonicity();
                //new BarlowHarmonicity();
                //new TenneyHarmonicity();
                //new SimpleHarmonicity(2.0);
                //new NarrowHarmonicity(2.0);

            //double d0 = harmonicity.GetDistance(new Rational(9, 4));
            //double d1 = harmonicity.GetDistance(new Rational(15, 8));

            var drawer = new GridDrawer(harmonicity, 3, 400);
            drawer.DrawGrid().Show();
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
