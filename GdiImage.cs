using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;

namespace Torec.Drawing.Gdi {

    public class GdiImage : IImage
    {
        private Viewport _viewport;
        private InternalElement _root;

        #region PointF Utils
        private static PointF PointF(Point p) { return new PointF(p.X, p.Y); }
        private static SizeF SizeF(Point p) { return new SizeF(p.X, p.Y); }
        private static PointF[] PointF(Point[] ps) {
            var res = new PointF[ps.Length];
            for (int i = 0; i < ps.Length; ++i) res[i] = PointF(ps[i]);
            return res;
        }
        #endregion

        public GdiImage(Viewport viewport) {
            _viewport = viewport;
            _root = new InternalElement { Image = this };
        }

        public void Draw(Graphics g) {
            _root.Draw(g);
        }

        public Point[] GetBounds() { return _viewport.GetUserBounds(); }

        #region Elements
        private class InternalElement : Element { //!!! SvgElement would be better name
            internal List<InternalElement> Children = new List<InternalElement>();
            internal Color FillColor;
            internal Color StrokeColor;
            internal float StrokeWidth;
            //
            internal virtual void Draw(Graphics g) {
                for (int i = 0; i < Children.Count; ++i) {
                    Children[i].Draw(g);
                }
            }
        }
        private class ElementLine : InternalElement {
            internal Point[] Points;
            internal bool Close;
            //
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
        }
        private class ElementCircle : InternalElement {
            internal Point Pos;
            internal float Radius;
            //
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
        }
        private class ElementRectangle : InternalElement {
            internal Point[] Points;
            //
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
        }
        private class ElementText : InternalElement {
            internal Point Pos;
            internal string Text;
            internal float FontSize;
            internal float LineLeading;
            internal Align Align;
            internal bool CenterHeight;
            //
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
        }
        #endregion

        public Element Line(Point[] points) {
            points = _viewport.ToImage(points);
            return new ElementLine {
                Image = this,
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
                Image = this,
                Points = ps,
                Close = true,
            };
        }

        public Element Path(Point[] points, bool close = true) {
            points = _viewport.ToImage(points);
            return new ElementLine {
                Image = this,
                Points = points,
                Close = close,
            };
        }

        public Element Circle(Point pos, float radius) {
            pos = _viewport.ToImage(pos);
            radius = _viewport.ToImage(radius);
            return new ElementCircle {
                Image = this,
                Pos = pos,
                Radius = radius,
            };
        }

        public Element Rectangle(Point[] points) {
            points = _viewport.ToImage(points);
            return new ElementRectangle {
                Image = this,
                Points = points,
            };
        }

        public Element Text(Point pos, string text, float fontSize, float lineLeading = 1f, Align align = Align.Left, bool centerHeight = false) {
            pos = _viewport.ToImage(pos);
            fontSize = _viewport.ToImage(fontSize);
            return new ElementText {
                Image = this,
                Pos = pos,
                Text = text,
                FontSize = fontSize,
                LineLeading = lineLeading,
                Align = align,
                CenterHeight = centerHeight,
            };
        }

        public Element Group() {
            return new InternalElement {
                Image = this,
            };
        }

        public Element Add(Element element, Element parent = null, string id = null, int index = -1) {
            InternalElement e = (InternalElement)element;
            InternalElement p = parent != null ? (InternalElement)parent : _root;
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
            //e.ID = id; -- we need no ID for GDI
            return element;
        }

        public Element FillStroke(Element element, Color fill, Color stroke, float strokeWidth = 0f) {
            strokeWidth = _viewport.ToImage(strokeWidth);
            InternalElement e = (InternalElement)element;
            e.FillColor = fill;
            e.StrokeColor = stroke;
            e.StrokeWidth = strokeWidth;
            return element;
        }

    }

}
