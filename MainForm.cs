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
    using Viewport   = Torec.Drawing.Viewport2;
    using TPoint     = Torec.Drawing.Point;

    public partial class MainForm : Form
    {
        private Size _initialSize;
        private Viewport _viewport;
        //private TPoint _mouseUserPoint;
        private Point _mousePoint;
        private Point _mousePointDrag;
        private double _cursorCents;

        private GridDrawer _gridDrawer;
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

        // temporal viewport values
        internal struct ViewportSettings {
            // set from loading preset
            public float scaleX;
            public float scaleY;
            public float originX;
            public float originY;
            // set from mouse handlers; all values in mouse wheel deltas
            public int scaleDX;
            public int scaleDY;
            public int scalePoint;
            public int originDX;
            public int originDY;
        }

        public MainForm()
        {
            DoubleBuffered = true; // Don't flick (but black window edges are seen on resize!!!)
            BackColor = Color.White;

            _gridDrawer = new GridDrawer();

            // Load previous or default settings (ApplyDrawerSettings() called from there)
            _toolsForm = new ToolsForm(this, _gridDrawer);

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
            UpdateViewportBounds(ViewportUpdateFlags.Size);
            Invalidate();
        }

        #region Mouse handlers

        protected override void OnMouseMove(MouseEventArgs e) {
            _mousePoint = new Point(e.X, e.Y);
            if (e.Button.HasFlag(MouseButtons.Middle)) {
                _viewportSettings.originDX += _mousePointDrag.X - _mousePoint.X;
                _viewportSettings.originDY += _mousePointDrag.Y - _mousePoint.Y;
                _mousePointDrag = _mousePoint;
                UpdateViewportBounds(ViewportUpdateFlags.Origin);
                Invalidate();
            } else {
                TPoint u = _viewport.ToUser(new TPoint(_mousePoint.X, _mousePoint.Y));
                _cursorCents = _gridDrawer.GetCursorCents(u.X, u.Y);
                Invalidate();
            }
        }

        protected override void OnMouseDown(MouseEventArgs e) {
            if (_mousePoint != new Point(e.X, e.Y)) throw new Exception("unexpected"); //!!! temp
            if (e.Button.HasFlag(MouseButtons.Left)) {
                double cents = 0;
                bool play = false;
                if (ModifierKeys == Keys.Control) {
                    cents = _cursorCents;
                    play = true;
                } else if (ModifierKeys == 0) {
                    Rational r = _gridDrawer.FindNearestRational(_cursorCents);
                    if (!r.IsDefault()) {
                        cents = r.ToCents();
                        play = true;
                    }

                }
                if (play) {
                    _midiPlayer.NoteOn(0, (float)cents, duration: 8f);
                }
            }
            if (e.Button.HasFlag(MouseButtons.Middle)) {
                _mousePointDrag = _mousePoint;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e) {
            if (e.Button.HasFlag(MouseButtons.Middle)) {
                _mousePointDrag = default(Point);
            }
        }

        protected override void OnMouseWheel(MouseEventArgs e) {
            base.OnMouseWheel(e);

            bool shift = ModifierKeys.HasFlag(Keys.Shift);
            bool ctrl  = ModifierKeys.HasFlag(Keys.Control);
            bool alt   = ModifierKeys.HasFlag(Keys.Alt);

            if (shift) {
                _viewportSettings.scaleDX += e.Delta;
                _viewportSettings.scaleDY -= e.Delta;
                UpdateViewportBounds(ViewportUpdateFlags.Scale);
            } else if (ctrl) {
                _viewportSettings.scaleDX += e.Delta;
                _viewportSettings.scaleDY += e.Delta;
                UpdateViewportBounds(ViewportUpdateFlags.Scale);
            } else if (alt) {
                _viewportSettings.scalePoint += e.Delta;
                UpdatePointScale();
            } else {
                _viewportSettings.originDY += e.Delta / 10;
                UpdateViewportBounds(ViewportUpdateFlags.Origin);
            }

            _toolsForm.MarkPresetChanged();

            Invalidate();
        }
        #endregion

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
            _gridDrawer.SetBase(s.limitPrimeIndex, s.subgroup, s.harmonicityName);
            _gridDrawer.SetCommas(s.stickCommas);
            _gridDrawer.SetStickMeasure(s.stickMeasure);
            _gridDrawer.SetGeneratorLimits(s.rationalCountLimit, s.distanceLimit);
            _gridDrawer.SetEDGrids(s.edGrids);
        }

        private void UpdateSlope() {
            var s = _gridDrawerSettings;
            _gridDrawer.SetSlope(s.slopeOrigin, s.slopeChainTurns);
        }

        private enum ViewportUpdateFlags {
            None   = 0,
            Size   = 1,
            Scale  = 2,
            Origin = 4,
            All    = 7,
        }

        private void UpdateViewportBounds(ViewportUpdateFlags flags, bool reset = false)
        {
            Size size = this.ClientSize;

            if (_viewport == null) {
                _viewport = new Viewport();
                _initialSize = size; // save it for "scaling" resize
            }

            // Update viewport
            if (flags.HasFlag(ViewportUpdateFlags.Size)) {
                _viewport.SetImageSize(size.Width, size.Height);
                // additional scale for "scaling resize"
                float sx = (float)Math.Sqrt(size.Width  * _initialSize.Width ) / 2;
                float sy = (float)Math.Sqrt(size.Height * _initialSize.Height) / 2;
                _viewport.SetAdditionalScale(sx, -sy); // flip here
            }
            if (flags.HasFlag(ViewportUpdateFlags.Scale)) {
                if (reset) {
                    _viewport.SetScale(_viewportSettings.scaleX, _viewportSettings.scaleY);
                    _viewportSettings.scaleX = 0;
                    _viewportSettings.scaleY = 0;
                }
                if (_viewportSettings.scaleDX != 0 || _viewportSettings.scaleDY != 0) {
                    float dx = (float)Math.Exp(_viewportSettings.scaleDX * 0.0005);
                    float dy = (float)Math.Exp(_viewportSettings.scaleDY * 0.0005);
                    _viewport.SetScaleDelta(dx, dy, _mousePoint.X, _mousePoint.Y);
                    _viewportSettings.scaleDX = 0;
                    _viewportSettings.scaleDY = 0;
                }
            }
            if (flags.HasFlag(ViewportUpdateFlags.Origin)) {
                if (reset) {
                    _viewport.SetCenter(_viewportSettings.originX, _viewportSettings.originY);
                }
                if (_viewportSettings.originDX != 0 || _viewportSettings.originDY != 0) {
                    float dx = _viewportSettings.originDX;
                    float dy = _viewportSettings.originDY;
                    _viewport.SetCenterDelta(dx, -dy); // flip here
                    _viewportSettings.originDX = 0;
                    _viewportSettings.originDY = 0;
                }
            }

            // Bounds affected - update drawer bounds
            _gridDrawer.SetBounds(_viewport.GetUserBounds());
        }

        private void UpdatePointScale() {
            float scalePoint = (float)Math.Exp(_viewportSettings.scalePoint * 0.0005);
            _gridDrawer.SetPointRadiusFactor(scalePoint);
        }

        private GdiImage DrawImage() {
#if false
            var viewport = new Torec.Drawing.Viewport(_size.Width, _size.Height, 0,20, 0,20, false);
            var image = new GdiImage(viewport);
            Torec.Drawing.Tests.DrawTest3(image);
#else
            _gridDrawer.UpdateItems();

            Rational highlight = default(Rational);
            if (ModifierKeys == 0) {
                highlight = _gridDrawer.FindNearestRational(_cursorCents);
            }

            var image = new GdiImage(_viewport);

            _gridDrawer.DrawGrid(image, highlight, _cursorCents);

            string highlightInfo = _gridDrawer.FormatRationalInfo(highlight, _cursorCents);
            _toolsForm.ShowInfo(highlightInfo);
#endif
            return image;
        }

        #region Preset
        //!!! these settings might be 
        public void SavePresetViewport(XmlWriter w) {
            TPoint scale  = _viewport.GetScale();
            TPoint origin = _viewport.GetCenter();
            w.WriteElementString("scaleX",  scale .X.ToString());
            w.WriteElementString("scaleY",  scale .Y.ToString());
            w.WriteElementString("originX", origin.X.ToString());
            w.WriteElementString("originY", origin.Y.ToString());
            w.WriteElementString("scalePoint", _viewportSettings.scalePoint.ToString());
        }
        public void LoadPresetViewport(XmlReader r) {
            _viewportSettings = new ViewportSettings();
            _viewportSettings.scaleX = 1f;
            _viewportSettings.scaleY = 1f;
            while (r.Read()) {
                if (r.NodeType == XmlNodeType.Element) {
                    switch (r.Name) {
                        case "scaleX":      _viewportSettings.scaleX     = r.ReadElementContentAsFloat(); break;
                        case "scaleY":      _viewportSettings.scaleY     = r.ReadElementContentAsFloat(); break;
                        case "originX":     _viewportSettings.originX    = r.ReadElementContentAsFloat(); break;
                        case "originY":     _viewportSettings.originY    = r.ReadElementContentAsFloat(); break;
                        case "scalePoint":  _viewportSettings.scalePoint = r.ReadElementContentAsInt();   break;
                    }
                }
            }
            UpdatePointScale();
            UpdateViewportBounds(ViewportUpdateFlags.All, true);
            Invalidate();
        }
        public void ResetViewport() {
            _viewportSettings = new ViewportSettings();
            _viewportSettings.scaleX = 1f;
            _viewportSettings.scaleY = 1f;
            UpdatePointScale();
            UpdateViewportBounds(ViewportUpdateFlags.All, true);
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
