using System;
using System.Collections.Generic;
//using System.Linq;

using MathNet.Numerics.Interpolation;

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
        protected class InterpolationChannel : Torec.Channel
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
                //} else if (count <= 2) {
                //    _interpolation = LinearSpline.Interpolate(xs, ys);
                } else if (count <= 4) {
                    _interpolation = CubicSpline.InterpolateNatural(xs, ys);
                } else {
                    _interpolation = CubicSpline.InterpolateAkima(xs, ys);
                }
            }
            protected TDouble Interpolate(TTime time) {
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
            internal void AddKeyFrames(TTime[] times, TDouble[] values) {
                int count = Math.Min(times.Length, values.Length);
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
        }

        protected Dictionary<int, InterpolationChannel> _channels = new Dictionary<int, InterpolationChannel>(); // id -> channel
        protected TTime _currentTime = 0.0;

        public Channel MakeChannel(ChannelInfo info) {
            var channel = new InterpolationChannel(info, this);
            int id = channel.GetId();
            _channels[id] = channel;
            return channel;
        }

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

        public bool AddKeyFrames(Channel channel, TTime[] times, TDouble[] values) {
            int id = channel.GetId();
            InterpolationChannel c;
            if (_channels.TryGetValue(id, out c)) {
                c.AddKeyFrames(times, values);
                return true;
            }
            return false; // channel not found
        }

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
