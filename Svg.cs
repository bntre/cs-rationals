using System;
using System.Collections.Generic;
using System.Text;

namespace Torec.Drawing.Svg
{
    // ref: Svg.dll - https://github.com/vvvv/SVG + System.XML
    using global::Svg; 
    using global::Svg.Pathing;

    using Color = System.Drawing.Color;

    public class Image : IImage
    {
        private IViewport _viewport;
        private SvgDocument _document;

        internal static bool IndentSvg = false; // allow to indent - to debug

        public Image(IViewport viewport, string id = null, bool viewBox = false) {
            _viewport = viewport;
            //
            _document = new SvgDocument();
            _document.ID = id ?? GetNextId("document");
            _document.Overflow = SvgOverflow.Auto;
            //
            _document.FontFamily = "Arial";
            //
            Point sizePx = _viewport.GetImageSize();
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
                Image = this,
                _svgElement = e
            };
        }
        private SvgElement GetSvgElement(Element e) {
            InternalElement i = (InternalElement)e;
            return i._svgElement;
        }
        #endregion

        //public Point[] GetBounds() { return _viewport.GetUserBounds(); }

        public Element Line(Point[] points) {
            points = Utils.ToImage(_viewport, points);
            //
            var line = new SvgLine();
            line.StartX = points[0].X;
            line.StartY = points[0].Y;
            line.EndX = points[1].X;
            line.EndY = points[1].Y;
            return NewElement(line);
        }

        public Element Line(Point p0, Point p1, float width0, float width1) {
            p0 = _viewport.ToImage(p0);
            p1 = _viewport.ToImage(p1);
            width0 = _viewport.ToImage(width0);
            width1 = _viewport.ToImage(width1);

            //!!! move the math outside
            Point dir = p1 - p0;
            dir /= (float)Math.Sqrt(dir.X * dir.X + dir.Y * dir.Y);
            dir = new Point(dir.Y, -dir.X);
            Point[] ps = new Point[4];
            ps[0] = p0 + dir * width0 * 0.5f;
            ps[1] = p0 - dir * width0 * 0.5f;
            ps[2] = p1 - dir * width1 * 0.5f;
            ps[3] = p1 + dir * width1 * 0.5f;

            var path = new SvgPath();
            path.PathData = Segments(ps, true);
            return NewElement(path);
        }

        public Element Path(Point[] points, bool close = true) {
            points = Utils.ToImage(_viewport, points);
            //
            var path = new SvgPath();
            path.PathData = Segments(points, close);
            return NewElement(path);
        }

        public Element Circle(Point point, float radius) {
            point = _viewport.ToImage(point);
            radius = _viewport.ToImage(radius);
            //
            var circle = new SvgCircle();
            circle.CenterX = point.X;
            circle.CenterY = point.Y;
            circle.Radius = radius;
            return NewElement(circle);
        }

        public Element Rectangle(Point[] points) {
            points = Utils.ToImage(_viewport, points);
            //
            var rect = new SvgRectangle();
            rect.X = Math.Min(points[0].X, points[1].X);
            rect.Y = Math.Min(points[0].Y, points[1].Y);
            rect.Width  = Math.Abs(points[1].X - points[0].X);
            rect.Height = Math.Abs(points[1].Y - points[0].Y);
            return NewElement(rect);
        }

        public Element Text(Point pos, string text, float fontSize, float lineLeading = 1f, Align align = Align.Left, bool centerHeight = false) {
            pos = _viewport.ToImage(pos);
            fontSize = _viewport.ToImage(fontSize);
            //
            string[] parts = text.Split('\n');
            //
            if (centerHeight) {
                float fontHeight = 0.75f; // real letter part for Arial
                float fullHeight = (parts.Length - 1) * lineLeading + fontHeight; // text full height
                pos.Y -= (fullHeight / 2 - fontHeight) * fontSize; // vertical shift of first line level
            }
            //
            var t = new SvgText();
            t.TextAnchor = (SvgTextAnchor)align;
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

        public Element Add(Element element, Element parent = null, string id = null, int index = -1) {
            SvgElement e = GetSvgElement(element);
            SvgElement p = parent != null ? GetSvgElement(parent) : _document;
            // Add to parent's children
            if (index == -1 || p.Children.Count == 0) {
                p.Children.Add(e);
            } else {
                if (index < 0) {
                    index += p.Children.Count + 1;
                }
                p.Children.Insert(index, e);
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

        public Element FillStroke(Element element, Color fill, Color stroke, float strokeWidth = 0f) {
            strokeWidth = _viewport.ToImage(strokeWidth);
            //
            SvgElement e = GetSvgElement(element);
            e.Fill   = fill   == Color.Empty ? SvgColourServer.None : new SvgColourServer(fill);
            e.Stroke = stroke == Color.Empty ? SvgColourServer.None : new SvgColourServer(stroke);
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


    public static class Tests {

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

        internal static void Test3() {
            var viewport = new Viewport(600,600, 0,20, 0,20, false);
            var image = new Image(viewport, viewBox: false);
            Torec.Drawing.Tests.DrawTest3(image);
            image.Show();
        }

        internal static void Test() {
            //SvgDocument doc = Test1();
            //SvgDocument doc = Test2();
            //string svgExportPath = @"Svg_sample_export.svg";
            //doc.Write(svgExportPath, indent: false);
            //System.Diagnostics.Process.Start("chrome.exe", svgExportPath);
        }
    }
}
