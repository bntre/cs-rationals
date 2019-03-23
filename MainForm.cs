#define USE_PERF

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml;

using Torec.Drawing.Gdi;


namespace Rationals.Forms
{
    using GridDrawer = Rationals.Drawing.GridDrawer;

    public partial class MainForm : Form
    {
        private Torec.Drawing.Point _mousePos;
        private Size _initialSize;
        private Torec.Drawing.Viewport2 _viewport;

        private GridDrawer _gridDrawer2;
        private GridDrawer.Settings _gridDrawerSettings;
        private ViewportSettings _viewportSettings;

        // Midi
        private Midi.Devices.IOutputDevice _midiDevice;
        private Midi.MidiPlayer _midiPlayer;

        // Tools
        private ToolsForm _toolsForm;

#if USE_PERF
        private Rationals.Utils.PerfCounter _perfCollectItems = new Rationals.Utils.PerfCounter("Collect items");
        private Rationals.Utils.PerfCounter _perfBuildImage   = new Rationals.Utils.PerfCounter("Build image");
        private Rationals.Utils.PerfCounter _perfRenderImage  = new Rationals.Utils.PerfCounter("Render image");
#endif

        internal struct ViewportSettings {
            // all values in mouse wheel deltas
            public int scale;
            public int scaleSkew;
            public int scalePoint;
            public int shiftVertical;
        }

        public MainForm()
        {
            DoubleBuffered = true; // Don't flick (but black window edges are seen on resize!!!)
            BackColor = Color.White;

            _gridDrawer2 = new GridDrawer();

            // Load previous or default settings (ApplyDrawerSettings() called from there)
            _toolsForm = new ToolsForm(this, _gridDrawer2);

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
            //
            _toolsForm.SaveAppSettings();
            //
#if USE_PERF
            Debug.WriteLine(_perfCollectItems.GetReport());
            Debug.WriteLine(_perfBuildImage  .GetReport());
            Debug.WriteLine(_perfRenderImage .GetReport());
#endif
        }

        protected override void OnShown(EventArgs e) {
            _toolsForm.Show(this);
        }

        protected override void OnResize(EventArgs e) {
            base.OnResize(e);
            //
            UpdateViewportBounds();
            Invalidate();
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            if (e.Button == MouseButtons.Right) return; // allow e.g. to move mouse out and copy current selection info
            var p = new Torec.Drawing.Point(e.X, e.Y);
            _mousePos = _viewport.ToUser(p);
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            var p = new Torec.Drawing.Point(e.X, e.Y);
            p = _viewport.ToUser(p);
            //Invalidate();

            Rational r = _gridDrawer2.FindNearestRational(p);
            if (!r.IsDefault()) {
                _midiPlayer.NoteOn(0, (float)r.ToCents(), duration: 8f);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e) {
            base.OnMouseWheel(e);

            bool shift = (Control.ModifierKeys & Keys.Shift) == Keys.Shift;
            bool ctrl = (Control.ModifierKeys & Keys.Control) == Keys.Control;
            bool alt = (Control.ModifierKeys & Keys.Alt) == Keys.Alt;

            if (shift) {
                _viewportSettings.scaleSkew += e.Delta;
                UpdateViewportBounds();
            } else if (ctrl) {
                _viewportSettings.scale += e.Delta;
                UpdateViewportBounds();
            } else if (alt) {
                _viewportSettings.scalePoint += e.Delta;
                UpdatePointScale();
            } else {
                _viewportSettings.shiftVertical += e.Delta;
                UpdateViewportBounds();
            }

            _toolsForm.MarkPresetChanged();

            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e) {
            base.OnPaint(e);

            //
#if USE_PERF
            _perfBuildImage.Start();
#endif
            GdiImage image = DrawImage();
#if USE_PERF
            _perfBuildImage.Stop();
#endif
            //
#if USE_PERF
            _perfRenderImage.Start();
#endif
            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            image.Draw(e.Graphics);
#if USE_PERF
            _perfRenderImage.Stop();
#endif
        }

        internal void ApplyDrawerSettings(GridDrawer.Settings s) {
            _gridDrawerSettings = s;
            UpdateBase();
            UpdateSlope();
            Invalidate();
        }

        private void UpdateBase() {
            var s = _gridDrawerSettings;
            _gridDrawer2.SetBase(s.limitPrimeIndex, s.subgroup, s.harmonicityName);
            _gridDrawer2.SetCommas(s.stickCommas);
            _gridDrawer2.SetStickMeasure(s.stickMeasure);
            _gridDrawer2.SetGeneratorLimits(s.rationalCountLimit, s.distanceLimit);
            _gridDrawer2.SetEDGrids(s.edGrids);
        }

        private void UpdateSlope() {
            var s = _gridDrawerSettings;
            _gridDrawer2.SetSlope(s.slopeOrigin, s.slopeChainTurns);
        }

        private void UpdateViewportBounds()
        {
            float scale = (float)Math.Exp(_viewportSettings.scale      * 0.0005);
            float skew  = (float)Math.Exp(_viewportSettings.scaleSkew  * 0.0005);
            float shift = _viewportSettings.shiftVertical * 0.0001f * 5;

            Size size = this.ClientSize;

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

            _gridDrawer2.SetBounds(_viewport.GetUserBounds());
        }

        private void UpdatePointScale() {
            float scalePoint = (float)Math.Exp(_viewportSettings.scalePoint * 0.0005);
            _gridDrawer2.SetPointRadiusFactor(scalePoint);
        }

        private GdiImage DrawImage() {
#if false
            var viewport = new Torec.Drawing.Viewport(_size.Width, _size.Height, 0,20, 0,20, false);
            var image = new GdiImage(viewport);
            Torec.Drawing.Tests.DrawTest3(image);
#else
            _gridDrawer2.UpdateItems();

            Rational highlight = _gridDrawer2.FindNearestRational(_mousePos);

            var image = new GdiImage(_viewport);

            _gridDrawer2.DrawGrid(image, highlight);

            string highlightInfo = _gridDrawer2.FormatRationalInfo(highlight);
            _toolsForm.ShowInfo(highlightInfo);
#endif
            return image;
        }

        #region Preset
        //!!! these settings might be 
        public void SavePresetViewport(XmlWriter w) {
            w.WriteElementString("scale",           _viewportSettings.scale.ToString());
            w.WriteElementString("scaleSkew",       _viewportSettings.scaleSkew.ToString());
            w.WriteElementString("scalePoint",      _viewportSettings.scalePoint.ToString());
            w.WriteElementString("shiftVertical",   _viewportSettings.shiftVertical.ToString());
        }
        public void LoadPresetViewport(XmlReader r) {
            while (r.Read()) {
                if (r.NodeType == XmlNodeType.Element) {
                    switch (r.Name) {
                        case "scale":           _viewportSettings.scale          = r.ReadElementContentAsInt(); break;
                        case "scaleSkew":       _viewportSettings.scaleSkew      = r.ReadElementContentAsInt(); break;
                        case "scalePoint":      _viewportSettings.scalePoint     = r.ReadElementContentAsInt(); break;
                        case "shiftVertical":   _viewportSettings.shiftVertical  = r.ReadElementContentAsInt(); break;
                    }
                }
            }
            UpdatePointScale();
            UpdateViewportBounds();
            Invalidate();
        }
        public void ResetViewport() {
            _viewportSettings = new ViewportSettings();
            UpdatePointScale();
            UpdateViewportBounds();
            Invalidate();
        }
        #endregion
    }

    public static class Utils {
        public static void RunForm() {
            Application.EnableVisualStyles();

            var form = new MainForm();
            Application.Run(form);
        }
    }
}
