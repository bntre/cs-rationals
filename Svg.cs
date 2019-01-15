using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Numerics;

// ref: Svg.dll - https://github.com/vvvv/SVG
// + ref: System.XML

namespace Svg
{
    using Element = SvgElement;

    public class Image {
        public SvgDocument document;
        public Image(float w, float h, string id = null) {
            document = new SvgDocument();
            document.ID = id ?? GetNextId("document");
            document.ViewBox = new SvgViewBox(0, 0, w, h);
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

        #region Init Point
        public static PointF Point(float x, float y) {
            return new PointF(x, y);
        }
        public static PointF[] Points(params float[] points) {
            int l = points.GetLength(0) / 2;
            var ps = new PointF[l];
            for (int i = 0; i < l; ++i) {
                ps[i] = new PointF(points[i * 2], points[i * 2 + 1]);
            }
            return ps;
        }
        public static PointF[] Points(float[,] points) {
            int l = points.GetLength(0);
            var ps = new PointF[l];
            for (int i = 0; i < l; ++i) {
                ps[i] = new PointF(points[i, 0], points[i, 1]);
            }
            return ps;
        }
        #endregion

        public static Pathing.SvgPathSegmentList Segments(PointF[] points, bool close = true) {
            var l = new Pathing.SvgPathSegmentList();
            //
            PointF p0 = PointF.Empty;
            for (int i = 0; i < points.GetLength(0); ++i) {
                var p1 = points[i];
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

        public Element Add(Element element, Element parent = null) {
            (parent ?? document).Children.Add(element);
            return element;
        }

        public Element AddLine(PointF[] points, Element parent = null, string id = null) {
            var line = new SvgLine();
            line.ID = id ?? GetNextId("line");
            //
            line.StartX = points[0].X;
            line.StartY = points[0].Y;
            line.EndX = points[1].X;
            line.EndY = points[1].Y;
            //
            return Add(line, parent);
        }

        public Element AddPath(PointF[] points, bool close = true, Element parent = null, string id = null) {
            var path = new SvgPath();
            path.ID = id ?? GetNextId("path");
            //
            path.PathData = Segments(points, close);
            //
            return Add(path, parent);
        }

        public Element AddCircle(PointF point, float radius, Element parent = null, string id = null) {
            var circle = new SvgCircle();
            circle.ID = id ?? GetNextId("circle");
            //
            circle.CenterX = point.X;
            circle.CenterY = point.Y;
            circle.Radius = radius;
            //
            return Add(circle, parent);
        }

        public Element AddRectangle(PointF[] points, Element parent = null, string id = null) {
            var rect = new SvgRectangle();
            rect.ID = id ?? GetNextId("rect");
            //
            rect.X = points[0].X;
            rect.Y = points[0].Y;
            rect.Width  = points[1].X - points[0].X;
            rect.Height = points[1].Y - points[0].Y;
            //
            return Add(rect, parent);
        }

        public Element AddText(PointF pos, string text, float size, float leading = 1f, int anchor = 0, Element parent = null, string id = null) {
            var t = new SvgText();
            t.ID = id ?? GetNextId("text");

            t.TextAnchor = (SvgTextAnchor)anchor;

            t.X.Add(pos.X);
            t.Y.Add(pos.Y);
            t.FontSize = size;

            string[] parts = text.Split('\n');
            for (int i = 0; i < parts.Length; ++i) {
                var span = new SvgTextSpan { Text = parts[i] };
                span.X.Add(pos.X);
                span.Dy.Add(i == 0 ? 0 : (size * leading));
                t.Children.Add(span);
            }

            return Add(t, parent);
        }

        public void Save(string svgFileName) {
            document.Write(svgFileName, indent: false);
        }

    }

    public static class SvgExtensions {
        public static Element SetStyle(this Element element, Color? fill = null, Color? stroke = null, float width = 0f) {
            element.Fill = fill == null ? SvgColourServer.None : new SvgColourServer(fill.Value);
            element.Stroke = stroke == null ? SvgColourServer.None : new SvgColourServer(stroke.Value);
            element.StrokeWidth = width;
            return element;
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
            var image = new Image(400, 300);

            image.AddRectangle(Image.Points(0,0, 100,100))
                .SetStyle(Color.Pink);

            image.AddPath(Image.Points(0,0, 50,10, 100,0, 90,50, 100,100, 50,90, 0,100, 10,50))
                .SetStyle(null, Color.Aqua, 5);

            image.AddLine(Image.Points(0,0, 100,100))
                .SetStyle(null, Color.Red, 10);

            image.AddCircle(Image.Point(50, 50), 20)
                .SetStyle(null, Color.DarkGreen, 5);

            int n = 16;
            for (int i = 0; i <= n; ++i) {
                image.AddCircle(Image.Point(100, 100f * i/n), 2)
                    .SetStyle(Color.DarkMagenta);
            }

            image.AddText(Image.Point(50, 50), "Жил\nбыл\nпёсик", size: 50, leading: 0.7f, anchor: 2)
                .SetStyle(Color.DarkCyan, Color.Black, 0.5f);

            return image;
        }

        public static void Test() {
            string svgExportPath = @"Svg_sample_export.svg";

            //SvgDocument doc = Test1();
            //SvgDocument doc = Test2();
            //doc.Write(svgExportPath, indent: false);

            Image image = Test3();
            image.Save(svgExportPath);

            System.Diagnostics.Process.Start("chrome.exe", svgExportPath);
        }
    }
}
