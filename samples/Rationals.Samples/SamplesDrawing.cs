using Rationals.Testing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Xml;

using Torec.Drawing;
using Color = System.Drawing.Color;

namespace Rationals.Samples
{
    public class RationalPlotter : IHandler<RationalInfo> {
        Image _image;
        IHarmonicity _harmonicity;
        EqualDivision _temperament;
        //
        public RationalPlotter(Image image, IHarmonicity harmonicity) {
            _image = image;
            _harmonicity = harmonicity;
            _temperament = new EqualDivision(12, Rational.Two);
        }
        public int Handle(RationalInfo info) {
            Rational r = info.rational;
            float cents = (float)r.ToCents();
            float distance = (float)info.distance;
            float harm = 1f / distance;
            //float harm = Utils.GetHarmonicity(distance); // harmonicity: 0..1

            float x = cents; // 0..1200

            // use id as a tip
            string id = String.Format("{0} {1} {2} {3:F2} {4}",
                r.ToString(),
                r.FormatMonzo(),
                distance,
                r.ToCents(),
                _temperament.FormatRational(r)
            );

            _image.Line(Point.Points(x, 0, x, harm * 3))
                .Add(id: id)
                .FillStroke(Color.Empty, Color.LightGray, harm * 2);

            string fraction = r.FormatFraction("\n");
            _image.Text(new Point(x,0), fraction, harm * 2f, lineLeading: 0.8f, align: Image.Align.Center)
                .Add()
                .FillStroke(Color.Black, Color.Empty);

            return 1;
        }
    }

    [Test]
    static public class DrawingTests
    {
        [Sample]
        static void Test6_Plotter()
        {
            var harmonicity = HarmonicityUtils.CreateHarmonicity(null); // default

            var viewport = new Torec.Drawing.Viewport(1200,600, 0,1200, 1,-1);
            var image = new Torec.Drawing.Image(viewport);

            var r0 = new Rational(1);
            var r1 = new Rational(2);
            var handler = new HandlerPipe<RationalInfo>(
                new RangeRationalHandler(r0, r1, false, true),
                new Samples.RationalPrinter(),
                new RationalPlotter(image, harmonicity)
            );

            Debug.WriteLine("Iterate {0} range {1}-{2}", harmonicity.GetType().Name, r0, r1);

            var limits = new RationalGenerator.Limits { dimensionCount = 7, rationalCount = 50, distance = -1 };
            new RationalIterator(harmonicity, limits, null, handler).Iterate();

            image.Show(svg: true);
            //image.Show(svg: false);
        }

        [Sample]
        internal static void Test7_Svg() {
            // save svg
            string svgPath = "svg_test1_temp.svg";
            var svgWriterSettings = new XmlWriterSettings {
                Indent = true,
                //OmitXmlDeclaration = true,
            };
            using (XmlWriter w = XmlWriter.Create(svgPath, svgWriterSettings)) {
                w.WriteStartDocument();
                w.WriteComment(" bntr ");

                //w.WriteDocType("svg", "-//W3C//DTD SVG 1.1//EN", "http://www.w3.org/Graphics/SVG/1.1/DTD/svg11.dtd", null);
                w.WriteStartElement("svg", "http://www.w3.org/2000/svg");
                w.WriteAttributeString("version", "1.1");
                w.WriteAttributeString("width", "600");
                w.WriteAttributeString("height", "600");

                w.WriteStartElement("rect");
                w.WriteAttributeString("width", "50%");
                w.WriteAttributeString("height", "50%");
                w.WriteAttributeString("fill", "red");

                w.WriteEndElement();

                w.WriteEndElement();
                w.WriteEndDocument();
            }

            // open svg
            Image.Show(svgPath);
        }

        public static void DrawTest_Pjosik(Image image)
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

            image.Text(new Point(5, 5), "81\n80\n79", fontSize: 1f, lineLeading: 0.7f, align: Image.Align.Center, centerHeight: true)
                .Add()
                .FillStroke(Color.Black, Color.Empty);
        }

        [Sample]
        internal static void Test8_Pjosik() {
            // build image
            var imageSize = new System.Drawing.Point(600, 600);
            var viewport = new Viewport(imageSize.X, imageSize.Y, 0,20, 0,20, false);
            var image = new Image(viewport);

            DrawTest_Pjosik(image);

            // save/show svg
            image.Show(svg: true);

            // save/show png
            image.Show(svg: false);
        }

        [Sample]
        internal static void Test38() {

            int frameCount = 1;
            for (int fi = 0; fi < frameCount; ++fi) {

                int iS = 600;
                float iR = 8; // image radius
                var viewport = new Viewport(iS,iS, -iR,iR, -iR,iR, true);
                var image = new Image(viewport);

                image.Rectangle(new[] { new Point(-iR,iR), new Point(iR,-iR) })
                    .Add()
                    .FillStroke(Color.White, Color.Empty);

                // draw
                for (int i = 0; i < 4; ++i) {
                    int count = 8 + i;
                    double k = Math.Pow(1.8, i);
                    double R = 1.0 * k; // ring radius
                    double r = 0.25 * k; // point radius
                    double c = 0.08 * k; // center shift
                    int val = (int)(0x22 + 0x99 * Math.Pow(i / 3.0, 0.55));
                    Color color = Color.FromArgb(val, val, val);

                    for (int j = 0; j < count; ++j) {
                        double a = Math.PI * 2 * j / count;
                        a += Math.PI * 2 * 3 / 4;
                        a += Math.PI*2 * 0.5 / count;
                        //a += Math.PI*2 * fi/frameCount / count;
                        double x = R * Math.Cos(a);
                        double y = R * Math.Sin(a);
                        Point pos = new Point((float)x, (float)(y - c));
                        image.Circle(pos, (float)r)
                            .Add()
                            .FillStroke(color, Color.Empty);
                    }

                }

                // save/open svg
                string svgPath = "38.svg";
                image.WriteSvg(svgPath);
                Image.Show(svgPath);

                /*
                // save to png
                string pngPath = String.Format("38_{0:00}.png", fi);
                image.WritePng(pngPath, true);
                */
            }

        }

#if true
        [Sample]
        internal static void DrawGrid() {
            string harmonicityName = "Euler Barlow Tenney Narrow".Split()[0];
            Rational[] subgroup = Rational.ParseRationals("2.3.5");

            /*
            var harmonicity = HarmonicityUtils.CreateHarmonicity(harmonicityName);
            foreach (string s in "5/6 4/3".Split()) {
                Rational r = Rational.Parse(s);
                Debug.WriteLine("{0} d:{1} h:{2}", 
                    r, harmonicity.GetDistance(r), harmonicity.GetHarmonicity(r)
                );
            }
            */

            var viewport = new Torec.Drawing.Viewport(1600,1200, -1,1, -3,3);
            var image = new Torec.Drawing.Image(viewport);

            var drawer = new Rationals.Drawing.GridDrawer();

            // configure drawer
            drawer.SetBounds(viewport.GetUserBounds());
            // below like UpdateDrawerFully()
            drawer.SetSubgroup(0, subgroup, null);
            drawer.SetGeneration(harmonicityName, 200);
            drawer.SetSlope(new Rational(3,2), 2.0f);
            //
            drawer.SetEDGrids(Rationals.Drawing.GridDrawer.EDGrid.Parse("12edo"));
            drawer.SetPointRadius(1.0f);

            // generate grid items
            drawer.UpdateItems();

            // make image elements from grid items
            drawer.DrawGrid(image);

            // render/show svg
            image.Show(svg: true);
        }
#endif

        #region Draw2020
        internal static Complex ToComplex(Point p) { return new Complex(p.X, p.Y); }
        internal static Point FromComplex(Complex c) { return new Point((float)c.Real, (float)c.Imaginary); }

        internal struct Item {
            public Complex[] points;

            public Item Clone() {
                return new Item { points = (Complex[])points.Clone() };
            }
            public void Transform(Func<Complex, Complex> transform) {
                for (int i = 0; i < points.Length; ++i) {
                    points[i] = transform(points[i]);
                }
            }
            public static Item UnitCircle(int steps = 32) {
                var points = new Complex[steps];
                for (int i = 0; i < steps; ++i) {
                    points[i] = MakePhase(1.0 * i/steps);
                }
                return new Item { points = points };
            }

        }

        internal static Complex MakePhase(double k) {
            return Complex.FromPolarCoordinates(1.0, Math.PI*2 * k);
        }

        internal static List<Item> Multiply(List<Item> items, int count, double scale, double rotatePhase = 0.0) {
            var result = new List<Item>();
            for (int i = 0; i < count; ++i) {
                foreach (var item in items) {
                    var clone = item.Clone();
                    clone.Transform(c => {
                        c *= scale;
                        c += new Complex(1.0, 0.0);
                        //
                        c = Complex.Pow(c, 4.0 / count);
                        c *= MakePhase(1.0 * i / count);
                        //
                        if (rotatePhase != 0) {
                            c *= MakePhase(rotatePhase);
                        }
                        //
                        return c;
                    });
                    result.Add(clone);
                }
            }
            return result;
        }

        internal static void MakeCircle(Complex[] points, out Complex pos, out double radius) {
            pos = Complex.Zero;
            foreach (Complex p in points) pos += p;
            pos /= points.Length;
            //
            radius = 0;
            foreach (Complex p in points) radius += (p - pos).Magnitude;
            radius /= points.Length;
        }

        internal static Color MakeColor(UInt32 color) {
            unchecked {
                return Color.FromArgb((int)color);
            }
        }

        [Sample]
        internal static void Draw2020()
        {
            int imageSize = 700;
            float radius = 1.7f; // image radius
            var viewport = new Viewport(imageSize, imageSize, -radius, radius, -radius, radius, true);

            int frameCount = 40;
            bool isSingleSvg = frameCount == 1;

            for (int fi = 0; fi < frameCount; ++fi) {

                double fa = 1.0 * fi/frameCount;

                var image = new Image(viewport);

                if (!isSingleSvg) {
                    image.Rectangle(new[] { new Point(-radius, radius), new Point(radius, -radius) })
                        .Add()
                        //.FillStroke(MakeColor(0xFFFFEEFF), Color.Empty);
                        .FillStroke(Color.White, Color.Empty);
                }

                var items = new List<Item> { Item.UnitCircle() };

                items = Multiply(items, 4, 0.5,     -fa/4);
                items = Multiply(items, 5, 0.44,    fa/5);      // -> 20

                items = Multiply(items, 5, 0.4,     fa/5);      // -> 100

                var c1 = Item.UnitCircle();
                c1.Transform(c => c * 0.45);
                items.Add(c1);                                  // -> 101

                items = Multiply(items, 4, 0.45,    -fa/4);
                items = Multiply(items, 5, 0.44,    fa/5);      // x 20 -> 2020


                // Draw all items
                foreach (Item item in items) {
                    //
                    item.Transform(c => c * MakePhase(0.25));
                    //
                    Complex pos; double r;
                    MakeCircle(item.points, out pos, out r);
                    image.Circle(FromComplex(pos), (float)r)
                        .Add()
                        //.FillStroke(Color.Empty, Color.Red, 0.001f);
                        //.FillStroke(Color.Gray, Color.Black, 0.001f);
                        .FillStroke(MakeColor(0xFF555555), Color.Empty);
                }

                if (isSingleSvg) {
                    // save/open svg
                    string svgPath = "2020.svg";
                    image.WriteSvg(svgPath);
                    Image.Show(svgPath);
                } else {
#if !NETCOREAPP
                    // save to png
                    string pngPath = String.Format("frames\\2020_{0:00}.png", fi);
                    image.WritePng(pngPath, true);
                    //Image.Show(pngPath);
#endif
                }
            }
        }
        #endregion Draw2020

        static Point[] Points(float[] coords) {
            int count = coords.Length / 2;
            var points = new Point[count];
            for (int i = 0; i < count; ++i) {
                points[i] = new Point(coords[i*2], coords[i*2+1]);
            }
            return points;
        }

        [Sample]
        internal static void Test9_ColorVertices() {
            var imageSize = new System.Drawing.Point(1000, 1000);
            var viewport = new Viewport(imageSize.X, imageSize.Y, 0,10, 0,10, true);
            var image = new Image(viewport);

            Point[] points = Points(new float[] {
                0, 0,
                10, 0,
                10, 10,
                0, 10,
                5, 5,
                5, 5,
            });
            Color[] colors = new[] {
                Color.Red,
                Color.Green,
                Color.Blue,
                Color.Yellow,
                Color.Orange,
                Color.Magenta,
            };

            //image.Mesh(points, colors, new int[] { 0,1,4, 1,5,2 }).Add();

            //image.Mesh(points, colors, new int[] { 4,0,1,2,3,0 }, Image.VertexMode.TriangleFan).Add();

            image.Mesh(points, colors, new int[] { 0,1,4,2 }, Image.VertexMode.TriangleStrip).Add();
            image.Mesh(points, colors, new int[] { 0,3,5,2 }, Image.VertexMode.TriangleStrip).Add();

            //image.Show(svg: true);
            image.Show(svg: false, smooth: false);
        }

        [Sample]
        internal static void Test10_DrawTimeline()
        {
            var timeline = new Torec.Timeline();

            double[] timeRange  = new double[] { 0, 6 };
            double[] valueRange = new double[] { -4, 4 };

            int channelCount = 5;
            var channels = new Torec.Channel[channelCount];
            for (int i = 0; i < channelCount; ++i) {
                channels[i] = timeline.MakeChannel(
                    new Torec.ChannelInfo(
                        valueRange[0],
                        valueRange[1],
                        name: String.Format("c{0}", i)
                    )
                );
            }

            double[] times  = new double[] { 0, 1, 5, 2, 4, 5 };
            double[] values = new double[] { 0, 1, 1, 1, 1, 1 };
            for (int i = 0; i < channelCount; ++i) {
                timeline.AddKeyFrames(channels[i], times, values, count: i + 2);
            }

            var viewport = new Viewport(
                1000, 600,
                (float)timeRange[0], 
                (float)timeRange[1], 
                (float)valueRange[0],
                (float)valueRange[1],
                yUp: true
            );
            var image = timeline.Draw(viewport);

            image.Show(svg: true);
        }

    }

}
