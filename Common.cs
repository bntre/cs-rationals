using System;
using System.Collections.Generic;

namespace Rationals
{
    public interface IHandler<T> {
        int Handle(T input); // 1 - accepted, 0 - rejected, -1 - stop
    }

    public class HandlerPipe<T> : IHandler<T> {
        private IHandler<T>[] _handlers;
        public HandlerPipe(params IHandler<T>[] handlers) {
            _handlers = handlers;
        }
        public int Handle(T item) {
            for (int i = 0; i < _handlers.Length; ++i) {
                int result = _handlers[i].Handle(item);
                if (result < 1) return result;
            }
            return 1;
        }
    }

    public interface IIterator<T> {
        void Iterate(IHandler<T> handler);
    }

    public class Collector<T> : IHandler<T>, IIterator<T> {
        private List<T> _items = new List<T>();
        // First collect them all
        public int Handle(T item) {
            _items.Add(item);
            return 1;
        }
        // Then sort/iterate
        public void Sort(Comparison<T> comparison) {
            _items.Sort(comparison);
        }
        public void Iterate(IHandler<T> handler) {
            int len = _items.Count;
            for (int i = 0; i < len; ++i) {
                handler.Handle(_items[i]);
            }
        }
        public void Iterate(Comparison<T> comparison, IHandler<T> handler) {
            Sort(comparison);
            Iterate(handler);
        }
        //
        public List<T> GetList() { return _items; }
        public T[] GetArray() { return _items.ToArray(); }
    }
}

namespace Rationals
{
    public static partial class Utils
    {
        // from http://www.java2s.com/Code/CSharp/2D-Graphics/HsvToRgb.htm
        public static System.Drawing.Color HsvToRgb(double h, double s, double v)
        {
            int hi = (int)Math.Floor(h / 60.0) % 6;
            double f = (h / 60.0) - Math.Floor(h / 60.0);

            double p = v * (1.0 - s);
            double q = v * (1.0 - (f * s));
            double t = v * (1.0 - ((1.0 - f) * s));

            switch (hi) {
                case 0:
                    return FromRgb(v, t, p);
                case 1:
                    return FromRgb(q, v, p);
                case 2:
                    return FromRgb(p, v, t);
                case 3:
                    return FromRgb(p, q, v);
                case 4:
                    return FromRgb(t, p, v);
                case 5:
                    return FromRgb(v, p, q);
                default:
                    return FromRgb(0, 0, 0);
            }
        }
        private static System.Drawing.Color FromRgb(double r, double g, double b) {
            return System.Drawing.Color.FromArgb(255, (byte)(r * 255.0), (byte)(g * 255.0), (byte)(b * 255.0));
        }

        // from http://csharphelper.com/blog/2016/08/convert-between-rgb-and-hls-color-models-in-c/
        public static System.Drawing.Color HslToRgb(double h, double s, double l) {
            if (s == 0) return FromRgb(l, l, l);
            //
            double p2 = l <= 0.5 ? l * (1 + s) : s + l * (1 - s);
            double p1 = 2 * l - p2;
            //
            double r = QqhToRgb(p1, p2, h + 120);
            double g = QqhToRgb(p1, p2, h);
            double b = QqhToRgb(p1, p2, h - 120);
            //
            return FromRgb(r, g, b);
        }
        private static double QqhToRgb(double q1, double q2, double h) {
            if (h > 360) h -= 360;
            else if (h < 0) h += 360;
            //
            if (h <  60) return q1 + (q2 - q1) * h / 60;
            if (h < 180) return q2;
            if (h < 240) return q1 + (q2 - q1) * (240 - h) / 60;
            return q1;
        }
        
    }

}
