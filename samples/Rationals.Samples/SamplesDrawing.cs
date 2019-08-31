#if !NETCOREAPP // System.Drawing.Graphics became in .Net Core 3.0 !!!
#define USE_GDI
#endif

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Xml;
using Color = System.Drawing.Color;
using Torec.Drawing;
using Rationals.Drawing;
using Rationals.Testing;

namespace Rationals
{
    public class RationalPlotter : IHandler<RationalInfo> {
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
    static class DrawingSamples
    {
        [Sample]
        static void Test6_Plotter()
        {
            var harmonicity = new SimpleHarmonicity(2.0);

            var viewport = new Torec.Drawing.Viewport(1200,600, 0,1200, 1,-1);
            var image = new Torec.Drawing.Image(viewport);

            var r0 = new Rational(1);
            var r1 = new Rational(2);
            var handler = new HandlerPipe<RationalInfo>(
                new RangeRationalHandler(r0, r1, false, true),
                new RationalPrinter(),
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

        internal static void DrawTest_Pjosik(Image image)
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

        [Sample]
        internal static void Test8_Pjosik() {
            // build image
            var imageSize = new System.Drawing.Point(600, 600);
            var viewport = new Viewport(imageSize.X, imageSize.Y, 0,20, 0,20, false);
            var image = new Image(viewport);

            DrawTest_Pjosik(image);

            // save/show svg
            image.Show(svg: true);

#if USE_GDI
            // save/show png
            using (var bitmap = new System.Drawing.Bitmap(imageSize.X, imageSize.Y, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
                using (var graphics = System.Drawing.Graphics.FromImage(bitmap)) {
                    image.Draw(graphics);
                }
                image.Show(svg: false); // png
            }
#endif
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

        [Sample]
        internal static void DrawGrid() {
            string harmonicityName = "Euler Barlow Tenney".Split()[1];

            var viewport = new Torec.Drawing.Viewport(1600,1200, -1,1, -3,3);
            var image = new Torec.Drawing.Image(viewport);

            var drawer = new GridDrawer();
            
            // configure drawer
            drawer.SetBounds(viewport.GetUserBounds());
            drawer.SetBase(2, null, null);
            drawer.SetGeneration(harmonicityName, 500);
            drawer.SetPointRadiusFactor(3f);
            drawer.SetEDGrids(new[] { new GridDrawer.EDGrid { baseInterval = new Rational(2), stepCount = 12 } });
            drawer.SetSlope(new Rational(3,2), 2.0f);

            // generate grid items
            drawer.UpdateItems();

            // make image elements from grid items
            drawer.DrawGrid(image, 0);

            // render/show svg/png
            image.Show(svg: true);
            image.Show(svg: false);
        }
    }

}
