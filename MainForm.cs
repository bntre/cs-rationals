using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;

using Torec.Drawing.Gdi;


namespace Rationals.Forms
{
    using GridDrawer = Rationals.Drawing.GridDrawer;

    public partial class MainForm : Form
    {
        //private Size _size;

        // in wheel deltas
        private int _scale = 0;
        private int _scaleSkew = 0;
        private int _scalePoint = 0;
        private int _shiftVertical = 0;

        private Torec.Drawing.Point _mousePos;
        private Size _initialSize;
        private Torec.Drawing.Viewport2 _viewport;
        private GridDrawer _gridDrawer;
        private GridDrawer.Settings _gridDrawerSettings;

        // Midi
        private Midi.Devices.IOutputDevice _midiDevice;
        private Midi.MidiPlayer _midiPlayer;

        // Tools
        private ToolsForm _toolsForm;

        public MainForm()
        {
            DoubleBuffered = true; // Don't flick (but black window edges flick on resize!!!)
            BackColor = Color.White;

            //
            _toolsForm = new ToolsForm(this);
            //
            _gridDrawerSettings = _toolsForm.GetCurrentSettings();

            /*
#if DEBUG
            var s = GridDrawer.Settings.Edo12();
#if false
            //s.customPrimeIndices = new int[] { 2, 7 },
            //s.customPrimeIndices = new int[] { 0, 2 },
#elif false
            // Bohlen-Pierce
            s.basePrimeIndex = 1;
            s.subgroupPrimeIndices = new int[] { 1, 2, 3 };
            s.up = new Rational(9, 5);
            s.upTurns = 3;
            s.edGrid = new[] { new[] { 13, 5, 2 } };
#elif true
            s.edGrids = new[] { new[] { 19, 6, 5 } }; // 19edo https://en.xen.wiki/w/19edo
#elif true
            s.edGrid = new[] { new[] { 53, 17,14 } }; // 53edo https://en.xen.wiki/w/53edo
#endif
            _gridDrawerSettings = s;
#endif
            */

            InitializeComponent(); // OnResize and Invalidate there

            _midiDevice = Midi.Devices.DeviceManager.OutputDevices.FirstOrDefault();
            _midiDevice.Open();
            _midiPlayer = new Midi.MidiPlayer(_midiDevice);
            _midiPlayer.StartClock(60 * 4);
            //_midiPlayer.SetInstrument(0, Midi.Enums.Instrument.Clarinet);
        }

        protected override void OnClosed(EventArgs e) {
            _midiPlayer.StopClock();
            _midiPlayer = null;
            _midiDevice.Close();
        }

        protected override void OnShown(EventArgs e) {
            _toolsForm.Show(this);
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);

            UpdateViewportBounds();
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            var p = new Torec.Drawing.Point(e.X, e.Y);
            _mousePos = _viewport.ToUser(p);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            var p = new Torec.Drawing.Point(e.X, e.Y);
            p = _viewport.ToUser(p);
            //Invalidate();

            Rational note = _gridDrawer.FindNearestRational(p);
            if (!note.IsDefault()) {
                _midiPlayer.NoteOn(0, (float)note.ToCents(), duration: 8f);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e) {
            base.OnMouseWheel(e);

            bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            bool alt = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;

            if (shift) {
                _scaleSkew += e.Delta;
            } else if (ctrl) {
                _scale += e.Delta;
            } else if (alt) {
                _scalePoint += e.Delta;
            } else {
                _shiftVertical += e.Delta;
            }

            UpdateViewportBounds();

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            //
            GdiImage image = DrawImage();
            //
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            image.Draw(e.Graphics);
        }

        internal void ApplyDrawerSettings(GridDrawer.Settings s) {
            _gridDrawerSettings = s;
            UpdateViewportBounds();
            Invalidate();
        }

        private void UpdateViewportBounds() {
            //_viewport = new Torec.Drawing.Viewport(_size.Width, _size.Height, -1,1, -1,1);

            float scale      = (float)Math.Exp(_scale      * 0.0005);
            float skew       = (float)Math.Exp(_scaleSkew  * 0.0005);
            float scalePoint = (float)Math.Exp(_scalePoint * 0.0005);
            float shift      = _shiftVertical * 0.0001f * 5;

            var size = this.ClientSize;

            if (_initialSize == default(Size)) {
                _initialSize = size;
            }

            // update viewport
            _viewport = new Torec.Drawing.Viewport2(
                size.Width, 
                size.Height, 
                0, shift,
                // scaleX * size.Width  / 2, 
                //-scaleY * size.Height / 2
                scale * skew * (float)Math.Sqrt(size.Width  * _initialSize.Width ) / 2,
               -scale / skew * (float)Math.Sqrt(size.Height * _initialSize.Height) / 2
            );

            // recreate drawer with new viewport bounds
            _gridDrawer = new GridDrawer(
                _viewport.GetUserBounds(),
                _gridDrawerSettings,
                pointRadiusFactor: scalePoint
            );
        }

        private GdiImage DrawImage() {
#if false
            var viewport = new Torec.Drawing.Viewport(_size.Width, _size.Height, 0,20, 0,20, false);
            var image = new GdiImage(viewport);
            Torec.Drawing.Tests.DrawTest3(image);
#else
            var image = new GdiImage(_viewport);
            //
            var highlight = _gridDrawer.FindNearestRational(_mousePos);
            _gridDrawer.DrawGrid(image, highlight);
            //_gridDrawer.Draw2DGrid(image, new[] { 12, 3,4 }, Color.Green); // additional grid -- svg id conflicts? !!!
#endif
            return image;
        }

    }

    public static class Utils {
        public static void RunForm() {
            Application.EnableVisualStyles();

            var form = new MainForm();
            Application.Run(form);
        }
    }
}
