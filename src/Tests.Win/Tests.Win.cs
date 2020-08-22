using System;
using System.Collections.Generic;
//using System.Linq;

using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

#if ALLOW_SKIA
using SkiaSharp;
#elif ALLOW_GDI
using System.Drawing;
#endif

using Torec.Input;

// WPF implementation for Torec.Input.IImageInput interaction

//!!! Rename this project because here is no dependency from Tests.Base


namespace Rationals.Testing.Win
{
    public static class Utils
    {
        // Called directly from Main()
        public static void RunImageInput(IImageInput imageInput, string windowTitle = "ImageInput")
        {
            var window = new Window();
            window.Title = windowTitle;
            window.Content = new ImageInputControl(imageInput);
            window.Show();

            var app = new Application();
            app.MainWindow = window;
            app.Run();
        }
    }


    public class ImageInputControl : UIElement
    {
        WriteableBitmap _wb = null;
        //byte[] _pixels = null;

        IImageInput _imageInput = null;

        public ImageInputControl(IImageInput imageInput) {
            _imageInput = imageInput;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo info) {
            System.Diagnostics.Debug.WriteLine("OnRenderSizeChanged -> {0}", info.NewSize);
            base.OnRenderSizeChanged(info);

            int w = (int)info.NewSize.Width;
            int h = (int)info.NewSize.Height;
            _wb = new WriteableBitmap(w, h, 96, 96, PixelFormats.Pbgra32, null);

            /*
            _pixels = new byte[_wb.PixelWidth * _wb.PixelHeight * _wb.Format.BitsPerPixel / 8];
            */

            if (_imageInput != null) {
                bool redraw = _imageInput.OnSize(w, h);
                if (redraw) {
                    InvalidateVisual();
                }
            }
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            System.Diagnostics.Debug.WriteLine("OnMouseMove {0} {1} {2}", e.LeftButton, e.RightButton, e.GetPosition(this));
            base.OnMouseMove(e);

            if (_imageInput != null)
            {
                Point p = e.GetPosition(this);

                var buttons = new WindowInput.Buttons();
                if (e.LeftButton  == MouseButtonState.Pressed)        buttons |= WindowInput.Buttons.LeftMouseButton;
                if (e.RightButton == MouseButtonState.Pressed)        buttons |= WindowInput.Buttons.RightMouseButton;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))     buttons |= WindowInput.Buttons.Alt;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) buttons |= WindowInput.Buttons.Ctrl;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))   buttons |= WindowInput.Buttons.Shift;

                bool redraw = _imageInput.OnMouseMove(p.X, p.Y, buttons);
                if (redraw) {
                    InvalidateVisual();
                }
            }
        }

        protected override void OnRender(DrawingContext drawingContext) {
            System.Diagnostics.Debug.WriteLine("OnRender");
            //base.OnRender(drawingContext);

            /*
            if (_wb == null || _pixels == null) return;

            int m = System.DateTime.Now.Millisecond / 2;
            for (int i = 0; i < _pixels.Length; i += 4) {
                _pixels[i]   = (byte)(m + i + 200); // b
                _pixels[i+1] = (byte)(m + i + 500); // g
                _pixels[i+2] = (byte)(m + i + 800); // r
                _pixels[i+3] = (byte)0xFF;
            }

            Int32Rect rect = new Int32Rect(0, 0, _wb.PixelWidth, _wb.PixelHeight);
            int stride = _wb.PixelWidth * _wb.Format.BitsPerPixel / 8;

            //_wb.Lock();
            _wb.WritePixels(rect, _pixels, stride, 0);
            // or use Lock https://docs.microsoft.com/en-us/dotnet/api/system.windows.media.imaging.writeablebitmap?view=netcore-3.1
            //_wb.Unlock();

            drawingContext.DrawImage(_wb, new Rect(0, 0, _wb.Width, _wb.Height));
            */

            if (_imageInput != null && _wb != null) {
                var image = _imageInput.GetImage();
                if (image != null) {
                    var size = image.GetSize();
#if ALLOW_SKIA
                    var imageInfo = new SKImageInfo(
                        (int)size.X, (int)size.Y, 
                        SKColorType.Bgra8888, SKAlphaType.Premul
                    );
                    _wb.Lock();
                    using (var surface = SKSurface.Create(imageInfo, _wb.BackBuffer, _wb.BackBufferStride)) {
                        image.Draw(surface.Canvas, true);
                    }
                    _wb.AddDirtyRect(new Int32Rect(0, 0, (int)size.X, (int)size.Y));
                    _wb.Unlock();
#elif ALLOW_GDI
                    throw new Exception("Implement!!!");
#endif
                    drawingContext.DrawImage(_wb, new Rect(0, 0, _wb.Width, _wb.Height));
                }
            }

        }
    }

}