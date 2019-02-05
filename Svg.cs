using System;
using System.Collections.Generic;
using System.Text;

namespace Torec.Drawing.Svg
{
    // ref: Svg.dll - https://github.com/vvvv/SVG + System.XML
    using global::Svg; 
    using global::Svg.Pathing;

    using Color = System.Drawing.Color;


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

        public Viewport(float sizeX, float sizeY, float x0, float x1, float y0, float y1) : this(
            new Point(sizeX, sizeY),
            new[] {
                new Point(x0, y0),
                new Point(x1, y1)
            }
        ) { }

        internal Point[] GetBounds() { return _bounds; }
        internal Point GetSizePx() { return _sizePx; }

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


    public class Image : IImage
    {
        private Viewport _viewport;

        private SvgDocument _document;

        internal static bool IndentSvg = false; // allow to indent - to debug

        public Image(Viewport viewport, string id = null, bool viewBox = false) {
            _viewport = viewport;
            //
            _document = new SvgDocument();
            _document.ID = id ?? GetNextId("document");
            _document.Overflow = SvgOverflow.Auto;
            //
            _document.FontFamily = "Arial";
            //
            Point sizePx = _viewport.GetSizePx();
            if (viewBox) {
                _document.ViewBox = new SvgViewBox(0, 0, sizePx.X, sizePx.Y);
            } else {
                _document.Width = sizePx.X;
                _document.Height = sizePx.Y;
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

        private static SvgPathSegmentList Segments(Point[] points, bool close = true) {
            var l = new SvgPathSegmentList();
            //
            var p0 = System.Drawing.PointF.Empty;
            for (int i = 0; i < points.GetLength(0); ++i) {
                var p1 = new System.Drawing.PointF(points[i].X, points[i].Y);
                SvgPathSegment s;
                if (i == 0) {
                    s = new SvgMoveToSegment(p1);
                } else {
                    s = new SvgLineSegment(p0, p1);
                }
                l.Add(s);
                p0 = p1;
            }
            //
            if (close) {
                l.Add(new SvgClosePathSegment());
            }
            //
            return l;
        }

        #region Element
        private class InternalElement : Element { //!!! SvgElement would be better name
            internal SvgElement _svgElement;
        }
        private Element NewElement(SvgElement e) {
            return new InternalElement {
                _image = this,
                _svgElement = e
            };
        }
        private SvgElement GetSvgElement(Element e) {
            InternalElement i = (InternalElement)e;
            return i._svgElement;
        }
        #endregion

        public Point[] GetBounds() { return _viewport.GetBounds(); }

        public Element Line(Point[] points) {
            points = _viewport.Transform(points);
            //
            var line = new SvgLine();
            line.StartX = points[0].X;
            line.StartY = points[0].Y;
            line.EndX = points[1].X;
            line.EndY = points[1].Y;
            return NewElement(line);
        }

        public Element Line(Point p0, Point p1, float width0, float width1) {
            p0 = _viewport.Transform(p0);
            p1 = _viewport.Transform(p1);
            width0 = _viewport.ScaleY(width0);
            width1 = _viewport.ScaleY(width1);
            
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
            points = _viewport.Transform(points);
            //
            var path = new SvgPath();
            path.PathData = Segments(points, close);
            return NewElement(path);
        }

        public Element Circle(Point point, float radius) {
            point = _viewport.Transform(point);
            radius = _viewport.ScaleY(radius);
            //
            var circle = new SvgCircle();
            circle.CenterX = point.X;
            circle.CenterY = point.Y;
            circle.Radius = radius;
            return NewElement(circle);
        }

        public Element Rectangle(Point[] points) {
            points = _viewport.Transform(points);
            //
            var rect = new SvgRectangle();
            rect.X = Math.Min(points[0].X, points[1].X);
            rect.Y = Math.Min(points[0].Y, points[1].Y);
            rect.Width  = Math.Abs(points[1].X - points[0].X);
            rect.Height = Math.Abs(points[1].Y - points[0].Y);
            return NewElement(rect);
        }

        public Element Text(Point pos, string text, float fontSize, float lineLeading = 1f, int anchorH = 0, bool centerV = false) {
            pos = _viewport.Transform(pos);
            fontSize = _viewport.ScaleY(fontSize);
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
            SvgElement e = GetSvgElement(element);
            SvgElement p = parent != null ? GetSvgElement(parent) : _document;
            // Add to parent's children
            if (front || p.Children.Count == 0) {
                p.Children.Add(e);
            } else {
                p.Children.Insert(0, e);
            }
            // Set id
            if (id == null) {
                string prefix = e.GetType().Name; //!!! using internal string SvgElement.ElementName would be good here
                id = GetNextId(prefix);
            } else if (id.Length > 0) {
                if (!char.IsLetter(id[0])) id = "id" + id;
            }
            e.ID = id;
            return element;
        }

        public Element FillStroke(Element element, Color? fill = null, Color? stroke = null, float strokeWidth = 0f) {
            strokeWidth = _viewport.ScaleY(strokeWidth);
            //
            SvgElement e = GetSvgElement(element);
            e.Fill   = fill   == null ? SvgColourServer.None : new SvgColourServer(fill.Value);
            e.Stroke = stroke == null ? SvgColourServer.None : new SvgColourServer(stroke.Value);
            e.StrokeWidth = strokeWidth;
            return element;
        }

        public void Save(string svgFileName) {
            _document.Write(svgFileName, indent: IndentSvg);
        }

        public void Show() {
            string svgFileName = "image_export_temp.svg";
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
            var viewport = new Viewport(600,600, 0,20, 0,20);
            var image = new Image(viewport, viewBox: false);

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

            Test3().Show();
        }
    }
}
