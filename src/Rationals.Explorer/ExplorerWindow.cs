using System;
using System.Collections.ObjectModel;

using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

//using Torec.Drawing;
using Rationals.Drawing;

namespace Rationals.Explorer
{
    class BitmapAdapter { //!!! IDisposable?
        public Avalonia.Media.Imaging.WriteableBitmap AvaloniaBitmap = null;
        public System.Drawing.Bitmap SystemBitmap = null;
        public PixelSize Size = PixelSize.Empty;

        public bool Empty() {
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

            if (Empty()) return;

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

        static MainWindow() {
            //MainWindow.Closing.AddClassHandler<MainWindow>(x => x.OnMyEvent));
        }

        public MainWindow()
        {
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

            var mainPanel = this.FindControl<Avalonia.Controls.Control>("mainPanel");
            if (mainPanel != null) {
                // https://avaloniaui.net/docs/binding/binding-from-code
                mainPanel
                    .GetObservable(Control.BoundsProperty)
                    .Subscribe(MainPanelBoundsChanged);
            }
        }

        void OnWindowInitialized(object sender, System.EventArgs e)
        {
            //var panel1 = this.FindControl<Panel>("panel1");
            //CreateDataGrid1(panel1);

            //panel1.Children.Add(new TextBox { Text = "text" });

        }

        void MainPanelBoundsChanged(Rect bounds) {
            //Console.WriteLine("mainPanel bounds -> {0}", bounds);

            //!!! we need PixelSize. missing some transform?
            var size = new PixelSize((int)bounds.Width, (int)bounds.Height);

            _mainBitmap.Resize(size);

            if (_mainBitmap.Empty()) {
                _mainImageControl.Source = null;
            } else {
                _mainImageControl.Source = _mainBitmap.AvaloniaBitmap;
                // 
                UpdateMainBitmap();
            }

        }

        void UpdateMainBitmap() {
            if (_mainBitmap.Empty()) return;

            // create our image
            /*
            var viewport = new Torec.Drawing.Viewport(_mainBitmap.Size.Width, _mainBitmap.Size.Height, 0,20, 0,20, false);
            var image = new Torec.Drawing.Image(viewport);
            Rationals.DrawingSamples.DrawTest_Pjosik(image);
            */
            var image = DrawGrid(_mainBitmap.Size.Width, _mainBitmap.Size.Height);

            // render image to system bitmap
            using (var graphics = System.Drawing.Graphics.FromImage(_mainBitmap.SystemBitmap)) {
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
                graphics.Clear(System.Drawing.Color.White);
                image.Draw(graphics);
            }

            // copy pixels to avalonia bitmap
            _mainBitmap.CopyPixels();

        }

        Torec.Drawing.Image DrawGrid(int w, int h) {
            string harmonicityName = "Euler Barlow Tenney".Split()[1];

            var viewport = new Torec.Drawing.Viewport(w,h, -1,1, -3,3);
            var image = new Torec.Drawing.Image(viewport);

            var drawer = new GridDrawer();
            
            // configure drawer
            drawer.SetBounds(viewport.GetUserBounds());
            drawer.SetBase(2, null, null);
            drawer.SetGeneration(harmonicityName, 500);
            drawer.SetPointRadiusFactor(3f);
            drawer.SetEDGrids(new[] { new GridDrawer.EDGrid { baseInterval = new Rational(2), stepCount = 12 } });
            drawer.SetSlope(new Rational(3,2), 2.0f);

            // generate grid items
            drawer.UpdateItems();

            // make image elements from grid items
            drawer.DrawGrid(image, 0);

            return image;
        }
    }
}