using System;
using System.Collections.Generic;


namespace Rationals
{
    public static partial class Utils
    {
        #region Math
        public static double Interp(double f0, double f1, float k) {
            return f0 + (f1 - f0) * k;
        }

        public static int Div(int n, int m) {
            // n          -6 -5 -4 -3 -2 -1  0  1  2  3  4  5  6
            // Div(n, 4)  -2 -2 -1 -1 -1 -1  0  0  0  0  1  1  1
            // n / 4      -1 -1 -1  0  0  0  0  0  0  0  1  1  1
            if (m < 0) return Div(-n, -m);
            return n >= 0 ? (n / m) : ((n + 1) / m - 1);
        }
        public static int Mod(int n, int m) {
            // n          -6 -5 -4 -3 -2 -1  0  1  2  3  4  5  6
            // Mod(n, 4)   2  3  0  1  2  3  0  1  2  3  0  1  2
            // n % 4      -2 -1  0 -3 -2 -1  0  1  2  3  0  1  2
            if (m < 0) return Mod(-n, -m);
            return n >= 0 ? (n % m) : ((n + 1) % m + m - 1);
        }
        #endregion


        #region PerfCounter
        public class PerfCounter {
            private string _name;
            private System.Diagnostics.Stopwatch _sw = new System.Diagnostics.Stopwatch();
            private int _count = 0;
            private long _totalTicks = 0;
            //
            public PerfCounter(string name) {
                _name = name;
            }
            public void Start() {
                if (_sw.IsRunning) throw new Exception("Already started");
                _sw.Restart();
            }
            public void Stop() {
                if (!_sw.IsRunning) throw new Exception("Not started");
                _sw.Stop();
                _totalTicks += _sw.ElapsedTicks;
                _count += 1;
            }
            public string GetReport() {
                return String.Format(
                    "PerfCounter {0,-12}: {1} ticks / count {2} = {3}",
                    _name, _totalTicks, _count, _totalTicks / (_count == 0 ? 1 : _count)
                );
            }
        }
        #endregion
    }

    public interface IHandler<T> {
        //!!! single method interface - replace with delegate
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

    /*
    public interface IIterator<T> {
        void Iterate(IHandler<T> handler);
    }
    */

    public class Collector<T> : IHandler<T> /*, IIterator<T>*/ {
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
