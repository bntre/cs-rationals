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
    using Point = Torec.Drawing.Point;

    public class FormViewport : Torec.Drawing.Viewport {
        public FormViewport(float sizeX, float sizeY, float x0, float x1, float y0, float y1) :
            base(sizeX, sizeY, x0, x1, y0, y1, false) {
        }

        public void ResizeKeepingScale(float sizeX, float sizeY) {
            Point c = (_bounds[1] - _bounds[0]) / 2;
            float fx = sizeX / _imageSize.X;
            float fy = sizeY / _imageSize.Y;
            //_bounds[0].X = 
            float x0 = c.X + (_bounds[0].X - c.X) * fx;
            float x1 = c.X + (_bounds[1].X - c.X) * fx;
            float y0 = c.Y + (_bounds[0].Y - c.Y) * fy;
            float y1 = c.Y + (_bounds[1].Y - c.Y) * fy;
        }
    }
}


namespace Rationals.Forms
{
    public partial class MainForm : Form
    {
        private Size _size;

        private Torec.Drawing.Viewport _viewport;
        private Rationals.Drawing.GridDrawer _gridDrawer;
        private Torec.Drawing.Point _mousePos;

        private Midi.Devices.IOutputDevice _midiDevice;
        private Midi.MidiPlayer _midiPlayer;


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

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);

            _size = ClientSize;
            _viewport = new Torec.Drawing.Viewport(_size.Width, _size.Height, -1,1, -1,1, true);

            var harmonicity = new BarlowHarmonicity();
            var distanceLimit = new Rational(new[] { 4, -4, 1 });
            _gridDrawer = new Rationals.Drawing.GridDrawer(harmonicity, _viewport.GetUserBounds(), levelLimit: 3, distanceLimit: distanceLimit);

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
            _midiPlayer.NoteOn(0, (float)note.ToCents(), duration: 8f);
        }

        protected override void OnMouseWheel(MouseEventArgs e) {
            base.OnMouseWheel(e);

            bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            bool alt = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;

            if (shift) {

            } else if (ctrl) {
                //_viewport.
            } else if (alt) {
            } else {
            }



            //e.Delta
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);
            //
            GdiImage image = DrawImage();
            //
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            image.Draw(e.Graphics);
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
