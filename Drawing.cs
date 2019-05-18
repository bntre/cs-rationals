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
        private Point _scaleAdditional; // used for "scaling" resize (depends on window size), not a saved setting
        private Point _scale;
        private float _scaleScalar; // additional scale considered

        public Viewport2() : this(100,100, 0,0, 50,-50) { }
        public Viewport2(float sizeX, float sizeY, float centerX, float centerY, float scaleX, float scaleY) {
            SetImageSize(sizeX, sizeY);
            SetCenter(centerX, centerY);
            SetAdditionalScale(1f, 1f);
            SetScale(scaleX, scaleY);
        }

        private static Point Mul(Point p, Point scale) { return new Point(p.X * scale.X, p.Y * scale.Y); }
        private static Point Div(Point p, Point scale) { return new Point(p.X / scale.X, p.Y / scale.Y); }

        public Point GetImageSize() { return _imageSize; }
        public Point GetScale()     { return _scale; }
        public Point GetCenter()    { return _userCenter; }

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
            Point scale = Mul(_scale, _scaleAdditional);
            return _imageSize/2 + Mul(p - _userCenter, scale);
        }
        #endregion

        #region Image -> User coordinates
        public float ToUser(float size) { return size / _scaleScalar; }
        public Point ToUser(Point p) {
            Point scale = Mul(_scale, _scaleAdditional);
            return _userCenter + Div(p - _imageSize/2, scale);
        }
        #endregion

        // Updating
        public void SetImageSize(float sizeX, float sizeY) {
            _imageSize.X = sizeX;
            _imageSize.Y = sizeY;
        }
        public void SetCenter(float centerX, float centerY) {
            _userCenter.X = centerX;
            _userCenter.Y = centerY;
        }
        public void SetCenterDelta(float centerDX, float centerDY) {
            _userCenter.X += ToUser(centerDX);
            _userCenter.Y += ToUser(centerDY);
        }
        public void SetAdditionalScale(float scaleX, float scaleY) {
            _scaleAdditional.X = scaleX;
            _scaleAdditional.Y = scaleY;
            UpdateScaleScalar();
        }
        public void SetScale(float scaleX, float scaleY) {
            _scale.X = scaleX;
            _scale.Y = scaleY;
            UpdateScaleScalar();
        }
        public void SetScaleDelta(float scaleDX, float scaleDY, int mouseX, int mouseY) {
            Point mouseImagePos = new Point(mouseX, mouseY);
            Point mouseUserPos0 = ToUser(mouseImagePos);

            _scale.X *= scaleDX;
            _scale.Y *= scaleDY;
            UpdateScaleScalar();

            Point mouseUserPos1 = ToUser(mouseImagePos);
            _userCenter -= mouseUserPos1 - mouseUserPos0;
        }

        private void UpdateScaleScalar() {
            Point scale = Mul(_scale, _scaleAdditional);
            _scaleScalar = (float)Math.Sqrt(Math.Abs(scale.X * scale.Y));
        }
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
            return System.Drawing.Color.FromArgb(0xFF, (byte)(r * 0xFF), (byte)(g * 0xFF), (byte)(b * 0xFF));
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
            if (h > 360) { do { h -= 360; } while (h > 360); }
            else while (h < 0) h += 360;
            //
            if (h <  60) return q1 + (q2 - q1) * h / 60;
            if (h < 180) return q2;
            if (h < 240) return q1 + (q2 - q1) * (240 - h) / 60;
            return q1;
        }
        #endregion

    }


    internal static partial class Tests
    {
        internal static void DrawTest3(Image image)
        {
            image.Rectangle(Point.Points(0,0, 20,20))
                .Add()
                .FillStroke(Color.FromArgb(unchecked((int)0xFFEEEEEE)), Color.Empty);

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

            image.Text(new Point(5, 5), "Жил\nбыл\nпёсик", fontSize: 5f, lineLeading: 0.7f, align: Image.Align.Center)
                .Add()
                .FillStroke(Color.DarkCyan, Color.Black, 0.05f);

            image.Text(new Point(5, 5), "81\n80", fontSize: 1f, lineLeading: 0.7f, align: Image.Align.Center, centerHeight: true)
                .Add()
                .FillStroke(Color.Black, Color.Empty);
        }
    }
}



namespace Rationals.Drawing
{
    using Torec.Drawing;
    using Color = System.Drawing.Color;


    class RationalPlotter : IHandler<RationalInfo> {
        Image _image;
        IHarmonicity _harmonicity;
        Temperament _temperament;
        //
        public RationalPlotter(Image image, IHarmonicity harmonicity) {
            _image = image;
            _harmonicity = harmonicity;
            _temperament = new Temperament(12, Rational.Two);
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
            _image.Text(new Point(x,0), fraction, harm * 2f, lineLeading: 0.8f, align: Image.Align.Center)
                .Add()
                .FillStroke(Color.Black, Color.Empty);

            return 1;
        }
    }
    
    public class RationalColors //!!! rename to some ColorUtils
    {
        public struct HueSaturation {
            public float hue; // 0..1
            public float saturation; // 0..1
        }

        private double[] _primeHues;
        private double[] _hueWeights;

        private double _hueStep = 0.025; // !!! make configurable?

        public RationalColors(int count) {
            //_primeHues = new[] { 0, 0.0, 0.7, 0.4, 0.4, 0.4, 0.4, 0.4 };
            //_hueWeights = new[] { 0, 1.0, 0.3, 0.2, 0.2, 0.2, 0.2, 0.2 };

            // generate hues and weights
            count = Math.Max(count, 2); // we need 5ths hue (for octaves)
            _primeHues  = new double[count];
            _hueWeights = new double[count];
            for (int i = 1; i < count; ++i) { // ignore octave hue
                _primeHues[i] = GetRareHue(i - 1);
                _hueWeights[i] = 1.0 / i;
            }
        }

        public static double GetRareHue(int i) {
            double h = Math.Log(i * 2 + 1, 2);
            return h - Math.Floor(h);
        }

        public HueSaturation GetRationalHue(int[] pows)
        {
            int len = Math.Max(pows.Length, 2); // use 5ths hue for octaves

            double[] hues = new double[len];
            for (int i = 0; i < len; ++i) {
                hues[i] = _primeHues[i] + Powers.SafeAt(pows, i) * _hueStep;
            }

            Point p = new Point(0, 0);
            for (int i = 0; i < len; ++i) {
                double a = hues[i] * Math.PI*2;
                p += new Point((float)Math.Cos(a), (float)Math.Sin(a)) * (float)_hueWeights[i];
            }

            double h = 0;
            if (p.X != 0 || p.Y != 0) {
                h = Math.Atan2(p.Y, p.X) / (Math.PI * 2);
            }
            h -= Math.Floor(h);

            double s = Math.Sqrt(p.X * p.X + p.Y * p.Y);
            s = Math.Min(1, s);

            return new HueSaturation { hue = (float)h, saturation = (float)s };
        }

        public static Color GetColor(HueSaturation h, double lightness) {
            return Utils.HslToRgb(h.hue * 360, h.saturation, lightness);
        }
    }

    internal static class Tests {

        internal static void DrawGrid() {
            string harmonicityName = "Euler Barlow Tenney".Split()[1];

            var viewport = new Viewport(1600,1200, -1,1, -3,3);

            var drawer = new GridDrawer();
            drawer.SetBounds(viewport.GetUserBounds());
            // UpdateBase
            drawer.SetBase(2, null, null, harmonicityName);
            drawer.SetGeneratorLimits(500);
            //drawer.SetCommas(null);
            drawer.SetPointRadiusFactor(3f);
            drawer.SetEDGrids(new[] { new GridDrawer.EDGrid { baseInterval = new Rational(2), stepCount = 12 } });
            // UpdateSlope
            drawer.SetSlope(new Rational(3,2), 2.0f);

            drawer.UpdateItems();

            // svg + png
            var image2 = new Torec.Drawing.Image(viewport);
            drawer.DrawGrid(image2, 0);
            image2.WriteSvg("test_new.svg");
            image2.WritePng("test_new.png", true);
        }
    }
}
