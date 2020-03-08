// System.Drawing.Graphics is missing in .Net Core 2.x
// See Target Framework Symbols: https://docs.microsoft.com/en-us/dotnet/core/tutorials/libraries
#if NETFRAMEWORK || NETCOREAPP3_0 || NETCOREAPP3_1
#define USE_GDI
#endif

using System;
using System.Collections.Generic;
//using System.Linq;
using System.Text;
using System.Drawing;
using System.Xml;

namespace Torec.Drawing {

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

        public Point Mul(Point scale) { return new Point(X * scale.X, Y * scale.Y); }
        public Point Div(Point scale) { return new Point(X / scale.X, Y / scale.Y); }
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
        //
        public static Point Invalid = new Point(float.NaN, float.NaN);
        public bool IsValid() { return !float.IsNaN(X) && !float.IsNaN(Y); }
        public bool IsEmpty() { return X == 0 && Y == 0; }

        public static Point Empty = new Point(0, 0);
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

    public class Image
    {
        private IViewport _viewport;
        private Element _root;

#region PointF Utils
        private static PointF PointF(Point p) { return new PointF(p.X, p.Y); }
        private static SizeF SizeF(Point p) { return new SizeF(p.X, p.Y); }
        private static PointF[] PointF(Point[] ps) {
            var res = new PointF[ps.Length];
            for (int i = 0; i < ps.Length; ++i) res[i] = PointF(ps[i]);
            return res;
        }
#endregion

        public Image(IViewport viewport) {
            _viewport = viewport;
            _root = new Element { Owner = this };
        }

        public Point GetSize() {
            return _viewport.GetImageSize();
        }

#if USE_GDI
        public void Draw(Graphics g) {
            _root.Draw(g);
        }
        public void WritePng(string pngPath, bool smooth = false) {
            Point size = _viewport.GetImageSize();
            using (var bitmap = new Bitmap((int)size.X, (int)size.Y, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
                using (var graphics = Graphics.FromImage(bitmap)) {
                    if (smooth) {
                        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                    }
                    this.Draw(graphics);
                }
                bitmap.Save(pngPath);
            }
        }
#endif

#region Svg
        public void WriteSvg(string svgPath, bool indent = false)
        {            
            //set separator to "." - ugly !!!
            System.Globalization.CultureInfo prevCulture = null;
            var thread = System.Threading.Thread.CurrentThread;
            if (thread.CurrentCulture.NumberFormat.NumberDecimalSeparator != ".") {
                prevCulture = thread.CurrentCulture;
                var ci = new System.Globalization.CultureInfo(thread.CurrentCulture.LCID);
                ci.NumberFormat.NumberDecimalSeparator = ".";
                thread.CurrentCulture = ci;
            }

            var svgWriterSettings = new XmlWriterSettings {
                Indent = indent,
            };
            var imageSize = _viewport.GetImageSize();
            using (XmlWriter w = XmlWriter.Create(svgPath, svgWriterSettings)) {
                w.WriteStartDocument();
                //w.WriteDocType("svg", "-//W3C//DTD SVG 1.1//EN", "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd", null);
                w.WriteStartElement("svg", "http://www.w3.org/2000/svg");
                w.WriteAttributeString("version", "1.1");
                w.WriteAttributeString("width", imageSize.X.ToString());
                w.WriteAttributeString("height", imageSize.Y.ToString());

                w.WriteAttributeString("font-family", "Arial");

                _root.WriteSvg(w); // write xml recursively

                w.WriteEndElement();
                w.WriteEndDocument();
            }

            if (prevCulture != null) {
                thread.CurrentCulture = prevCulture;
            }
        }
#endregion

        public void Show(bool svg = true) {
            string filePath = "temp_image_export" + (svg ? ".svg" : ".png");
            if (svg) {
                bool indent = false;
                //indent = true;
                WriteSvg(filePath, indent);
            } else {
#if USE_GDI
                WritePng(filePath, smooth: true);
#else
                return;
#endif
            }
            Show(filePath);
        }

        public static void Show(string filePath) { // open file in default app
            new System.Diagnostics.Process {
                StartInfo = new System.Diagnostics.ProcessStartInfo {
                    FileName = filePath,
                    UseShellExecute = true
                }
            }.Start();
        }

        public enum Align {
            Default = 0,
            Left    = 1,
            Center  = 2,
            Right   = 3,
        }

#region Elements
        public class Element {
            //
            internal Image Owner;
            internal string ID; // for Svg only
            //
            internal List<Element> Children = new List<Element>();
            internal Color FillColor;
            internal Color StrokeColor;
            internal float StrokeWidth;

            // sugar
            public Element Add(Element parent = null, string id = null, int index = -1) {
                return Owner.Add(this, parent, id, index);
            }
            public Element FillStroke(Color fill, Color stroke, float strokeWidth = 0f) {
                return Owner.FillStroke(this, fill, stroke, strokeWidth);
            }

            // Gdi
#if USE_GDI
            internal virtual void Draw(Graphics g) {
                for (int i = 0; i < Children.Count; ++i) {
                    Children[i].Draw(g);
                }
            }
#endif

            // Svg
            internal virtual void WriteSvg(XmlWriter w) {
                for (int i = 0; i < Children.Count; ++i) {
                    Children[i].WriteSvg(w);
                }
            }
            internal static string SvgFormatColor(Color color) {
                if (color == Color.Empty) return "none";
                return String.Format("rgb({0},{1},{2})", color.R, color.G, color.B);
            }
            internal string SvgGetStyle() {
                // e.g. style="fill:rgb(0,0,255);stroke-width:3;stroke:rgb(0,0,0)"
                string style = "";
                style += "fill:" + SvgFormatColor(FillColor  ) + ";";
                if (StrokeColor != Color.Empty) style += "stroke:" + SvgFormatColor(StrokeColor) + ";";
                if (StrokeWidth != 0) style += "stroke-width:" + StrokeWidth.ToString() + ";";
                return style;
            }
        }
        private class ElementLine : Element {
            internal Point[] Points;
            internal bool Close;
            //
#if USE_GDI
            internal override void Draw(Graphics g) {
                PointF[] points = PointF(Points);
                if (FillColor != Color.Empty) {
                    using (var brush = new SolidBrush(FillColor)) {
                        g.FillPolygon(brush, points);
                    }
                }
                if (StrokeColor != Color.Empty) {
                    if (Close) {
                        PointF[] ps = new PointF[points.Length + 1];
                        points.CopyTo(ps, 0);
                        ps[ps.Length - 1] = ps[0]; // close path
                        points = ps;
                    }
                    using (var pen = new Pen(StrokeColor, (int)StrokeWidth)) {
                        g.DrawLines(pen, points);
                    }
                }
                base.Draw(g);
            }
#endif
            internal override void WriteSvg(XmlWriter w) {
                // e.g. <polyline points="20,20 40,25 60,40 80,120 120,140 200,180" style="fill:none;stroke:black;stroke-width:3" />
                // e.g. <polygon points="200,10 250,190 160,210" style="fill:lime;stroke:purple;stroke-width:1" />
                string points = "";
                for (int i = 0; i < Points.Length; ++i) {
                    if (i != 0) points += " ";
                    points += String.Format("{0},{1}", Points[i].X, Points[i].Y);
                }
                w.WriteStartElement(Close ? "polygon" : "polyline");
                w.WriteAttributeString("points", points);
                w.WriteAttributeString("style", SvgGetStyle());
                base.WriteSvg(w);
                w.WriteEndElement();
            }
        }
        private class ElementCircle : Element {
            internal Point Pos;
            internal float Radius;
            //
#if USE_GDI
            internal override void Draw(Graphics g) {
                var radius = new SizeF(Radius, Radius);
                var rect = new RectangleF(PointF(Pos) - radius, radius + radius);
                if (FillColor != Color.Empty) {
                    using (var brush = new SolidBrush(FillColor)) {
                        g.FillEllipse(brush, rect);
                    }
                }
                if (StrokeColor != Color.Empty) {
                    using (var pen = new Pen(StrokeColor, (int)StrokeWidth)) {
                        g.DrawEllipse(pen, rect);
                    }
                }
                base.Draw(g);
            }
#endif
            internal override void WriteSvg(XmlWriter w) {
                // e.g. <circle cx="50" cy="50" r="40" stroke="black" stroke-width="3" fill="red" />
                w.WriteStartElement("circle");
                w.WriteAttributeString("cx", Pos.X.ToString());
                w.WriteAttributeString("cy", Pos.Y.ToString());
                w.WriteAttributeString("r", Radius.ToString());
                w.WriteAttributeString("style", SvgGetStyle());
                base.WriteSvg(w);
                w.WriteEndElement();
            }
        }
        private class ElementRectangle : Element {
            internal Point[] Points;
            //
#if USE_GDI
            internal override void Draw(Graphics g) {
                var rect = new RectangleF(PointF(Points[0]), SizeF(Points[1] - Points[0]));
                if (FillColor != Color.Empty) {
                    using (var brush = new SolidBrush(FillColor)) {
                        g.FillRectangle(brush, rect);
                    }
                }
                if (StrokeColor != Color.Empty) {
                    using (var pen = new Pen(StrokeColor, (int)StrokeWidth)) {
                        g.DrawRectangles(pen, new[] { rect });
                    }
                }
                base.Draw(g);
            }
#endif
            internal override void WriteSvg(XmlWriter w) {
                var pos = Points[0];
                var size = Points[1] - Points[0];
                // e.g. <rect width="300" height="100" style="fill:rgb(0,0,255);stroke-width:3;stroke:rgb(0,0,0)" />
                w.WriteStartElement("rect");
                w.WriteAttributeString("x", pos.X.ToString());
                w.WriteAttributeString("y", pos.Y.ToString());
                w.WriteAttributeString("width",  size.X.ToString());
                w.WriteAttributeString("height", size.Y.ToString());
                w.WriteAttributeString("style", SvgGetStyle());
                base.WriteSvg(w);
                w.WriteEndElement();
            }
        }
        private class ElementText : Element {
            internal Point Pos;
            internal string Text;
            internal float FontSize;
            internal float LineLeading;
            internal Align Align;
            internal bool CenterHeight;
            //
#if USE_GDI
            internal override void Draw(Graphics g) {
                Point pos = Pos;
                //
                string[] parts = Text.Split('\n');
                //
                using (var font = new Font("Arial", FontSize, GraphicsUnit.Pixel))
                using (var style = new StringFormat()) {
                    if (Align > 0) {
                        style.Alignment = (StringAlignment)(Align - 1);
                    }
                    if (CenterHeight) {
                        float fontHeight = 0.75f; // real letter part for Arial
                        float fullHeight = (parts.Length - 1) * LineLeading + fontHeight; // text full height
                        pos.Y -= (fullHeight / 2 - fontHeight) * FontSize; // vertical shift of first line level
                    }
                    if (FillColor != Color.Empty) {
                        using (var brush = new SolidBrush(FillColor)) {
                            for (int i = 0; i < parts.Length; ++i) {
                                Point p = pos;
                                p.Y += FontSize * LineLeading * i;
                                p.Y -= FontSize * 0.9f; // GDI specific
                                g.DrawString(parts[i], font, brush, PointF(p), style);
                            }
                        }
                    }
                }
                //
                base.Draw(g);
            }
#endif

            private static readonly string[] _textAnchor = new string[] { "inherit", "start", "middle", "end" };
            internal override void WriteSvg(XmlWriter w) {
                // e.g. <text x="0" y="15" fill="red">I love SVG!</text>
                Point pos = Pos;
                string[] parts = Text.Split('\n');
                if (CenterHeight) {
                    float fontHeight = 0.75f; // real letter part for Arial
                    float fullHeight = (parts.Length - 1) * LineLeading + fontHeight; // text full height
                    pos.Y -= (fullHeight / 2 - fontHeight) * FontSize; // vertical shift of first line level
                }
                //
                w.WriteStartElement("text");
                w.WriteAttributeString("x", pos.X.ToString());
                w.WriteAttributeString("y", pos.Y.ToString());
                w.WriteAttributeString("font-size", FontSize.ToString());
                w.WriteAttributeString("text-anchor", _textAnchor[(int)Align]);
                w.WriteAttributeString("style", SvgGetStyle());
                //
                for (int i = 0; i < parts.Length; ++i) {
                    w.WriteStartElement("tspan");
                    w.WriteAttributeString("x", pos.X.ToString());
                    w.WriteAttributeString("dy", (i == 0 ? 0 : (FontSize * LineLeading)).ToString());
                    w.WriteString(parts[i]);
                    w.WriteEndElement();
                }
                //
                base.WriteSvg(w);
                w.WriteEndElement();
            }
        }
#endregion

#region Creating elements

        public Element Line(Point[] points) {
            points = Utils.ToImage(_viewport, points);
            return new ElementLine {
                Owner = this,
                Points = points,
                Close = false,
            };
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

            return new ElementLine {
                Owner = this,
                Points = ps,
                Close = true,
            };
        }

        public Element Path(Point[] points, bool close = true) {
            points = Utils.ToImage(_viewport, points);
            return new ElementLine {
                Owner = this,
                Points = points,
                Close = close,
            };
        }

        public Element Circle(Point pos, float radius) {
            pos = _viewport.ToImage(pos);
            radius = _viewport.ToImage(radius);
            return new ElementCircle {
                Owner = this,
                Pos = pos,
                Radius = radius,
            };
        }

        public Element Rectangle(Point[] points) {
            points = Utils.ToImage(_viewport, points);
            return new ElementRectangle {
                Owner = this,
                Points = points,
            };
        }

        public Element Text(Point pos, string text, float fontSize, float lineLeading = 1f, Align align = Align.Left, bool centerHeight = false) {
            pos = _viewport.ToImage(pos);
            fontSize = _viewport.ToImage(fontSize);
            if (fontSize == 0) throw new Exception("Invalid viewport");
            return new ElementText {
                Owner = this,
                Pos = pos,
                Text = text,
                FontSize = fontSize,
                LineLeading = lineLeading,
                Align = align,
                CenterHeight = centerHeight,
            };
        }

        public Element Group() {
            return new Element {
                Owner = this,
            };
        }

#endregion

        public Element Add(Element element, Element parent = null, string id = null, int index = -1) {
            Element e = (Element)element;
            Element p = parent != null ? (Element)parent : _root;
            // Add to parent's children
            if (index == -1 || p.Children.Count == 0) {
                p.Children.Add(e);
            } else {
                if (index < 0) {
                    index += p.Children.Count + 1;
                }
                p.Children.Insert(index, e);
            }
            e.ID = id;
            return element;
        }

        public Element FillStroke(Element element, Color fill, Color stroke, float strokeWidth = 0f) {
            strokeWidth = _viewport.ToImage(strokeWidth);
            Element e = (Element)element;
            e.FillColor = fill;
            e.StrokeColor = stroke;
            e.StrokeWidth = strokeWidth;
            return element;
        }

    }

}
