using System;
using System.Collections.Generic;
//using System.Linq;

//!!! Move this module out of Drawing because WindowInput may be used for other purposes (sound etc)

namespace Torec.Input
{
    public class WindowInput
    {
        public enum Buttons {
            LeftButton  = 1,
            RightButton = 2,
            Alt         = 4,
            Ctrl        = 8,
            Shift       = 16,
        }

        public static class State
        {
            public delegate double GetValue(double x0, double x1, double def = 0.0);

            private class Value {
                internal bool isSet = false;
                internal double value = 0.0; // relative 0..1
                internal double Get(double r0, double r1, double def) {
                    if (!isSet) return def;
                    return r0 + (r1 - r0) * value;
                }
            }

            public class Coordinates {
                private Value _x = new Value();
                private Value _y = new Value();

                internal Coordinates() {
                    X = _x.Get;
                    Y = _y.Get;
                }
                internal void Set(double x, double y) {
                    _x.value = x; _x.isSet = true;
                    _y.value = y; _y.isSet = true;
                }

                public GetValue X;
                public GetValue Y;
                public double x { get { return _x.value; } }
                public double y { get { return _y.value; } }

                internal static readonly Coordinates Empty = new Coordinates();
            }

            private static Dictionary<Buttons, Coordinates> _buttons = new Dictionary<Buttons, Coordinates>();
            internal static void Set(Buttons b, double x, double y) {
                Coordinates c;
                bool exists = _buttons.TryGetValue(b, out c);
                if (!exists) c = new Coordinates();
                c.Set(x, y);
                if (!exists) _buttons[b] = c;
            }
            public static Coordinates Get(Buttons b) {
                return _buttons.TryGetValue(b, out Coordinates c) ? c : Coordinates.Empty;
            }

            public static Coordinates Left        { get { return Get(Buttons.LeftButton); } }
            public static Coordinates Right       { get { return Get(Buttons.RightButton); } }
            public static Coordinates AltLeft     { get { return Get(Buttons.LeftButton | Buttons.Alt); } }
            public static Coordinates AltRight    { get { return Get(Buttons.RightButton | Buttons.Alt); } }
            public static Coordinates CtrlLeft    { get { return Get(Buttons.LeftButton | Buttons.Ctrl); } }
            public static Coordinates CtrlRight   { get { return Get(Buttons.RightButton | Buttons.Ctrl); } }
            public static Coordinates ShiftLeft   { get { return Get(Buttons.LeftButton | Buttons.Shift); } }
            public static Coordinates ShiftRight  { get { return Get(Buttons.RightButton | Buttons.Shift); } }
        }

        protected double _width;
        protected double _height;

        public virtual bool OnSize(double w, double h) {
            // default logic: just remember the new size
            _width  = w;
            _height = h;
            return false; // request no action by default
        }

        public virtual bool OnMouseMove(double x, double y, Buttons buttons) {
            // default logic: change the input state
            if (_width != 0 && _height != 0) {
                State.Set(
                    buttons,
                    x / _width,
                    y / _height
                );
            }
            //
            return false; // request no action by default
        }
    }

}
