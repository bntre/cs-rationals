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


    public class Viewport {
        // used to transform from user space (user units) to image space (pixels)
        private Point _sizePx;
        private Point[] _bounds;
        //
        private Point _origin; // in user units
        private float _scaleX; // factor
        private float _scaleY;
        private int _dirX; // -1 or 1
        private int _dirY;
        //
        public Viewport(Point sizePx, Point[] bounds = null, bool flipY = true) {
            _sizePx = sizePx;
            _bounds = bounds ?? new[] { new Point(0, 0), sizePx };
            Point size = _bounds[1] - _bounds[0]; // size in user units
            _scaleX = _sizePx.X / size.X;
            _scaleY = _sizePx.Y / size.Y;
            if (flipY) {
                _origin = new Point(_bounds[0].X, _bounds[1].Y);
                _dirX = 1;
                _dirY = -1;
            } else {
                _origin = new Point(_bounds[0].X, _bounds[0].Y);
                _dirX = 1;
                _dirY = 1;
            }
        }

        public Viewport(float sizeX, float sizeY, float x0, float x1, float y0, float y1, bool flipY = true) : this(
            new Point(sizeX, sizeY),
            new[] {
                new Point(x0, y0),
                new Point(x1, y1)
            },
            flipY
        ) { }

        internal Point[] GetBounds() { return _bounds; }
        internal Point GetSizePx() { return _sizePx; }

        //!!! we might move this transformation stuff to some "ViewportImage : IImage" wrapper
        //internal float ScaleX(float x) { return x * _scaleX; }
        internal float ScaleY(float y) { return y * _scaleY; }
        //
        internal Point Transform(Point p) {
            return new Point(
                (p.X - _origin.X) * _dirX * _scaleX,
                (p.Y - _origin.Y) * _dirY * _scaleY
            );
        }
        internal Point[] Transform(Point[] ps) {
            int l = ps.Length;
            var res = new Point[l];
            for (int i = 0; i < l; ++i) res[i] = Transform(ps[i]);
            return res;
        }
    }


    public abstract class Element {
        internal IImage Image;
        // sugar
        public Element Add(Element parent = null, string id = null, int index = -1) {
            return Image.Add(this, parent, id, index);
        }
        public Element FillStroke(Color fill, Color stroke, float strokeWidth = 0f) {
            return Image.FillStroke(this, fill, stroke, strokeWidth);
        }
    }

    public enum Align {
        Default = 0,
        Left    = 1,
        Center  = 2,
        Right   = 3,
    }

    public interface IImage {
        Point[] GetBounds();
        Element Line(Point[] points);
        Element Line(Point p0, Point p1, float width0, float width1);
        Element Path(Point[] points, bool close = true);
        Element Circle(Point point, float radius);
        Element Rectangle(Point[] points);
        Element Text(Point pos, string text, float fontSize, float lineLeading = 1f, Align align = Align.Left, bool centerHeight = false);
        Element Group();
        Element Add(Element element, Element parent = null, string id = null, int index = -1);
        Element FillStroke(Element element, Color fill, Color stroke, float strokeWidth = 0f);
        //void Save(string fileName);
        //void Show();
    }

    public static class Tests
    {
        internal static void DrawTest3(IImage image)
        {
            image.Rectangle(Point.Points(0,0, 20,20))
                .Add()
                .FillStroke(Color.FromArgb(0xEEEEEE), Color.Empty);

            image.Rectangle(Point.Points(0,0, 10,10))
                .Add()
                .FillStroke(Color.Pink, Color.Empty);

            image.Path(Point.Points(0,0, 5,1, 10,0, 9,5, 10,10, 5,9, 0,10, 1,5))
                .Add()
                .FillStroke(Color.Empty, Color.Aqua, 0.5f);

            image.Line(Point.Points(0,0, 10,10))
                .Add()
                .FillStroke(Color.Empty, Color.Red, 1);

            image.Line(Point.Points(0,5, 10,5))
                .Add()
                .FillStroke(Color.Empty, Color.Red, 0.1f);

            image.Circle(new Point(5, 5), 2)
                .Add()
                .FillStroke(Color.Empty, Color.DarkGreen, 0.25f);

            int n = 16;
            for (int i = 0; i <= n; ++i) {
                image.Circle(new Point(10f * i / n, 10f), 0.2f)
                    .Add()
                    .FillStroke(Color.DarkMagenta, Color.Empty);
            }

            image.Text(new Point(5, 5), "Жил\nбыл\nпёсик", fontSize: 5f, lineLeading: 0.7f, align: Align.Center)
                .Add()
                .FillStroke(Color.DarkCyan, Color.Black, 0.05f);

            image.Text(new Point(5, 5), "81\n80", fontSize: 1f, lineLeading: 0.7f, align: Align.Center, centerHeight: true)
                .Add()
                .FillStroke(Color.Black, Color.Empty);
        }
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
                .FillStroke(Color.Empty, Color.LightGray, harm * 200);

            string fraction = r.FormatFraction("\n");
            _image.Text(new Point(x,0), fraction, harm * 2f, lineLeading: 0.8f, align: Align.Center)
                .Add()
                .FillStroke(Color.Black, Color.Empty);

            return 1;
        }
    }
    


    public class GridDrawer : IHandler<RationalInfo> {
        private IHarmonicity _harmonicity;
        private int _levelLimit;
        private int _countLimit;

        private IImage _image;

        private float _octaveSizeX;
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
            _octaveSizeX = turnsCount / d;
            // set basis
            _basis = new Point[_levelLimit];
            for (int i = 0; i < _levelLimit; ++i) {
                Rational r = Rational.GetNarrowPrime(i); // 2/1, 3/2, 5/4, 7/4, 11/8,..
                _basis[i] = MakeBasisVector(r.ToCents());
            }
        }

        private Point MakeBasisVector(double cents) {
            float d = (float)cents / 1200; // 0..1
            float y = d;
            float x = d * _octaveSizeX;
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
        private void GetPointVisibleRangeX(float posX, out int i0, out int i1) {
            i0 = -(int)Math.Floor(posX - _boundsForPoints[0].X);
            i1 =  (int)Math.Floor(_boundsForPoints[1].X - posX);
        }
        private void GetPointVisibleRangeY(float posY, out int i0, out int i1) {
            i0 = -(int)Math.Floor(posY - _boundsForPoints[0].Y);
            i1 =  (int)Math.Floor(_boundsForPoints[1].Y - posY);
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
            GetPointVisibleRangeX(item.pos.X, out i0, out i1);

            // Point & Text
            if (item.visible)
            {
                for (int i = i0; i <= i1; ++i)
                {
                    Point p = item.pos;
                    p.X += i;

                    string id_i = id + "_" + i.ToString();

                    _image.Circle(p, item.radius)
                        .Add(_groupPoints, index: 0, id: "c " + id_i)
                        .FillStroke(colorPoint, Color.Empty);

                    string t = item.rational.FormatFraction("\n");
                    //string t = item.rational.FormatMonzo();
                    _image.Text(p, t, fontSize: item.radius, lineLeading: 0.8f, align: Align.Center, centerHeight: true)
                        .Add(_groupText, index: 0, id: "t " + id_i)
                        .FillStroke(colorText, Color.Empty);
                }
            }

            // Line to parent
            if (!item.parent.IsDefault())
            {
                Item parent = FindItem(item.parent);

                if (item.visible || parent.visible)
                {
                    int pi0, pi1;
                    GetPointVisibleRangeX(parent.pos.X, out pi0, out pi1);
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
                            .Add(_groupLines, index: 0, id: "l " + id_i)
                            .FillStroke(colorPoint, Color.Empty);
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

        #region 12EDO Grid
        public void Draw12EdoGrid() {
            //
            Element group12EDO = _image.Group().Add(id: "group12EDO", index: -2); // put under groupText
            //
            Point p4 = MakeBasisVector(400);
            Point p3 = MakeBasisVector(300);
            var lines = new Point[7][];
            for (int i = 0; i < 4; ++i) lines[i  ] = new Point[] { p3*i, p3*i + p4*3 };
            for (int i = 0; i < 3; ++i) lines[i+4] = new Point[] { p4*i, p4*i + p3*4 };
            //
            for (int i = 0; i < 7; ++i) {
                Point p0 = lines[i][0];
                Point p1 = lines[i][1];
                int j0, j1;
                GetPointVisibleRangeY(p0.Y, out j0, out j1);
                for (int j = j0-1; j <= j1; ++j) {
                    Rational r = new Rational(new int[] { j }); // ..1/4 - 1/2 - 1 - 2 - 4..
                    Point origin = GetPoint(r);
                    int k0, k1, ktemp;
                    GetPointVisibleRangeX(origin.X + Math.Max(p0.X, p1.X), out k0, out ktemp);
                    GetPointVisibleRangeX(origin.X + Math.Min(p0.X, p1.X), out ktemp, out k1);
                    for (int k = k0; k <= k1; ++k) {
                        Point shift = new Point(1f, 0) * k;
                        _image.Line(new[] { origin + p0 + shift, origin + p1 + shift })
                            .Add(group12EDO, id: String.Format("12edo_{0}_{1}_{2}", j, k, i))
                            .FillStroke(Color.Empty, Color.DarkGray, (i == 0 || i == 4) ? 0.012f : 0.004f);
                    }
                }
            }
        }
        #endregion

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

            var viewport = new Viewport(1600,1200, -1,1, -3,3);
            var image = new Svg.Image(viewport, viewBox: false);

            var drawer = new GridDrawer(harmonicity, 3, 300, image);
            drawer.DrawGrid();

            drawer.Draw12EdoGrid();

            image.Show();
        }

    }
}
