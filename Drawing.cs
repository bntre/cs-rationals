using System;
using System.Collections.Generic;
//using System.Linq;

namespace Torec.Drawing
{
    using Color = System.Drawing.Color;

    [System.Diagnostics.DebuggerDisplay("({X},{Y})")]
    public struct Point {
        public float X;
        public float Y;
        public Point(float x, float y) { X = x; Y = y; }
        //
        public static Point operator +(Point a, Point b) { return new Point(a.X + b.X, a.Y + b.Y); }
        public static Point operator -(Point a, Point b) { return new Point(a.X - b.X, a.Y - b.Y); }
        public static Point operator *(Point p, float f) { return new Point(p.X * f, p.Y * f); }
        public static Point operator /(Point p, float f) { return new Point(p.X / f, p.Y / f); }
        //
        public static Point[] Points(params float[] points) {
            int l = points.Length / 2;
            var ps = new Point[l];
            for (int i = 0; i < l; ++i) {
                ps[i] = new Point(
                    points[i * 2],
                    points[i * 2 + 1]
                );
            }
            return ps;
        }
    }

    public abstract class Element {
        internal IImage _image;
        // sugar
        public Element Add(Element parent = null, string id = null, bool front = true) {
            return _image.Add(this, parent, id, front);
        }
        public Element FillStroke(Color? fill = null, Color? stroke = null, float strokeWidth = 0f) {
            return _image.FillStroke(this, fill, stroke, strokeWidth);
        }
    }

    public interface IImage {
        Point[] GetBounds();
        Element Line(Point[] points);
        Element Line(Point p0, Point p1, float width0, float width1);
        Element Path(Point[] points, bool close = true);
        Element Circle(Point point, float radius);
        Element Rectangle(Point[] points);
        Element Text(Point pos, string text, float fontSize, float lineLeading = 1f, int anchorH = 0, bool centerV = false);
        Element Group();
        Element Add(Element element, Element parent = null, string id = null, bool front = true);
        Element FillStroke(Element element, Color? fill = null, Color? stroke = null, float strokeWidth = 0f);
        void Save(string fileName);
        void Show();
    }
}



namespace Rationals.Drawing
{
    using Torec.Drawing;
    using Svg = Torec.Drawing.Svg;
    using Color = System.Drawing.Color;


    class RationalPlotter : IHandler<RationalInfo> {
        IImage _image;
        IHarmonicity _harmonicity;
        Temperament _temperament;
        //
        public RationalPlotter(IImage image, IHarmonicity harmonicity) {
            _image = image;
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

            _image.Line(Point.Points(x, 0, x, harm * 3))
                .Add(id: id)
                .FillStroke(null, Color.LightGray, harm * 200);

            string fraction = r.FormatFraction("\n");
            _image.Text(new Point(x,0), fraction, harm * 2f, lineLeading: 0.8f, anchorH: 2)
                .Add()
                .FillStroke(Color.Black);

            return 1;
        }
    }
    


    public class GridDrawer : IHandler<RationalInfo> {
        private IHarmonicity _harmonicity;
        private int _levelLimit;
        private int _countLimit;

        private IImage _image;

        private Point[] _basis; // basis vectors per prime

        private class Item {
            public Rational rational;
            public float harmonicity;
            public Point pos;
            public float radius;
            public bool visible;
            public Rational parent;
        }
        private List<Item> _items;

        // Render the image
        private Element _groupLines;
        private Element _groupPoints;
        private Element _groupText;

        private float _maxPointRadius = 0.08f; //!!! make configurable
        private float _lineWidthFactor = 0.612f;

        private Point[] _boundsForPoints; // = image bounds + max point radius

        public GridDrawer(IHarmonicity harmonicity, int levelLimit, int countLimit, IImage image) {
            _harmonicity = new HarmonicityNormalizer(harmonicity);
            _levelLimit = levelLimit;
            _countLimit = countLimit;
            _image = image;

            Point[] imageBounds = image.GetBounds();
            Point pointRadius = new Point(1, 1) * _maxPointRadius;
            _boundsForPoints = new[] {
                imageBounds[0] - pointRadius,
                imageBounds[1] + pointRadius
            };

            // set basis
            //SetBasis(new Rational(4).ToCents(), 7); // second octave up
            SetBasis(new Rational(3, 2).ToCents(), 2); // 5th up
            //SetBasis(new Rational(15, 8).ToCents(), 3); // 7th up
        }

        private void SetBasis(double centsUp, int turnsCount) {
            float d = (float)centsUp / 1200; // 0..1
            float octaveSizeX = turnsCount / d;
            // set basis
            _basis = new Point[_levelLimit];
            for (int i = 0; i < _levelLimit; ++i) {
                Rational r = Rational.GetNarrowPrime(i); // 2/1, 3/2, 5/4, 7/4, 11/8,..
                _basis[i] = MakeBasisVector(r.ToCents(), octaveSizeX);
            }
        }

        private static Point MakeBasisVector(double cents, float octaveSizeX) {
            float d = (float)cents / 1200; // 0..1
            float y = d;
            float x = d * octaveSizeX;
            x -= (float)Math.Round(x);
            return new Point(x, y);
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

        private float GetPointRadius(float harmonicity) {
            return (float)Interp(0.01, _maxPointRadius, harmonicity);
        }
        private bool IsPointVisible(float posY) {
            return (_boundsForPoints[0].Y <= posY) && (posY <= _boundsForPoints[1].Y);
        }
        private void GetPointVisibleRange(float posX, out int i0, out int i1) {
            i0 = -(int)Math.Floor(posX - _boundsForPoints[0].X);
            i1 =  (int)Math.Floor(_boundsForPoints[1].X - posX);
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
            float harmonicity = (float)Math.Exp(-distance * 1.2); // 0..1
            //
            Point pos = GetPoint(rational);
            float radius = GetPointRadius(harmonicity);
            bool visible = IsPointVisible(pos.Y);
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

        public void DrawGrid()
        {
            // Collect rationals within the Y range
            _items = new List<Item>();
            new RationalIterator(_harmonicity, _countLimit, _levelLimit)
                .Iterate(this);

            _groupLines  = _image.Group().Add(id: "groupLines");
            _groupPoints = _image.Group().Add(id: "groupPoints");
            _groupText   = _image.Group().Add(id: "groupText");

            for (int i = 0; i < _items.Count; ++i) {
                DrawItem(_items[i]);
            }
        }

        private void DrawItem(Item item)
        {
            // id for image elements
            string id = String.Format("{0} {1} {2}",
                item.rational.FormatFraction(),
                item.rational.FormatMonzo(),
                item.harmonicity
            );

            Color colorPoint = GetPointColor(item.rational, item.harmonicity);
            Color colorText = GetTextColor(item.rational, item.harmonicity);

            int i0, i1;
            GetPointVisibleRange(item.pos.X, out i0, out i1);

            // Point & Text
            if (item.visible)
            {
                for (int i = i0; i <= i1; ++i)
                {
                    Point p = item.pos;
                    p.X += i;

                    string id_i = id + "_" + i.ToString();

                    _image.Circle(p, item.radius)
                        .Add(_groupPoints, front: false, id: "c " + id_i)
                        .FillStroke(colorPoint);

                    string t = item.rational.FormatFraction("\n");
                    //string t = item.rational.FormatMonzo();
                    _image.Text(p, t, fontSize: item.radius, lineLeading: 0.8f, anchorH: 2, centerV: true)
                        .Add(_groupText, front: false, id: "t " + id_i)
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
                    GetPointVisibleRange(parent.pos.X, out pi0, out pi1);
                    pi0 = Math.Min(pi0, i0);
                    pi1 = Math.Max(pi1, i1);

                    for (int i = pi0; i <= pi1; ++i)
                    {
                        Point p = item.pos;
                        Point pp = parent.pos;
                        p.X += i;
                        pp.X += i;

                        string id_i = id + "_" + i.ToString();

                        _image.Line(p, pp, item.radius * _lineWidthFactor, parent.radius * _lineWidthFactor)
                            .Add(_groupLines, front: false, id: "l " + id_i)
                            .FillStroke(colorPoint);
                    }
                }
            }
        }

        private static double[] _powHues = new[] { 0, 0.0, 0.7, 0.4 };
        private static double[] _hueWeights = new[] { 0, 1.0, 0.3, 0.2 }; // ignore octave hue

        private static Color GetColor(double[] hues, double[] weights, double lightness) {
            int len = hues.Length;

            Point p = new Point(0, 0);
            for (int i = 0; i < len; ++i) {
                double a = hues[i] * Math.PI*2;
                p += new Point((float)Math.Cos(a), (float)Math.Sin(a)) * (float)weights[i];
            }

            double h = 0;
            if (p.X != 0 || p.Y != 0) {
                h = Math.Atan2(p.Y, p.X) / (Math.PI * 2);
            }
            h -= Math.Floor(h);

            double s = Math.Sqrt(p.X * p.X + p.Y * p.Y);
            s = Math.Min(1, s);

            return Utils.HslToRgb(h * 360, s, lightness);
        }
        
        private Color GetPointColor(Rational r, float harmonicity)
        {
            int len = Math.Max(2, r.GetLevel()); // paint octaves as 5ths
            double[] hues = new double[len];
            int[] pows = r.GetNarrowPowers(); //!!! prime powers?
            for (int i = 0; i < len; ++i) {
                hues[i] = _powHues[i] + Powers.SafeAt(pows, i) * 0.025; // !!! hue shift hardcoded
            }

            double lightness = Interp(1, 0.4, harmonicity);

            return GetColor(hues, _hueWeights, lightness);

        }

        private Color GetTextColor(Rational r, float harmonicity) {
            return Utils.HsvToRgb(0, 0, Interp(0.4, 0, harmonicity));
        }

    }


    internal static class Tests {

        internal static void DrawGrid() {
            var harmonicity =
                //new EulerHarmonicity();
                new BarlowHarmonicity();
                //new TenneyHarmonicity();
                //new SimpleHarmonicity(2.0);
                //new NarrowHarmonicity(2.0);

            //double d0 = harmonicity.GetDistance(new Rational(9, 4));
            //double d1 = harmonicity.GetDistance(new Rational(15, 8));

            var viewport = new Svg.Viewport(1600,1200, -1,1, -3,3);
            IImage image = new Svg.Image(viewport, viewBox: false);

            var drawer = new GridDrawer(harmonicity, 3, 300, image);
            drawer.DrawGrid();

            image.Show();
        }

    }
}
