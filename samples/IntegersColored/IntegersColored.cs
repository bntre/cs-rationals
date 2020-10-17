#define VIEW_EXP_Y

// plot y=Prime[x] x=0..2000
// https://www.wolframalpha.com/input/?i=plot+y%3DPrime%5Bx%5D+x%3D0..2000

using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Diagnostics;

using Torec.UI;
using Torec.Drawing;
using Color = System.Drawing.Color;

namespace Rationals.IntegersColored
{
    struct Point {
        public double x, y;
    }

    public class Painting : InteractiveControl, IDrawer<Image>
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

        public enum ViewMode {
            Linear          = 0,
            Logarithmic     = 1,
            TimelineFlag    = 0x10000, // draw the timeline itself
        }
        ViewMode _viewMode = default(ViewMode);

        public Painting(int itemCountPow = 8, bool useTimeline = false) {
            _useTimeline = useTimeline;
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

        public Image Draw(int pixelWidth, int pixelHeight)
        {
            //!!! always recreate viewport ?
            float x0 = 0f;
            float x1 = _viewMode.HasFlag(ViewMode.Logarithmic) ? _itemCountPow : 1f;
            Viewport viewport = new Viewport(
                pixelWidth,
                pixelHeight,
                x0,x1, -1f,2f,
                yUp: true
            );

            float boundX = viewport.GetUserBounds()[1].X;

            Image image = new Image(viewport); // recreate image
            
            // fill blank
            image.Rectangle(viewport.GetUserBounds())
                .Add()
                .FillStroke(Color.White, Color.Empty);

            if (_items == null) return image;

            var groupLines   = image.Group().Add();
            var groupCircles = image.Group().Add();
            var groupNumbers = image.Group().Add();

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
                    image.Line(points)
                        .Add(groupLines)
                        .FillStroke(Color.Empty, Color.Gray, strokeWidth: radius * 0.1f);
                }

                image.Circle(viewPos, radius)
                    .Add(groupCircles)
                    .FillStroke(Color.Gray, Color.Empty);

                string text = item.integer.ToString();
                //if (item.parent != null) text = item.parent.integer.ToString() + "->" + text;
                image.Text(viewPos, text, radius, align: Image.Align.Center, centerHeight: true)
                    .Add(groupNumbers)
                    .FillStroke(Color.Black, Color.Empty);
            }

            return image;
        }

        #region Channels
        Torec.Channel _primeE;
        Torec.Channel _scaleY;
        Torec.Channel _powY;
        Torec.Channel _radius;
        Torec.Channel _globalScaleY;

        // for timeline
        bool _useTimeline = false;
        Torec.Channel _time;
        Torec.Timeline _timeline = new Torec.Timeline();

        private void InitChannels() {
            const int X = 0;
            const int Y = 1;

            var primeE          = new Torec.ChannelInfo(-1,1, 0, "primeE");
            var scaleY          = new Torec.ChannelInfo(-1,1, 0, "scaleY");
            var powY            = new Torec.ChannelInfo(-1,1, 0, "powY");
            var radius          = new Torec.ChannelInfo(-1,1, 0, "radius");
            var globalScaleY    = new Torec.ChannelInfo( 0,1, 1, "globalScaleY");

            if (!_useTimeline)
            {
                // get all needed channels from WindowInput
                _primeE         = base.MakeChannel(primeE,      MouseButtons.L,     X);
                _scaleY         = base.MakeChannel(scaleY,      MouseButtons.L,     Y);
                _powY           = base.MakeChannel(powY,        MouseButtons.AltL,  X);
                _radius         = base.MakeChannel(radius,      MouseButtons.CtrlR, Y);
                _globalScaleY   = base.MakeChannel(globalScaleY,MouseButtons.R,     Y);
            }
            else
            {
                // create needed channels in the timeline
                _primeE         = _timeline.MakeChannel(primeE);
                _scaleY         = _timeline.MakeChannel(scaleY);
                _powY           = _timeline.MakeChannel(powY);
                _radius         = _timeline.MakeChannel(radius);
                _globalScaleY   = _timeline.MakeChannel(globalScaleY);

                // set time only from WindowInput
                var time = new Torec.ChannelInfo(0,1,0, "time");
                _time = base.MakeChannel(time, MouseButtons.L, X);

                // fill the timeline
                var times =                                  new[] { 0.0, 0.2, 0.5, 1.0 };
                _timeline.AddKeyFrames(_globalScaleY, times, new[] { 0.0, 1.0, 1.0 });
                _timeline.AddKeyFrames(_primeE,       times, new[] { 0.3,-0.8 });
                _timeline.AddKeyFrames(_scaleY,       times, new[] { 0.0,-0.2 });
            }
        }

        #endregion Channels

        #region Drawing logic
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
            if (_viewMode.HasFlag(ViewMode.Logarithmic)) {
                double n = 1.0 / x; // get out of hyperbolic
                double primeE = Math.Pow(2, 3.0 * _primeE.GetValue()); // ..0.5.. 1 ..2..
                x = Math.Log(n, Math.Pow(2, primeE)); // put to logarithmic
#if VIEW_EXP_Y
                //y = 1.0 - Math.Exp(-y); // 0..N -> 0..1
                y = 1.0 - Math.Exp(-y) * _globalScaleY.GetValue(); // 0..N -> 0..1
#endif
            } else {
                // Linear mode - just flip x back
                x = 1.0 - x;
            }

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

        #endregion Drawing logic


        #region InteractiveControl
        public override bool SetMouseMove(double x01, double y01, MouseButtons buttons) {
            bool affected = base.SetMouseMove(x01, y01, buttons);
            if (affected) {
                if (_useTimeline) {
                    double time = _time.GetValue(); // 0..1
                    _timeline.SetCurrentTime(time);
                }
                OnInvalidated(); // raise Invalidated event to request redrawing
            }
            return affected;
        }
        #endregion InteractiveControl

        #region IDrawer
        public Image GetImage(int pixelWidth, int pixelHeight, int contextId) {
            _viewMode = (ViewMode)contextId;
            Image image;
            if (_useTimeline && _viewMode.HasFlag(ViewMode.TimelineFlag)) {
                // Draw the timeline itself
                Viewport viewport = new Viewport(pixelWidth, pixelHeight, 0f,1f, -2f,2f);
                image = _timeline.Draw(viewport);
            } else {
                // Draw painting
                image = this.Draw(pixelWidth, pixelHeight);
            }
            _viewMode = default(ViewMode);
            return image;
        }
        #endregion IDrawer
    }

    static class Program
    {
        static void Test1() {
            int pow = 7;

            var painting = new Painting(pow);

            int pixelWidth = pow * 200;
            int pixelHeight = 500;

            Image image = painting.Draw(pixelWidth, pixelHeight);
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