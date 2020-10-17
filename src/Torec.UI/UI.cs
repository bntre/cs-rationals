using System;
using System.Collections.Generic;
//using System.Linq;

using TDouble = System.Double;

namespace Torec.UI
{
    // InteractiveControl
    //  Allows to get channels from user mouse input.
    //  Owner calls SetMouseMove.
    // !!! extend it to use with keyboard etc

    public class InteractiveControl
    {
        public enum MouseButtons {
            Left  = 1,
            Right = 2,
            Alt   = 4,
            Ctrl  = 8,
            Shift = 16,
            //
            L       =         Left,
            R       =         Right,
            AltL    = Alt   | Left,
            AltR    = Alt   | Right,
            CtrlL   = Ctrl  | Left,
            CtrlR   = Ctrl  | Right,
            ShiftL  = Shift | Left,
            ShiftR  = Shift | Right,
        }

        protected class InputChannel : Channel
        {
            TDouble _value; // value set on the fly by user unput

            public InputChannel(ChannelInfo info)
                : base(info) 
            {
                var def = _info.GetDefaultValue();
                _value = !def.Equals(ChannelInfo.NotAValue) ? def : info.GetValueRange()[0];
            }

            public void SetValue01(TDouble v01) {
                TDouble[] r = _info.GetValueRange();
                _value = r[0] + (r[1] - r[0]) * v01;
                System.Diagnostics.Debug.WriteLine("Channel {0} set value {1}", _info.GetName(), _value);
            }

            // Channel
            public override TDouble GetValue() { return _value; }
        }

        protected Dictionary<MouseButtons, InputChannel[]> _channels = new Dictionary<MouseButtons, InputChannel[]>();

        public Channel MakeChannel(ChannelInfo info, MouseButtons buttons, int coordinateIndex) {
            if (coordinateIndex < 0 || coordinateIndex > 1) throw new IndexOutOfRangeException();

            InputChannel channel = new InputChannel(info);

            InputChannel[] cs;
            if (!_channels.TryGetValue(buttons, out cs)) {
                cs = new InputChannel[] { null, null };
                _channels[buttons] = cs;
            }

            cs[coordinateIndex] = channel;

            return channel;
        }

        public virtual bool SetMouseMove(TDouble x01, TDouble y01, MouseButtons buttons) {
            // default logic: save coordinate value
            InputChannel[] cs;
            if (_channels.TryGetValue(buttons, out cs)) {
                if (cs[0] != null) cs[0].SetValue01(x01);
                if (cs[1] != null) cs[1].SetValue01(y01);
                return true; // true if there was a channel made
            }
            return false;
        }

        #region Invalidated
        public event Action Invalidated;
        protected void OnInvalidated() {
            Invalidated?.Invoke();
        }
        #endregion Invalidated
    }

    public interface IDrawer<TImage> {
        TImage GetImage(int pixelWidth, int pixelHeight, int contextId);
    }

    // WindowInfo
    //  and
    // ControlInfo
    public class WindowInfo<TWindow, TImage>
        where TWindow : class
        where TImage : class
    {
        public struct ControlInfo {
            public InteractiveControl logic;
            public IDrawer<TImage> drawer;
            public string nativeName;
            public int contextId;
        } 

        public string title = null;
        public TWindow window = null;
        public List<ControlInfo> controls = new List<ControlInfo>();

        public void AddControl(InteractiveControl logic, IDrawer<TImage> drawer, string nativeName = null, int contextId = 0) {
            controls.Add(new ControlInfo {
                logic      = logic,
                drawer     = drawer,
                nativeName = nativeName,
                contextId  = contextId
            });
        }
    }

}
