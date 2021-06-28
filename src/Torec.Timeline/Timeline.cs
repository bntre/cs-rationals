using System;
using System.Collections.Generic;
//using System.Linq;

using MathNet.Numerics.Interpolation;

using Torec.Channels;

#if USE_DRAWING
using Torec.Drawing;
using Color = System.Drawing.Color;
#endif

using TDouble = System.Double;

namespace Torec
{
    /*
     * https://www.codeproject.com/Articles/560163/Csharp-Cubic-Spline-Interpolation
     * https://numerics.mathdotnet.com/api/MathNet.Numerics.Interpolation/CubicSpline.htm
     * http://www.mosismath.com/NaturalSplines/NaturalSplines.html
     * https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.animation.timeline?view=netcore-3.1
     */

    using TTime = TDouble;

    public class Timeline
    {
        protected class InterpolationChannel : Channel
        {
            protected Timeline _timeline;

            protected struct KeyFrame {
                public TTime time;
                public TDouble value;
            }
            protected List<KeyFrame> _keyFrames = new List<KeyFrame>(); // sorted by time

            #region Interpolation
            protected IInterpolation _interpolation = null;
            protected void UpdateInterpolation() {
                int count = _keyFrames.Count;
                var xs = new double[count];
                var ys = new double[count];
                for (int i = 0; i < count; ++i) {
                    xs[i] = (double)_keyFrames[i].time;
                    ys[i] = (double)_keyFrames[i].value;
                }
                if (count <= 1) {
                    _interpolation = StepInterpolation.Interpolate(xs, ys);
                } else if (count <= 2) {
                    _interpolation = LinearSpline.Interpolate(xs, ys);
                } else if (count <= 3) {
                    _interpolation = MathNet.Numerics.Interpolate.Polynomial(xs, ys);
                } else if (count <= 4) {
                    _interpolation = CubicSpline.InterpolateNatural(xs, ys);
                } else {
                    _interpolation = CubicSpline.InterpolateAkima(xs, ys);
                }
            }
            public TDouble Interpolate(TTime time) {
                if (_interpolation == null) return default(TDouble);
                double value = _interpolation.Interpolate((double)time);
                return value;
            }
            #endregion Interpolation

            internal InterpolationChannel(ChannelInfo info, Timeline owner)
                : base(info) {
                _timeline = owner;
            }

            internal void AddKeyFrame(TTime time, TDouble value) {
                var k = new KeyFrame { time = time, value = value };
                _keyFrames.Add(k);
                UpdateInterpolation();
            }
            internal void AddKeyFrames(TTime[] times, TDouble[] values, int count = int.MaxValue) {
                count = Math.Min(count, times.Length);
                count = Math.Min(count, values.Length);
                for (int i = 0; i < count; ++i) {
                    var k = new KeyFrame { time = times[i], value = values[i] };
                    _keyFrames.Add(k);
                }
                UpdateInterpolation();
            }

            // Channel
            public override TDouble GetValue() {
                TDouble value = Interpolate(_timeline._currentTime);
                return value;
            }

#if USE_DRAWING
            public void Draw(TTime[] timeRange, Image image, int segmentCount, Color color)
            {
                // Draw keyframes points
                for (int j = 0; j < _keyFrames.Count; ++j) {
                    TTime t = _keyFrames[j].time;
                    TDouble v = _keyFrames[j].value;
                    Point pos = new Point((float)t, (float)v);
                    image.Circle(pos, 0.02f)
                        .Add()
                        .FillStroke(color, Color.Empty);
                }

                // Draw line
                var points = new Point[segmentCount + 1];
                for (int j = 0; j <= segmentCount; ++j) {
                    TTime t = timeRange[0] + (timeRange[1] - timeRange[0]) * j / segmentCount;
                    TDouble v = this.Interpolate(t);
                    points[j] = new Point((float)t, (float)v);
                }
                image.Line(points)
                    .Add()
                    .FillStroke(Color.Empty, color, strokeWidth: 0.01f);
            }
#endif


        }

        protected Dictionary<int, InterpolationChannel> _channels = new Dictionary<int, InterpolationChannel>(); // id -> channel
        
        protected TTime _currentTime = 0.0;
        
        public Channel MakeChannel(ChannelInfo info) {
            var channel = new InterpolationChannel(info, this);
            int id = channel.GetId();
            _channels[id] = channel;
            return channel;
        }

        //protected TTime[] GetTimeRange() {
        //    return new TTime[] { 0.0, 1.0 }; //!!!
        //}

        public void SetCurrentTime(TTime time) {
            _currentTime = time;
        }

        public bool AddKeyFrame(Channel channel, TTime time, TDouble value) {
            int id = channel.GetId();
            InterpolationChannel c;
            if (_channels.TryGetValue(id, out c)) {
                c.AddKeyFrame(time, value);
                return true;
            }
            return false; // channel not found
        }

        public bool AddKeyFrames(Channel channel, TTime[] times, TDouble[] values, int count = int.MaxValue) {
            int id = channel.GetId();
            InterpolationChannel c;
            if (_channels.TryGetValue(id, out c)) {
                c.AddKeyFrames(times, values, count);
                return true;
            }
            return false; // channel not found
        }

#if USE_DRAWING
        public Image Draw(Viewport viewport)
        {
            Point[] bounds = viewport.GetUserBounds();
            TTime[] timeRange = new TTime[] { bounds[0].X, bounds[1].X };

            Image image = new Image(viewport); // recreate image

            // fill blank
            image.Rectangle(viewport.GetUserBounds())
                .Add()
                .FillStroke(Color.LightYellow, Color.Empty);
            // draw grid
            for (int i = -1; i <= 1; ++i) {
                var p0 = new Point(bounds[0].X, i);
                var p1 = new Point(bounds[1].X, i);
                image.Line(new[] { p0, p1 })
                    .Add()
                    .FillStroke(Color.Empty, Color.LightGray, strokeWidth: 0.01f);
            }
            // draw current time
            {
                var p0 = new Point((float)_currentTime, bounds[0].Y);
                var p1 = new Point((float)_currentTime, bounds[1].Y);
                image.Line(new[] { p0, p1 })
                    .Add()
                    .FillStroke(Color.Empty, Color.Gray, strokeWidth: 0.01f);
            }

            // draw channels
            int segmentCount = 50;
            foreach (var i in _channels) {
                InterpolationChannel c = i.Value;
                Color color = ColorUtils.GetRareColor(c.GetId(), 0.5, 0.5);
                c.Draw(timeRange, image, segmentCount, color);
            }

            return image;
        }
#endif
    }

#if RUN_TEST
    static class Program
    {
        static void Test1()
        {
            var x = new double[] { 1.0, 2.5, 3.3, 4.0, 5.0 };
            var y = new double[] { 10.0, 20.0, 15.0, 12.0, 8.0 };

            var spline = MathNet.Numerics.Interpolation.CubicSpline.InterpolateAkimaSorted(x, y);
            double y1 = spline.Interpolate(1.2); // 12.79
            double y2 = spline.Interpolate(4.2); // 11.17
        }


        static int Main()
        {
            Test1();
            return 0;
        }
    }
#endif
}
