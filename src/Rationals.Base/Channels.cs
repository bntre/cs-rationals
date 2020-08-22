using System;
using System.Collections.Generic;
//using System.Linq;

using TDouble = System.Double;

namespace Torec
{
    public class ChannelInfo {
        public const TDouble NotAValue = TDouble.NaN;

        public ChannelInfo(TDouble range0, TDouble range1, TDouble defaultValue = NotAValue, string name = null) {
            _valueRange = new TDouble[] { range0, range1 };
            _defaultValue = defaultValue;
            _name = name;
        }

        public TDouble[] GetValueRange() { return _valueRange; }
        public TDouble GetDefaultValue() { return _defaultValue; }
        public string GetName() { return _name ?? "nonamed"; }

        protected TDouble[] _valueRange;
        protected TDouble _defaultValue = NotAValue;
        protected string _name = null; //!!! for debug only ?
    }

    public abstract class Channel
    {
        protected ChannelInfo _info;
        protected int _id;

        protected static int _idCounter = 0;

        public Channel(ChannelInfo info) {
            _info = info;
            _id = _idCounter ++;
        }

        public ChannelInfo GetInfo() { return _info; }
        public int GetId() { return _id; }
        public abstract TDouble GetValue();
    }
}


namespace Torec.Input
{
    public class WindowInput
    {
        public enum Buttons {
            LeftMouseButton  = 1,
            RightMouseButton = 2,
            Alt              = 4,
            Ctrl             = 8,
            Shift            = 16,
            //
            L       =         LeftMouseButton,
            R       =         RightMouseButton,
            AltL    = Alt   | LeftMouseButton,
            AltR    = Alt   | RightMouseButton,
            CtrlL   = Ctrl  | LeftMouseButton,
            CtrlR   = Ctrl  | RightMouseButton,
            ShiftL  = Shift | LeftMouseButton,
            ShiftR  = Shift | RightMouseButton,
        }

        public class InputChannel : Channel
        {
            TDouble _value; // value set on the fly by user unput

            public InputChannel(ChannelInfo info)
                : base(info) 
            {
                _value = _info.GetDefaultValue();
                if (_value.Equals(ChannelInfo.NotAValue)) {
                    _value = info.GetValueRange()[0];
                }
            }

            internal void SetValue01(TDouble v01) {
                TDouble[] r = _info.GetValueRange();
                _value = r[0] + (r[1] - r[0]) * v01;
                System.Diagnostics.Debug.WriteLine("Channel {0} set value {1}", _info.GetName(), _value);
            }

            // Channel
            public override TDouble GetValue() { return _value; }
        }

        protected Dictionary<Buttons, InputChannel[]> _coordinates = new Dictionary<Buttons, InputChannel[]>();

        public Channel MakeChannel(ChannelInfo info, Buttons buttons, int coordinateIndex) {
            if (coordinateIndex < 0 || coordinateIndex > 1) throw new IndexOutOfRangeException();

            InputChannel channel = new InputChannel(info);

            InputChannel[] cs;
            if (!_coordinates.TryGetValue(buttons, out cs)) {
                cs = new InputChannel[] { null, null };
                _coordinates[buttons] = cs;
            }

            cs[coordinateIndex] = channel;

            return channel;
        }

        protected double _width;
        protected double _height;

        public void SetSize(TDouble w, TDouble h) {
            // default logic: just remember the new size
            _width  = w;
            _height = h;
        }

        public void SetMouseMove(TDouble x, TDouble y, Buttons buttons) {
            // default logic: save coordinate value
            if (_width != 0 && _height != 0) {
                InputChannel[] cs;
                if (_coordinates.TryGetValue(buttons, out cs)) {
                    if (cs[0] != null) cs[0].SetValue01(x / _width);
                    if (cs[1] != null) cs[1].SetValue01(y / _height);
                }
            }
        }
    }
}
