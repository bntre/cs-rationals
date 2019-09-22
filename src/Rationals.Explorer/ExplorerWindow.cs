using System;
using System.Collections.ObjectModel;

using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using TD = Torec.Drawing;
using Rationals.Drawing;


namespace Rationals.Explorer
{
    class BitmapAdapter { //!!! IDisposable?
        public Avalonia.Media.Imaging.WriteableBitmap AvaloniaBitmap = null;
        public System.Drawing.Bitmap SystemBitmap = null;
        public PixelSize Size = PixelSize.Empty;

        public bool IsEmpty() {
            return Size.Width == 0 || Size.Height == 0;
        }

        public void Resize(PixelSize size) {
            if (size == Size) return;
            Size = size;

            if (AvaloniaBitmap != null) {
                AvaloniaBitmap.Dispose();
                AvaloniaBitmap = null;
            }
            if (SystemBitmap != null) {
                SystemBitmap.Dispose();
                SystemBitmap = null;
            }

            if (IsEmpty()) return;

            AvaloniaBitmap = new Avalonia.Media.Imaging.WriteableBitmap(
                Size,
                new Vector(1, 1), // DPI scale ?
                //Avalonia.Platform.PixelFormat.Rgba8888
                Avalonia.Platform.PixelFormat.Bgra8888 // seems faster for me! // like System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            SystemBitmap = new System.Drawing.Bitmap(
                Size.Width,
                Size.Height,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );
        }

        public void CopyPixels() // copy pixels from SystemBitmap to AvaloniaBitmap
        {
            if (AvaloniaBitmap == null || SystemBitmap == null) return;

            using (var buf1 = AvaloniaBitmap.Lock())
            {
                long length1 = buf1.Size.Height * buf1.RowBytes;

                var buf2 = SystemBitmap.LockBits(
                    new System.Drawing.Rectangle(0, 0, Size.Width, Size.Height), 
                    System.Drawing.Imaging.ImageLockMode.ReadOnly,
                    SystemBitmap.PixelFormat
                );

                long length2 = buf2.Height * buf2.Stride;

                if (length1 == length2) {
                    unsafe {
                        System.Buffer.MemoryCopy(
                            buf2.Scan0.ToPointer(), 
                            buf1.Address.ToPointer(), 
                            length1, length2
                        );
                    }
                }

                SystemBitmap.UnlockBits(buf2);
            }
        }
    }

    public class MainWindow : Window
    {
        Avalonia.Controls.Image _mainImageControl = null;
        BitmapAdapter _mainBitmap = new BitmapAdapter();
        Avalonia.Point _pointerPos;
        Avalonia.Point _pointerPosDrag; // dragging start position

        TD.Viewport3 _viewport = null;

        private GridDrawer _gridDrawer;

        /*
        static MainWindow() {
            //MainWindow.Closing.AddClassHandler<MainWindow>(x => x.OnMyEvent));
        }
        */

        public MainWindow()
        {
            _gridDrawer = new GridDrawer();

            //TopLevel.AddHandler(this.Initialized, OnWindowInitialized, RoutingStrategies.Tunnel);
            this.Initialized += new EventHandler(OnWindowInitialized);

            InitializeComponent();
        }

        private void LogInfo(string template, params object[] args) {
            Avalonia.Logging.Logger.Information("Mine", this, template, args);
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            LogInfo(">>>> Test logger <<<<");

            _mainImageControl = this.FindControl<Avalonia.Controls.Image>("mainImage");
            System.Diagnostics.Debug.Assert(_mainImageControl != null, "mainImage not found");

            _mainImageControl.PointerMoved += MainImageControl_PointerMoved;
            _mainImageControl.PointerPressed += MainImageControl_PointerPressed;
            _mainImageControl.PointerWheelChanged += MainImageControl_PointerWheelChanged;

            var mainImagePanel = this.FindControl<Avalonia.Controls.Control>("mainImagePanel");
            if (mainImagePanel != null) {
                mainImagePanel
                    .GetObservable(Control.BoundsProperty)
                    .Subscribe(MainImagePanel_BoundsChanged);
            }
        }

        void OnWindowInitialized(object sender, EventArgs e)
        {
        }


        #region Pointer handling

        private static TD.Point ToPoint(Point p) {
            return new TD.Point((float)p.X, (float)p.Y);
        }

        private void MainImageControl_PointerMoved(object sender, PointerEventArgs e) {
            if (!e.InputModifiers.HasFlag(InputModifiers.RightMouseButton)) { // allow to move cursor out leaving selection/hignlighting
                _pointerPos = e.GetPosition(_mainImageControl);
            }
            if (e.InputModifiers.HasFlag(InputModifiers.MiddleMouseButton)) {
                TD.Point delta = ToPoint(_pointerPosDrag - _pointerPos);
                if (_viewport != null) {
                    _viewport.MoveOrigin(delta);
                }
                _pointerPosDrag = _pointerPos;
            } else {
                TD.Point u = _viewport.ToUser(ToPoint(_pointerPos));
                _gridDrawer.SetCursor(u.X, u.Y);
            }

            UpdateMainBitmap();
            _mainImageControl.InvalidateVisual();
        }

        private void MainImageControl_PointerPressed(object sender, PointerPressedEventArgs e) {
            if (_pointerPos != e.GetPosition(_mainImageControl)) return;
            // _gridDrawer.SetCursor already called from OnMouseMove
            if (e.InputModifiers.HasFlag(InputModifiers.LeftMouseButton))
            {
                // Get tempered note
                Drawing.SomeInterval t = null;
                if (e.InputModifiers.HasFlag(InputModifiers.Alt)) { // by cents
                    float c = _gridDrawer.GetCursorCents();
                    t = new Drawing.SomeInterval { cents = c };
                } else { // nearest rational
                    Rational r = _gridDrawer.UpdateCursorItem();
                    if (!r.IsDefault()) {
                        t = new Drawing.SomeInterval { rational = r };
                    }
                }
                if (t != null) {
                    /*
                    // Toggle selection
                    if (e.InputModifiers.HasFlag(InputModifiers.Control)) {
                        _toolsForm.ToggleSelection(t); // it calls ApplyDrawerSettings
                    }
                    // Play note
                    else {
#if USE_MIDI
                        _midiPlayer.NoteOn(0, t.ToCents(), duration: 8f);
#endif
                    }
                    */
                }
            }
            else if (e.InputModifiers.HasFlag(InputModifiers.MiddleMouseButton)) {
                _pointerPosDrag = _pointerPos;
            }
        }

        private void MainImageControl_PointerWheelChanged(object sender, PointerWheelEventArgs e) {
            bool shift = e.InputModifiers.HasFlag(InputModifiers.Shift);
            bool ctrl  = e.InputModifiers.HasFlag(InputModifiers.Control);
            bool alt   = e.InputModifiers.HasFlag(InputModifiers.Alt);

            float delta = (float)e.Delta.Y;

            if (shift || ctrl) {
                _viewport.AddScale(delta * 0.1f, straight: ctrl, ToPoint(_pointerPos));
            } else if (alt) {
                //!!!
                //_viewportSettings.scalePoint += e.Delta;
                //UpdatePointScale();
            } else {
                _viewport.MoveOrigin(new TD.Point(0, -delta * 10f));
            }

            //_toolsForm.MarkPresetChanged();

            UpdateMainBitmap(); //!!! 
            _mainImageControl.InvalidateVisual();
        }

        #endregion

        private void MainImagePanel_BoundsChanged(Rect bounds) {
            Console.WriteLine("mainImagePanel bounds -> {0}", bounds);
            if (bounds.IsEmpty) return;

            // update viewport
            TD.Point imageSize = new TD.Point((float)bounds.Width, (float)bounds.Height);
            if (_viewport == null) {
                _viewport = new TD.Viewport3(imageSize, new TD.Point(1, 1));
            } else {
                _viewport.SetImageSize(imageSize);
            }

            // update bitmap
            //!!! we need PixelSize. missing some transform?
            var pixelSize = new PixelSize((int)bounds.Width, (int)bounds.Height);
            _mainBitmap.Resize(pixelSize);

            // update image control
            if (_mainBitmap.IsEmpty()) {
                _mainImageControl.Source = null;
            } else {
                UpdateMainBitmap();
                _mainImageControl.Source = _mainBitmap.AvaloniaBitmap;
            }
            _mainImageControl.InvalidateVisual();
        }

        void UpdateMainBitmap() {
            if (_mainBitmap.IsEmpty()) return;

            // create grid image
            var image = DrawGrid();

            // render image to system bitmap
            using (var graphics = System.Drawing.Graphics.FromImage(_mainBitmap.SystemBitmap)) {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.Clear(System.Drawing.Color.White);
                image.Draw(graphics);
            }

            // copy pixels to avalonia bitmap
            _mainBitmap.CopyPixels();
        }

        TD.Image DrawGrid() {
            string harmonicityName = "Euler Barlow Tenney".Split()[1];

            var image = new TD.Image(_viewport);

            // configure drawer
            _gridDrawer.SetBounds(_viewport.GetUserBounds());
            _gridDrawer.SetBase(2, null, null);
            _gridDrawer.SetGeneration(harmonicityName, 500);
            _gridDrawer.SetPointRadiusFactor(2f);
            _gridDrawer.SetEDGrids(new[] { new GridDrawer.EDGrid { baseInterval = new Rational(2), stepCount = 12 } });
            _gridDrawer.SetSlope(new Rational(3,2), 2.0f);

            // generate grid items
            _gridDrawer.UpdateItems();

            Rational r = _gridDrawer.UpdateCursorItem();
            Console.WriteLine("CursorItem: {0}", r);

            // make image elements from grid items
            _gridDrawer.DrawGrid(image, 1);

            return image;
        }
    }
}