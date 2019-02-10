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
        private Rationals.Drawing.GridDrawer _gridDrawer;

        // Midi
        private Midi.Devices.IOutputDevice _midiDevice;
        private Midi.MidiPlayer _midiPlayer;

        // Tools
        private ToolsForm _toolsForm;

        public MainForm() {
            InitializeComponent();

            DoubleBuffered = true; // Don't flick (but black window edges flick on resize!!!)
            BackColor = Color.White;

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
            /*
            _toolsForm = new ToolsForm();
            _toolsForm.Show(this);
            */
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

            _viewport = new Torec.Drawing.Viewport2(
                size.Width, 
                size.Height, 
                0, shift,
                // scaleX * size.Width  / 2, 
                //-scaleY * size.Height / 2
                scale * skew * (float)Math.Sqrt(size.Width  * _initialSize.Width ) / 2,
               -scale / skew * (float)Math.Sqrt(size.Height * _initialSize.Height) / 2
            );

            Rational distanceLimit = new Rational(new[] {
                //4, -4, 1,
                8, -8, 2,
            });
            var settings = new Drawing.GridDrawer.Settings {
                harmonicityName = "Barlow",
                rationalCountLimit = 100,
                distanceLimit = distanceLimit,
                levelLimit = 3,
                //customPrimeIndices = new int[] { 2, 7 },
                //customPrimeIndices = new int[] { 0, 2 },
            };
            _gridDrawer = new Drawing.GridDrawer(
                _viewport.GetUserBounds(),
                settings,
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
            _gridDrawer.Draw12EdoGrid(image);
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
