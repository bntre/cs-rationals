using System;
using System.Collections.Generic;
//using System.Linq;

using TDouble = System.Double;


namespace Torec.UI
{
    // IInteractiveControl
    //  Allows to get channels from user mouse input.
    //  Owner calls SetMouseMove.

    // !!! extend it to use with keyboard etc? or IInteractiveControl is for "channels" only

    public enum MouseButtons {
        None  = 0,
        Left  = 1,
        Right = 2,
    }

    public enum KeyModifiers {
        None  = 0,
        Alt   = 1 << 16,
        Ctrl  = 2 << 16,
        Shift = 4 << 16,
    }

    /*
    public enum MouseButtons {
        None  = 0,
        Left  = 1,
        Right = 2,
        Alt   = 4,
        Ctrl  = 8,
        Shift = 16,
        //
        L      =         Left,
        R      =         Right,
        AltL   = Alt   | Left,
        AltR   = Alt   | Right,
        CtrlL  = Ctrl  | Left,
        CtrlR  = Ctrl  | Right,
        ShiftL = Shift | Left,
        ShiftR = Shift | Right,
    }
    */

    public interface IInteractiveControl
    {
        void OnMouseMove(TDouble x01, TDouble y01, MouseButtons mouse, KeyModifiers mods); // give user input to the logic control

        void OnKeyDown(int keyCode, KeyModifiers mods);

        void DoIdle();
    }

    public interface IDrawer<TImage>
    {
        event Action UpdateImage; // raised on time to update image, e.g. redrawing a changed logic

        TImage GetImage(int pixelWidth, int pixelHeight, int contextId);
    }
}


namespace Torec.UI
{
    using Torec.Channels;

    public class InteractiveControl : IInteractiveControl
    {
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
                //System.Diagnostics.Debug.WriteLine("Channel {0} set value {1}", _info.GetName(), _value);
            }

            // Channel
            public override TDouble GetValue() { return _value; }
        }

        protected Dictionary<int, InputChannel[]> _channels = new Dictionary<int, InputChannel[]>(); // code -> [channel x, channel y]

        protected int GetChannelCode(MouseButtons mouse, KeyModifiers mods) { 
            return (int)mouse | (int)mods;
        }

        public Channel MakeChannel(ChannelInfo info, MouseButtons mouse, KeyModifiers mods, int coordinateIndex) {
            if (coordinateIndex < 0 || coordinateIndex > 1) throw new IndexOutOfRangeException();

            int code = GetChannelCode(mouse, mods);

            InputChannel[] cs;
            if (!_channels.TryGetValue(code, out cs)) {
                cs = new InputChannel[] { null, null };
                _channels[code] = cs;
            }

            InputChannel channel = new InputChannel(info);

            cs[coordinateIndex] = channel;

            return channel;
        }

        #region IInteractiveControl
        public virtual void OnMouseMove(TDouble x01, TDouble y01, MouseButtons mouse, KeyModifiers mods) {
            // default logic: save coordinate value

            int code = GetChannelCode(mouse, mods);

            InputChannel[] cs;
            if (_channels.TryGetValue(code, out cs)) {
                var ids = new List<int>();
                if (cs[0] != null) { cs[0].SetValue01(x01); ids.Add(cs[0].GetId()); }
                if (cs[1] != null) { cs[1].SetValue01(y01); ids.Add(cs[1].GetId()); }
                //!!! here might be started some recursive cascade of channel affecting

                // our channels affected
                ChannelsChanged?.Invoke(ids.ToArray());
            }
        }
        public virtual void OnKeyDown(int keyCode, KeyModifiers mods) {
            //KeyDown?.Invoke(keyCode, mods);
        }
        public virtual void DoIdle() {}
        #endregion IInteractiveControl

        #region Events
        public delegate void ChannelsChangeHandler(int[] channelIds);
        public event ChannelsChangeHandler ChannelsChanged;

        //public delegate void KeyDownHandler(int keyCode, KeyModifiers mods);
        //public event KeyDownHandler KeyDown;
        #endregion Events

    }
}


namespace Torec.UI
{
    // WindowInfo
    //  and
    // ControlInfo
    public class WindowInfo<TWindow, TImage>
        where TWindow : class
        where TImage : class
    {
        public struct ControlInfo {
            public IInteractiveControl logic;
            public IDrawer<TImage> drawer;
            public string nativeName;
            public int contextId;
        } 

        public string title = null;
        public TWindow window = null;
        public List<ControlInfo> controls = new List<ControlInfo>();

        public void AddControl(IInteractiveControl logic, IDrawer<TImage> drawer, string nativeName = null, int contextId = 0) {
            controls.Add(new ControlInfo {
                logic      = logic,
                drawer     = drawer,
                nativeName = nativeName,
                contextId  = contextId
            });
        }
    }

}
