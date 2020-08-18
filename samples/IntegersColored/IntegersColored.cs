#define VIEW_LOG_X
#define VIEW_EXP_Y

// plot y=Prime[x] x=0..2000
// https://www.wolframalpha.com/input/?i=plot+y%3DPrime%5Bx%5D+x%3D0..2000

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

using Torec.Drawing;
using Color = System.Drawing.Color;

namespace Rationals.IntegersColored
{
    struct Point {
        public double x, y;
    }

    public class Painting : ImageInput
    {
        class Item {
            public int integer;
            public Rational rational;

            // natural tree growing step
            public Item parent = null;
            public int stepPrimeIndex;

            // pos
            public Point pos; // NB! x flipped: 1..0 (zero at right)
        }

        int _itemCountPow = 0;
        Item[] _items = null;

        Viewport _viewport = null;
        Image _image = null;

        public Painting(int itemCountPow = 9) {
            _itemCountPow = itemCountPow;
            ResetItems();
        }

        protected void ResetItems()
        {
            int itemCount = 1 << _itemCountPow;
            _items = new Item[itemCount];

            for (int i = 0; i < itemCount; ++i) {
                Rational r = new Rational(i + 1, 1);
                Item item = new Item {
                    integer = i + 1,
                    rational = r
                };
                //
                if (i == 0) { // root
                    item.parent = null;
                    item.stepPrimeIndex = -1;
                } else {
                    int j = 0; // find step prime index
                    while (r.GetPrimePower(j) == 0) j += 1;

                    Rational p = r / Rational.Prime(j);
                    item.parent = _items[(int)p.ToInt() - 1];
                    item.stepPrimeIndex = j;
                }
                //
                _items[i] = item;
            }
        }

        public void SetSize(int pixelWidth, int pixelHeight)
        {
            _viewport = new Viewport(
                pixelWidth,
                pixelHeight,
#if VIEW_LOG_X
                0, _itemCountPow, -1f, 2f,
#else
                0f, 1f, -1f, 2f,
#endif
                yUp: true
            );
        }

        public Image Draw()
        {
            if (_viewport == null) return null;

            float boundX = _viewport.GetUserBounds()[1].X;

            _image = new Image(_viewport); // recreate image
            
            // fill blank
            _image.Rectangle(_viewport.GetUserBounds())
                .Add()
                .FillStroke(Color.White, Color.Empty);

            if (_items == null) return _image;

            var groupLines   = _image.Group().Add();
            var groupCircles = _image.Group().Add();
            var groupNumbers = _image.Group().Add();

            foreach (Item item in _items)
            {
                if (item.parent != null && item.parent.parent != null) {
                    if (item.parent.pos.x == 0f || item.parent.pos.x > boundX) {
                        //continue;
                    }
                }

                item.pos = GetPos(item);

                var viewPos = GetViewPos(item.pos);
                float radius = GetViewRadius(item);

                if (item.parent != null)
                {
                    // draw segment-line from the parent
                    int segmentCount = item.parent.parent == null ? 100 : 10;
                    //int segmentCount = 1;
                    var points = Enumerable.Range(0, segmentCount+1)
                        .Select(j => GetPos(item, (double)j/segmentCount))
                        .Select(p => GetViewPos(p))
                        .ToArray();
                    _image.Line(points)
                        .Add(groupLines)
                        .FillStroke(Color.Empty, Color.Gray, strokeWidth: radius * 0.1f);
                }

                _image.Circle(viewPos, radius)
                    .Add(groupCircles)
                    .FillStroke(Color.Gray, Color.Empty);

                string text = item.integer.ToString();
                //if (item.parent != null) text = item.parent.integer.ToString() + "->" + text;
                _image.Text(viewPos, text, radius, align: Image.Align.Center, centerHeight: true)
                    .Add(groupNumbers)
                    .FillStroke(Color.Black, Color.Empty);
            }

            return _image;
        }

#region Logic

#if false
        static double GetHarmonicDistance(double[] pows) {
            double d = 0.0;
            for (int i = 0; i < pows.Length; ++i) {
                double e = pows[i];
                if (e != 0) {
                    double p = Utils.GetPrime(i);
                    //d += Math.Abs(e) * 2.0*(p-1)*(p-1)/p; // Barlow

                    //e = Math.Pow(e, 0.9);
                    e = Math.Pow(e, State.Left.x * 2.0);
                    p = 2.0 * (p-1)*(p-1)/p;

                    d += e * p;
                }
            }
            return d;
        }

        static double GetHarmonicity(double distance) {
            double h = 10.0 / (distance + 10.0);
            return h;
        }

        static Point GetItemPos(double number, double[] pows) {
            double x = Math.Log(number, 2);

            double d = GetHarmonicDistance(pows);
            double h = GetHarmonicity(d);
            double y = 1.0 - h;

            return new Point((float)x, (float)y);
        }

        static double[] GetFloatPowers(Item item, int minCount = 0) {
            int[] pows = item.rational.GetPrimePowers();
            if (pows.Length < minCount) Array.Resize(ref pows, minCount);
            return pows.Select(e => (double)e).ToArray();
        }

        static Point GetItemPos(Item item) {
            double number = item.integer;
            
            double[] pows = GetFloatPowers(item);
            
            return GetItemPos(number, pows);
        }

        static Point GetItemPos(Item item, float stepPhase) {
            int stepPrime = (int)Rational.Prime(item.stepPrimeIndex).ToInt();
            double number = item.parent.integer * Math.Pow(stepPrime, stepPhase);

            double[] pows = GetFloatPowers(item.parent, minCount: item.stepPrimeIndex + 1);
            pows[item.stepPrimeIndex] += stepPhase;

            return GetItemPos(number, pows);
        }
#else
        static Point GetPos(Item item, double stepPhase = 1.0)
        {
            if (item.parent == null) { // root is (1)
                return new Point { x = 1, y = 0 };
            }

            int primeIndex = item.stepPrimeIndex;
            int prime = (int)Rational.Prime(primeIndex).ToInt();

            //!!! coefE also used in GetViewPos
            double coefE = Math.Pow(2, 3.0 * State.Left.X(-1, 1, def: 0)); // ..0.5.. 1 ..2..
            double primeX = 1.0 / Math.Pow(prime, coefE); // 2 ~> 0.5, 3 ~> 0.33,..

            double right = item.parent.pos.x;
            double stepX = right * (1.0 - primeX);

            stepX *= stepPhase;

            double scaleY = Math.Pow(2.0, 10.0 * State.Left.Y(-1, 1, def: 0)); // ..0.5.. 1 ..2..
            double powY   = Math.Pow(2.0, 3.0 * State.AltLeft.Y(-1, 1, def: 0)); // ..0.5.. 1 ..2..
            double primeG = Math.Pow(primeIndex, powY) * scaleY;

            // normalize G by (3)
            double prime3X = Math.Pow(1.0 / 3, coefE);
            primeG /= 1.0 - prime3X;

            double x = item.parent.pos.x - stepX;
            double y = item.parent.pos.y + stepX * primeG;

            return new Point { x = x, y = y };
        }

        static Torec.Drawing.Point GetViewPos(Point pos) {
            double x = pos.x;
            double y = pos.y;
#if VIEW_LOG_X
            double n = 1.0 / x; // get out of hyperbolic

            double coefE = Math.Pow(2, 3.0 * State.Left.X(-1, 1, def: 0)); // ..0.5.. 1 ..2..

            x = Math.Log(n, Math.Pow(2, coefE)); // put to logarithmic
#else
            x = 1.0 - x; // just flip it back
#endif
#if VIEW_EXP_Y
            y = 1.0 - Math.Exp(-y); // 0..N -> 0..1
#endif
            return new Torec.Drawing.Point((float)x, (float)y);
        }
        static float GetViewRadius(Item item) {
            double n = item.integer;
            double x = Math.Log(n, 2);
            double r = Math.Pow(1.5, -x) *
                State.CtrlRight.Y(1, 10, 2)
                * 0.05;
            return (float)r;
        }

#endif

#endregion Logic


#region Handle ImageInput calls
        public override bool OnSize(double newWidth, double newHeight) {
            base.OnSize(newWidth, newHeight);

            this.SetSize(
                (int)newWidth,
                (int)newHeight
            );

            return true; // request redrawing
        }
        public override bool OnMouseMove(double x, double y, Buttons buttons) {
            base.OnMouseMove(x, y, buttons);

            return true; // request redrawing
        }

        public override Image Redraw() {
            return this.Draw();
        }
#endregion Handle ImageInput calls
    }

    static class Program
    {
        static void Test1() {
            int pow = 7;

            var painting = new Painting(pow);

            int pixelWidth = pow * 200;
            int pixelHeight = 500;

            painting.SetSize(pixelWidth, pixelHeight);

            Image image = painting.Draw();
            if (image != null) {
                image.Show(svg: true);
                //image.Show(svg: false, smooth: true);
            }
        }


        static int Main() {
            Test1();
            return 0;
        }
    }
}