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

    public interface IViewport {
        Point GetImageSize();
        Point[] GetUserBounds();
        //
        float ToImage(float size);
        Point ToImage(Point p);
        //
        float ToUser(float size);
        Point ToUser(Point p);
    }

    public class Viewport : IViewport {
        // used to transform from user space (user units) to image space (e.g. pixels)
        protected Point _imageSize;
        protected Point[] _bounds;
        //
        private int _dirX; // -1 or 1
        private int _dirY; // -1 or 1
        // secondary values - updated in Update()
        protected float _scaleX; // factor
        protected float _scaleY;
        protected Point _origin; // in user units
        //
        public Viewport(bool flipY = true)
            : this(100,100, 0,1, 0,1, flipY) 
        { }

        public Viewport(float sizeX, float sizeY, float x0, float x1, float y0, float y1, bool flipY = true) {
            _dirX = 1;
            _dirY = flipY ? -1 : 1;
            SetImageSize(sizeX, sizeY);
            SetUserBounds(x0,x1, y0,y1);
            Update();
        }

        protected void SetImageSize(Point size) {
            _imageSize = size;
        }
        protected void SetImageSize(float w, float h) {
            SetImageSize(new Point(w, h));
        }
        protected void SetUserBounds(Point[] bounds) {
            _bounds = bounds;
        }
        protected void SetUserBounds(float x0, float x1, float y0, float y1) {
            SetUserBounds(new[] { new Point(x0,y0), new Point(x1,y1) });
        }

        protected void Update() {
            // scale
            Point size = _bounds[1] - _bounds[0]; // size in user units
            _scaleX = _imageSize.X / size.X; // * _dirX here !!! 
            _scaleY = _imageSize.Y / size.Y;
            // origin
            _origin = new Point(
                _bounds[_dirX > 0 ? 0 : 1].X, 
                _bounds[_dirY > 0 ? 0 : 1].Y
            );
        }

        public Point GetImageSize() { return _imageSize; }
        public Point[] GetUserBounds() { return _bounds; }

        #region User -> Image coordinates
        public float ToImage(float size) { return size * _scaleY; }
        public Point ToImage(Point p) {
            p -= _origin;
            p = new Point(
                p.X * _dirX * _scaleX,
                p.Y * _dirY * _scaleY
            );
            return p;
        }
        #endregion

        #region Image -> User coordinates
        public float ToUser(float size) { return size / _scaleY; }
        public Point ToUser(Point p) {
            p = new Point(
                p.X / (_dirX * _scaleX),
                p.Y / (_dirY * _scaleY)
            );
            p += _origin;
            return p;
        }
        #endregion
    }

    public class Viewport2 : IViewport {
        private Point _imageSize;
        private Point _userCenter;
        private Point _scale;
        private float _scaleScalar;

        public Viewport2() : this(100,100, 0,0, 50,-50) { }
        public Viewport2(float sizeX, float sizeY, float centerX, float centerY, float scaleX, float scaleY) {
            _imageSize = new Point(sizeX, sizeY);
            _userCenter = new Point(centerX, centerY);
            _scale = new Point(scaleX, scaleY);
            // update
            _scaleScalar = (float)Math.Sqrt(Math.Abs(_scale.X * _scale.Y));
        }

        private static Point Mul(Point p, Point scale) { return new Point(p.X * scale.X, p.Y * scale.Y); }
        private static Point Div(Point p, Point scale) { return new Point(p.X / scale.X, p.Y / scale.Y); }

        public Point GetImageSize() { return _imageSize; }
        public Point[] GetUserBounds() {
            Point p0 = ToUser(new Point(0, 0));
            Point p1 = ToUser(_imageSize);
            bool px = p0.X <= p1.X;
            bool py = p0.Y <= p1.Y;
            return new[] {
                new Point(px ? p0.X : p1.X,  py ? p0.Y : p1.Y),
                new Point(px ? p1.X : p0.X,  py ? p1.Y : p0.Y)
            };
        }

        #region User -> Image coordinates
        public float ToImage(float size) { return size * _scaleScalar; }
        public Point ToImage(Point p) {
            return _imageSize/2 + Mul(p - _userCenter, _scale);
        }
        #endregion

        #region Image -> User coordinates
        public float ToUser(float size) { return size / _scaleScalar; }
        public Point ToUser(Point p) {
            return _userCenter + Div(p - _imageSize/2, _scale);
        }
        #endregion

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
        //Point[] GetBounds();
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

    public static class Utils
    {
        public static Point[] ToImage(IViewport viewport, Point[] ps) {
            var res = new Point[ps.Length];
            for (int i = 0; i < ps.Length; ++i) {
                res[i] = viewport.ToImage(ps[i]);
            }
            return res;
        }

        #region Color spaces
        // from http://www.java2s.com/Code/CSharp/2D-Graphics/HsvToRgb.htm
        public static System.Drawing.Color HsvToRgb(double h, double s, double v)
        {
            int hi = (int)Math.Floor(h / 60.0) % 6;
            double f = (h / 60.0) - Math.Floor(h / 60.0);

            double p = v * (1.0 - s);
            double q = v * (1.0 - (f * s));
            double t = v * (1.0 - ((1.0 - f) * s));

            switch (hi) {
                case 0:
                    return FromRgb(v, t, p);
                case 1:
                    return FromRgb(q, v, p);
                case 2:
                    return FromRgb(p, v, t);
                case 3:
                    return FromRgb(p, q, v);
                case 4:
                    return FromRgb(t, p, v);
                case 5:
                    return FromRgb(v, p, q);
                default:
                    return FromRgb(0, 0, 0);
            }
        }
        private static System.Drawing.Color FromRgb(double r, double g, double b) {
            return System.Drawing.Color.FromArgb(255, (byte)(r * 255.0), (byte)(g * 255.0), (byte)(b * 255.0));
        }

        // from http://csharphelper.com/blog/2016/08/convert-between-rgb-and-hls-color-models-in-c/
        public static System.Drawing.Color HslToRgb(double h, double s, double l) {
            if (s == 0) return FromRgb(l, l, l);
            //
            double p2 = l <= 0.5 ? l * (1 + s) : s + l * (1 - s);
            double p1 = 2 * l - p2;
            //
            double r = QqhToRgb(p1, p2, h + 120);
            double g = QqhToRgb(p1, p2, h);
            double b = QqhToRgb(p1, p2, h - 120);
            //
            return FromRgb(r, g, b);
        }
        private static double QqhToRgb(double q1, double q2, double h) {
            if (h > 360) h -= 360;
            else if (h < 0) h += 360;
            //
            if (h <  60) return q1 + (q2 - q1) * h / 60;
            if (h < 180) return q2;
            if (h < 240) return q1 + (q2 - q1) * (240 - h) / 60;
            return q1;
        }
        #endregion

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
        //private int _countLimit;
        private IIterator<RationalInfo> _rationalIterator;

        //private IImage _image;

        private float _octaveSizeX;
        private Point[] _basis; // basis vectors per prime
        private float _maxLineHeight; // used to skip unreachable items from collecting

        private class Item {
            public Rational rational;
            public float harmonicity;
            public Point pos;
            public float radius;
            public bool visible;
            public Rational parent;
        }
        private List<Item> _items;
        private Dictionary<Rational, Item> _itemMap;
        private HashSet<Rational> _handledRationals; // all handled Rationals (including skipped)

        // Render the image
        private Element _groupLines;
        private Element _groupPoints;
        private Element _groupText;

        private float _maxPointRadius;

        private const float _lineWidthFactor = 0.612f;

        private Point[] _bounds;

        public GridDrawer(IHarmonicity harmonicity, Point[] bounds, int countLimit = -1, int levelLimit = -1, Rational distanceLimit = default(Rational), float pointRadiusFactor = 1f) {
            _harmonicity = new HarmonicityNormalizer(harmonicity);
            _rationalIterator = new RationalIterator(
                _harmonicity, 
                countLimit, 
                levelLimit,
                distanceLimit.IsDefault() ? -1 : _harmonicity.GetDistance(distanceLimit)
            );
            _levelLimit = levelLimit;

            _maxPointRadius = 0.05f * pointRadiusFactor;

            // set basis
            //SetBasis(new Rational(4).ToCents(), 7); // second octave up
            SetBasis(new Rational(3, 2).ToCents(), 2); // 5th up
            //SetBasis(new Rational(15, 8).ToCents(), 3); // 7th up

            //
            _bounds = bounds;
            // Get max vector to skip items
            _maxLineHeight = 0f;
            for (int i = 0; i < _levelLimit; ++i) {
                float h = Math.Abs(_basis[i].Y);
                if (_maxLineHeight < h) _maxLineHeight = h;
            }

            // Collect rationals within the Y range
            CollectItems();
        }

        private void SetBasis(double centsUp, int turnsCount) {
            float d = (float)centsUp / 1200; // 0..1
            _octaveSizeX = turnsCount / d;
            // Set basis
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
            return (float)Interp(_maxPointRadius * 0.1, _maxPointRadius, harmonicity);
        }

        private bool IsLineVisible(float posY) { 
            // returns false if the line (to the parent) can't be visible in Y range
            float d = _maxPointRadius + _maxLineHeight;
            return (_bounds[0].Y <= (posY + d)) && ((posY - d) <= _bounds[1].Y);
        }
        private bool IsPointVisible(float posY) {
            return (_bounds[0].Y <= (posY + _maxPointRadius)) && ((posY - _maxPointRadius) <= _bounds[1].Y);
        }
        private void GetPointVisibleRangeX(float posX, out int i0, out int i1) {
            i0 = -(int)Math.Floor((posX + _maxPointRadius) - _bounds[0].X);
            i1 =  (int)Math.Floor(_bounds[1].X - (posX - _maxPointRadius));
        }
        private void GetPointVisibleRangeY(float posY, out int i0, out int i1) {
            i0 = -(int)Math.Floor((posY + _maxPointRadius) - _bounds[0].Y);
            i1 =  (int)Math.Floor(_bounds[1].Y - (posY - _maxPointRadius));
        }

        private static double Interp(double f0, double f1, float k) { // Move out
            return f0 + (f1 - f0) * k;
        }

        #region Collecting items
        private static Rational GetParent(Rational r) {
            int[] n = r.GetNarrowPowers();
            int level = Powers.GetLevel(n); // ignore trailing zeros
            if (level == 0) return default(Rational);
            int i = level - 1; // last level
            Rational step = Rational.GetNarrowPrime(i); // last level step -- !!! cache them to some _narrowPrimes[]
            int last = n[i]; // last level coordinate
            if (last > 0) {
                return r / step;
            } else {
                return r * step;
            }
        }

        private Item FindItem(Rational rational) {
            Item item = null;
            _itemMap.TryGetValue(rational, out item);
            return item;
        }

        private Item AddItem(Rational rational, double distance = -1)
        {
            _handledRationals.Add(rational);

            Point pos = GetPoint(rational);
            if (!IsLineVisible(pos.Y)) return null; // skip item from collecting

            if (distance < 0) distance = _harmonicity.GetDistance(rational); // distance may be unknown when adding a parent

            float harmonicity = (float)Math.Exp(-distance * 1.2); // 0..1 -- move out of here!!!

            Item item = new Item {
                rational = rational,
                pos = pos,
                harmonicity = harmonicity,
                radius = GetPointRadius(harmonicity),
                visible = IsPointVisible(pos.Y),
            };

            bool hasParent = rational.GetLevel() > 1; // don't link octaves (1/4 - 1/2 - 1 - 2 - 4)
            if (hasParent) {
                item.parent = GetParent(rational);
            }

            _items.Add(item);
            _itemMap[rational] = item;

            // the parent probably not iterated by RationalIterator yet
            if (hasParent && !_handledRationals.Contains(item.parent)) {
                AddItem(item.parent);
            }

            return item;
        }

        public int Handle(RationalInfo r) {
            Item item = FindItem(r.rational); // probably this rational was already added as some parent
            if (item == null) {
                if (_handledRationals.Contains(r.rational)) return 0; // rational was previously handled but skipped as unreachable
                item = AddItem(r.rational, r.distance); // try to add item
                if (item == null) return 0; // the rational is now handled but skipped as unreachable
            }
            return item.visible ? 1 : 0; // let Iterator count only visible rationals
        }

        private void CollectItems() {
            _items = new List<Item>();
            _itemMap = new Dictionary<Rational, Item>();
            _handledRationals = new HashSet<Rational>();
            _rationalIterator.Iterate(this);
        }
        #endregion

        public Rational FindNearestRational(Point pos) {
            Rational nearest = default(Rational);
            float dist = float.MaxValue;
            for (int i = 0; i < _items.Count; ++i) {
                Item item = _items[i];
                if (item.visible) {
                    Point p = item.pos - pos;
                    p.X -= (float)Math.Round(p.X);
                    float d = p.X * p.X + p.Y * p.Y;
                    if (dist > d) {
                        dist = d;
                        nearest = item.rational;
                    }
                }
            }
            return nearest;
        }

        public void DrawGrid(IImage image, Rational highlight = default(Rational))
        {
            _groupLines  = image.Group().Add(id: "groupLines");
            _groupPoints = image.Group().Add(id: "groupPoints");
            _groupText   = image.Group().Add(id: "groupText");

            for (int i = 0; i < _items.Count; ++i) {
                bool h = _items[i].rational.Equals(highlight);
                DrawItem(image, _items[i], h);
            }
        }

        private void DrawItem(IImage image, Item item, bool highlight = false)
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

                    image.Circle(p, item.radius)
                        .Add(_groupPoints, index: -1, id: "c " + id_i)
                        .FillStroke(colorPoint, highlight ? Color.Red : Color.Empty, _maxPointRadius * 0.1f);

                    string t = item.rational.FormatFraction("\n");
                    //string t = item.rational.FormatMonzo();
                    image.Text(p, t, fontSize: item.radius, lineLeading: 0.8f, align: Align.Center, centerHeight: true)
                        .Add(_groupText, index: -1, id: "t " + id_i)
                        .FillStroke(colorText, Color.Empty);
                }
            }

            // Line to parent
            if (!item.parent.IsDefault())
            {
                Item parent = FindItem(item.parent);

                if (parent != null && (item.visible || parent.visible))
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

                        image.Line(p, pp, item.radius * _lineWidthFactor, parent.radius * _lineWidthFactor)
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
            //return Utils.HsvToRgb(0, 0, Interp(0.4, 0, harmonicity));

            int len = Math.Max(2, r.GetLevel()); // paint octaves as 5ths
            double[] hues = new double[len];
            int[] pows = r.GetNarrowPowers(); //!!! prime powers?
            for (int i = 0; i < len; ++i) {
                hues[i] = _powHues[i] + Powers.SafeAt(pows, i) * 0.025; // !!! hue shift hardcoded
            }

            double lightness = Interp(0.4, 0, harmonicity);

            return GetColor(hues, _hueWeights, lightness);
        }

        #region 12EDO Grid
        public void Draw12EdoGrid(IImage image) {
            //
            Element group12EDO = image.Group().Add(id: "group12EDO", index: -2); // put under groupText
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
                        image.Line(new[] { origin + p0 + shift, origin + p1 + shift })
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

            var drawer = new GridDrawer(harmonicity, viewport.GetUserBounds(), levelLimit: 3, countLimit: 300);
            drawer.DrawGrid(image);
            drawer.Draw12EdoGrid(image);

            image.Show();
        }
    }
}
