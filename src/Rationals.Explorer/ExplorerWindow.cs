using System;
using System.Collections.ObjectModel;

using Avalonia;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Rationals.Explorer
{
    public class MainWindow : Window
    {
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

            var image1 = this.FindControl<Avalonia.Controls.Image>("image1");
            if (image1 != null)
            {
                int w = 400;
                int h = 400;

                var bitmap1 = new Avalonia.Media.Imaging.WriteableBitmap(
                    new PixelSize(w,h),
                    new Vector(300,300), // DPI ?
                    //Avalonia.Platform.PixelFormat.Rgba8888
                    Avalonia.Platform.PixelFormat.Bgra8888 // seems faster for me! // like System.Drawing.Imaging.PixelFormat.Format32bppArgb
                );

#if false
                using (var buf = bitmap1.Lock()) {
                    unsafe {
                        uint* ptr = (uint*)buf.Address;
                        for (uint i = 0; i < h; ++i) {
                            for (uint j = 0; j < w; ++j) {
                                uint c = 0xFF000000;
                                c |= ((i * 2) & 0xFF) << 16; // Blue
                                c |= ((j * 2) & 0xFF) << 0;  // Red
                                *(ptr + i*w + j) = c;
                            }
                        }
                    }
                }
#else
                // build image
                var viewport = new Torec.Drawing.Viewport(w,h, 0,20, 0,20, false);
                var image = new Torec.Drawing.Image(viewport);
                Rationals.DrawingSamples.DrawTest_Pjosik(image);

                using (var bitmap2 = new System.Drawing.Bitmap(w,h, System.Drawing.Imaging.PixelFormat.Format32bppArgb)) {
                    // render to bitmap
                    using (var graphics = System.Drawing.Graphics.FromImage(bitmap2)) {
                        image.Draw(graphics);
                    }
                    // copy pixels from System.Drawing.Bitmap to Avalonia WriteableBitmap
                    using (var buf1 = bitmap1.Lock()) {

                        long length1 = buf1.Size.Height * buf1.RowBytes;

                        var buf2 = bitmap2.LockBits(
                            new System.Drawing.Rectangle(0, 0, bitmap2.Width, bitmap2.Height), 
                            System.Drawing.Imaging.ImageLockMode.ReadOnly,
                            bitmap2.PixelFormat
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
                    }

                }
#endif
                image1.Source = bitmap1;

            }
        }

        void OnWindowInitialized(object sender, System.EventArgs e)
        {
            var panel1 = this.FindControl<Panel>("panel1");
            //CreateDataGrid1(panel1);

            //panel1.Children.Add(new TextBox { Text = "text" });

        }
    }
}