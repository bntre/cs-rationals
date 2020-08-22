#define VIEW_LOG_X
#define VIEW_EXP_Y
#define USE_TIMELINE

// plot y=Prime[x] x=0..2000
// https://www.wolframalpha.com/input/?i=plot+y%3DPrime%5Bx%5D+x%3D0..2000

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

using Torec.Input;
using Torec.Drawing;
using Color = System.Drawing.Color;

namespace Rationals.IntegersColored
{
    struct Point {
        public double x, y;
    }

    public class Painting : IImageInput
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

        WindowInput _windowInput = new WindowInput();

        Viewport _viewport = null;
        Image _image = null;

        public Painting(int itemCountPow = 9) {
            InitChannels();
            
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

        #region Channels
        Torec.Channel _primeE;
        Torec.Channel _scaleY;
        Torec.Channel _powY;
        Torec.Channel _radius;
        Torec.Channel _globalScaleY;
#if USE_TIMELINE
        Torec.Channel _time;
        Torec.Timeline _timeline = new Torec.Timeline();
#endif

        private void InitChannels() {
            const int X = 0;
            const int Y = 1;

            var primeE          = new Torec.ChannelInfo(-1,1, 0, "primeE");
            var scaleY          = new Torec.ChannelInfo(-1,1, 0, "scaleY");
            var powY            = new Torec.ChannelInfo(-1,1, 0, "powY");
            var radius          = new Torec.ChannelInfo(-1,1, 0, "radius");
            var globalScaleY    = new Torec.ChannelInfo( 0,1, 1, "globalScaleY");

#if !USE_TIMELINE
            // get needed channels from WindowInput
            _primeE         = _windowInput.MakeChannel(primeE,      WindowInput.Buttons.L,     X);
            _scaleY         = _windowInput.MakeChannel(scaleY,      WindowInput.Buttons.L,     Y);
            _powY           = _windowInput.MakeChannel(powY,        WindowInput.Buttons.AltL,  X);
            _radius         = _windowInput.MakeChannel(radius,      WindowInput.Buttons.CtrlR, Y);
            _globalScaleY   = _windowInput.MakeChannel(globalScaleY,WindowInput.Buttons.R,     Y);
#else
            // create needed channels
            _primeE         = _timeline.MakeChannel(primeE);
            _scaleY         = _timeline.MakeChannel(scaleY);
            _powY           = _timeline.MakeChannel(powY);
            _radius         = _timeline.MakeChannel(radius);
            _globalScaleY   = _timeline.MakeChannel(globalScaleY);

            // set time from WindowInput
            var time = new Torec.ChannelInfo(0,1,0, "time");
            _time = _windowInput.MakeChannel(time, WindowInput.Buttons.L, X);

            // fill Timeline
            //_timeline.AddKeyFrame

            var times                                  = new[] { 0.0, 0.2, 0.5, 1.0 };
            _timeline.AddKeyFrames(_globalScaleY, times, new[] { 0.0, 1.0, 1.0 });
            _timeline.AddKeyFrames(_primeE,       times, new[] { 0.3,-0.8 });
            _timeline.AddKeyFrames(_scaleY,       times, new[] { 0.0,-0.2 });

#endif
        }
        #endregion Channels

        Point GetPos(Item item, double stepPhase = 1.0)
        {
            if (item.parent == null) { // root is (1)
                return new Point { x = 1, y = 0 };
            }

            int primeIndex = item.stepPrimeIndex;
            int prime = (int)Rational.Prime(primeIndex).ToInt();

            //!!! primeE also used in GetViewPos
            double primeE = Math.Pow(2, 3.0 * _primeE.GetValue()); // ..0.5.. 1 ..2..
            double primeX = 1.0 / Math.Pow(prime, primeE); // 2 ~> 0.5, 3 ~> 0.33,..

            double right = item.parent.pos.x;
            double stepX = right * (1.0 - primeX);

            stepX *= stepPhase;

            double scaleY = Math.Pow(2.0, 10.0 * _scaleY.GetValue()); // ..0.5.. 1 ..2..
            double powY   = Math.Pow(2.0, 3.0 * _powY.GetValue()); // ..0.5.. 1 ..2..
            double primeG = Math.Pow(primeIndex, powY) * scaleY;

            // normalize G by (3)
            double prime3X = 1.0 / Math.Pow(3, primeE);
            primeG /= 1.0 - prime3X;

            double x = item.parent.pos.x - stepX;
            double y = item.parent.pos.y + stepX * primeG;

            return new Point { x = x, y = y };
        }

        Torec.Drawing.Point GetViewPos(Point pos) {
            double x = pos.x;
            double y = pos.y;
#if VIEW_LOG_X
            double n = 1.0 / x; // get out of hyperbolic

            double primeE = Math.Pow(2, 3.0 * _primeE.GetValue()); // ..0.5.. 1 ..2..

            x = Math.Log(n, Math.Pow(2, primeE)); // put to logarithmic
#else
            x = 1.0 - x; // just flip it back
#endif
#if VIEW_EXP_Y
            //y = 1.0 - Math.Exp(-y); // 0..N -> 0..1
            y = 1.0 - Math.Exp(-y) * _globalScaleY.GetValue(); // 0..N -> 0..1
#endif
            return new Torec.Drawing.Point((float)x, (float)y);
        }
        float GetViewRadius(Item item) {
            double n = item.integer;
            double x = Math.Log(n, 2);
            double r = Math.Pow(1.5, -x) *
                Math.Pow(2.0, 5.0 * _radius.GetValue())
                * 0.1;
            return (float)r;
        }

        #endregion Logic


        #region IImageInput
        public bool OnSize(double newWidth, double newHeight) {
            _windowInput.SetSize(newWidth, newHeight);

            this.SetSize(
                (int)newWidth,
                (int)newHeight
            );

            return true; // request redrawing
        }
        public bool OnMouseMove(double x, double y, WindowInput.Buttons buttons) {
            _windowInput.SetMouseMove(x, y, buttons);

#if USE_TIMELINE
            double time = _time.GetValue(); // 0..1
            _timeline.SetCurrentTime(time);
#endif

            return true; // request redrawing
        }
        public Image GetImage() {
            var image = this.Draw();
            return image;
        }
        #endregion IImageInput
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