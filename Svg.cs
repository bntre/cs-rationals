using System;
using System.Collections.Generic;
using System.Text;

// ref: Svg.dll - https://github.com/vvvv/SVG
// + ref: System.XML

namespace Svg
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
                    points[i*2], 
                    points[i*2 + 1]
                );
            }
            return ps;
        }
    }

    public class Coordinates {
        private Point _size; // pixels
        private Point _leftTop; // user coordinates
        private float _scaleX; // factor
        private float _scaleY;
        private int _dirX; // -1 or 1
        private int _dirY;
        //
        public Coordinates(Point size, bool yUp = false) {
            _size = size;
            if (yUp) {
                _leftTop = new Point(0, size.Y);
                _scaleX = 1f;
                _scaleY = 1f;
                _dirX = 1;
                _dirY = -1;
            } else {
                _leftTop = new Point(0, 0);
                _scaleX = 1f;
                _scaleY = 1f;
                _dirX = 1;
                _dirY = 1;
            }
        }
        public Coordinates(float left, float right, float top, float bottom, Point size) {
            _size = size;
            _leftTop = new Point(left, top);
            float dX = right - left;
            float dY = bottom - top;
            _scaleX = size.X / Math.Abs(dX);
            _scaleY = size.Y / Math.Abs(dY);
            _dirX = Math.Sign(dX);
            _dirY = Math.Sign(dY);
        }
        //
        internal Point GetSize() { return _size; }
        //
        //internal float ScaleX(float x) { return x * _scaleX; }
        internal float ScaleY(float y) { return y * _scaleY; }
        //
        internal Point Transform(Point p) {
            return new Point(
                (p.X - _leftTop.X) * _dirX * _scaleX,
                (p.Y - _leftTop.Y) * _dirY * _scaleY
            );

        }
        internal Point[] Transform(Point[] ps) {
            int l = ps.Length;
            var res = new Point[l];
            for (int i = 0; i < l; ++i) res[i] = Transform(ps[i]);
            return res;
        }
    }

    public class Element {
        internal Image _image;
        internal SvgElement _svgElement;
        // sugar
        public Element Add(Element parent = null, string id = null, bool front = true) {
            return _image.Add(this, parent, id, front);
        }
        public Element FillStroke(Color? fill = null, Color? stroke = null, float strokeWidth = 0f) {
            return _image.FillStroke(this, fill, stroke, strokeWidth);
        }
    }

    public class Image {

        private Coordinates _coordinates;
        private SvgDocument _document;

        internal static bool IndentSvg = false; // allow to indent

        public Image(Coordinates coordinates, string id = null, bool viewBox = false) {
            _coordinates = coordinates;
            //
            _document = new SvgDocument();
            _document.ID = id ?? GetNextId("document");
            _document.Overflow = SvgOverflow.Auto;
            //
            _document.FontFamily = "Arial";
            //
            Point size = _coordinates.GetSize();
            if (viewBox) {
                _document.ViewBox = new SvgViewBox(0, 0, size.X, size.Y);
            } else {
                _document.Width = size.X;
                _document.Height = size.Y;
            }
        }

        #region Element ID Counters
        private static Dictionary<string, int> _elementCounters = new Dictionary<string, int>();
        private static string GetNextId(string prefix = "element") {
            int n = 0;
            _elementCounters.TryGetValue(prefix, out n);
            n += 1;
            _elementCounters[prefix] = n;
            return prefix + n.ToString();
        }
        #endregion

        private static System.Drawing.PointF PointF(Point p) {
            return new System.Drawing.PointF(p.X, p.Y);
        }

        private static Pathing.SvgPathSegmentList Segments(Point[] points, bool close = true) {
            var l = new Pathing.SvgPathSegmentList();
            //
            var p0 = System.Drawing.PointF.Empty;
            for (int i = 0; i < points.GetLength(0); ++i) {
                var p1 = PointF(points[i]);
                Pathing.SvgPathSegment s;
                if (i == 0) {
                    s = new Pathing.SvgMoveToSegment(p1);
                } else {
                    s = new Pathing.SvgLineSegment(p0, p1);
                }
                l.Add(s);
                p0 = p1;
            }
            //
            if (close) {
                l.Add(new Pathing.SvgClosePathSegment());
            }
            //
            return l;
        }

        private Element NewElement(SvgElement e) {
            return new Element { _svgElement = e, _image = this };
        }

        public Element Line(Point[] points) {
            points = _coordinates.Transform(points);
            //
            var line = new SvgLine();
            line.StartX = points[0].X;
            line.StartY = points[0].Y;
            line.EndX = points[1].X;
            line.EndY = points[1].Y;
            return NewElement(line);
        }

        public Element Line(Point p0, Point p1, float width0, float width1) {
            p0 = _coordinates.Transform(p0);
            p1 = _coordinates.Transform(p1);
            width0 = _coordinates.ScaleY(width0);
            width1 = _coordinates.ScaleY(width1);
            
            //!!! move the math outside
            Point dir = p1 - p0;
            dir *= 1f / (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
            //
            Point[] ps = new Point[4];
            ps[0] = p0 + new Point( dir.Y,-dir.X) * width0 * 0.5f;
            ps[1] = p0 + new Point(-dir.Y, dir.X) * width0 * 0.5f;
            ps[2] = p1 + new Point(-dir.Y, dir.X) * width1 * 0.5f;
            ps[3] = p1 + new Point( dir.Y,-dir.X) * width1 * 0.5f;

            var path = new SvgPath();
            path.PathData = Segments(ps, true);
            return NewElement(path);
        }

        public Element Path(Point[] points, bool close = true) {
            points = _coordinates.Transform(points);
            //
            var path = new SvgPath();
            path.PathData = Segments(points, close);
            return NewElement(path);
        }

        public Element Circle(Point point, float radius) {
            point = _coordinates.Transform(point);
            radius = _coordinates.ScaleY(radius);
            //
            var circle = new SvgCircle();
            circle.CenterX = point.X;
            circle.CenterY = point.Y;
            circle.Radius = radius;
            return NewElement(circle);
        }

        public Element Rectangle(Point[] points) {
            points = _coordinates.Transform(points);
            //
            var rect = new SvgRectangle();
            rect.X = Math.Min(points[0].X, points[1].X);
            rect.Y = Math.Min(points[0].Y, points[1].Y);
            rect.Width  = Math.Abs(points[1].X - points[0].X);
            rect.Height = Math.Abs(points[1].Y - points[0].Y);
            return NewElement(rect);
        }

        public Element Text(Point pos, string text, float fontSize, float lineLeading = 1f, int anchorH = 0, bool centerV = false) {
            pos = _coordinates.Transform(pos);
            fontSize = _coordinates.ScaleY(fontSize);
            //
            string[] parts = text.Split('\n');
            //
            if (centerV) {
                float fontHeight = 0.75f; // real letter part for Arial
                float fullHeight = (parts.Length - 1) * lineLeading + fontHeight; // text full height
                pos.Y -= (fullHeight / 2 - fontHeight) * fontSize; // vertical shift of first line level
            }
            //
            var t = new SvgText();
            t.TextAnchor = (SvgTextAnchor)anchorH;
            t.X.Add(pos.X);
            t.Y.Add(pos.Y);
            t.FontSize = fontSize;
            //
            for (int i = 0; i < parts.Length; ++i) {
                var span = new SvgTextSpan { Text = parts[i] };
                span.X.Add(pos.X);
                span.Dy.Add(i == 0 ? 0 : (fontSize * lineLeading));
                t.Children.Add(span);
            }
            return NewElement(t);
        }

        public Element Group() {
            var g = new SvgGroup();
            return NewElement(g);
        }

        public Element Add(Element element, Element parent = null, string id = null, bool front = true) {
            // Add to children
            SvgElement el = parent != null ? parent._svgElement : _document;
            if (front || el.Children.Count == 0) {
                el.Children.Add(element._svgElement);
            } else {
                el.Children.Insert(0, element._svgElement);
            }
            // Set id
            if (id == null) {
                string prefix = element._svgElement.GetType().Name; //!!! using internal string SvgElement.ElementName would be good here
                id = GetNextId(prefix);
            } else if (id.Length > 0) {
                if (!char.IsLetter(id[0])) id = "id" + id;
            }
            element._svgElement.ID = id;
            return element;
        }

        public Element FillStroke(Element element, Color? fill = null, Color? stroke = null, float strokeWidth = 0f) {
            strokeWidth = _coordinates.ScaleY(strokeWidth);
            //
            var e = element._svgElement;
            e.Fill   = fill   == null ? SvgColourServer.None : new SvgColourServer(fill.Value);
            e.Stroke = stroke == null ? SvgColourServer.None : new SvgColourServer(stroke.Value);
            e.StrokeWidth = strokeWidth;
            return element;
        }

        public void Save(string svgFileName) {
            _document.Write(svgFileName, indent: IndentSvg);
        }

        public void Show(string svgFileName = "image_export_temp.svg") {
            Save(svgFileName);
            System.Diagnostics.Process.Start("chrome.exe", svgFileName);
        }
    }

    
    public static class Utils {

        private static SvgDocument Test1() {
            SvgDocument sampleDoc = SvgDocument.Open(@"..\..\Svg_sample.svg");
            return sampleDoc;
        }

        private static SvgDocument Test2() {
            var document = new SvgDocument();

            document.ID = "Test2";
            document.ViewBox = new SvgViewBox(0, 0, 800, 600);

            document.Children.Add(new SvgText { Text = "test1" });

            document.Children.Add(new SvgLine() {
                StartX = 0,
                StartY = 15,
                EndX = 35,
                EndY = 35,
                Stroke = new SvgColourServer(Color.Black),
                StrokeWidth = 3
            });

            return document;
        }

        private static Image Test3() {
            var coordinates = new Coordinates(0,20, 20,0, size: new Point(600,600));
            var image = new Image(coordinates, viewBox: false);

            image.Rectangle(Point.Points(0,0, 20,20))
                .Add()
                .FillStroke(Color.FromArgb(0xEEEEEE));

            image.Rectangle(Point.Points(0,0, 10,10))
                .Add()
                .FillStroke(Color.Pink);

            image.Path(Point.Points(0,0, 5,1, 10,0, 9,5, 10,10, 5,9, 0,10, 1,5))
                .Add()
                .FillStroke(null, Color.Aqua, 0.5f);

            image.Line(Point.Points(0,0, 10,10))
                .Add()
                .FillStroke(null, Color.Red, 1);

            image.Circle(new Point(5,5), 2)
                .Add()
                .FillStroke(null, Color.DarkGreen, 0.5f);

            int n = 16;
            for (int i = 0; i <= n; ++i) {
                image.Circle(new Point(10f * i/n, 10f), 0.2f)
                    .Add()
                    .FillStroke(Color.DarkMagenta);
            }

            image.Text(new Point(5,5), "Жил\nбыл\nпёсик", fontSize: 5, lineLeading: 0.7f, anchorH: 2)
                .Add()
                .FillStroke(Color.DarkCyan, Color.Black, 0.05f);

            return image;
        }

        public static void Test() {

            //SvgDocument doc = Test1();
            //SvgDocument doc = Test2();
            //string svgExportPath = @"Svg_sample_export.svg";
            //doc.Write(svgExportPath, indent: false);
            //System.Diagnostics.Process.Start("chrome.exe", svgExportPath);

            Image image = Test3();
            image.Show();
        }
    }
}
