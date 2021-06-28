using System;
using System.Collections.Generic;
//using System.Linq;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Interop;

#if ALLOW_SKIA
using SkiaSharp;
#elif ALLOW_GDI
using System.Drawing;
#endif

// WPF implementation for Torec.UI logic

using Window = System.Windows.Window;
using Image  = Torec.Drawing.Image;

namespace Torec.UI.Win
{
    public static class Utils
    {


        // Called directly from Main()
        public static void RunWindow(WindowInfo<Window, Image> wi)
        {
            Window window = wi.window ?? new Window();

            if (wi.title != null) {
                window.Title = wi.title;
            }

            // Create native controls
            foreach (var ci in wi.controls) {
                var nativeControl = new NativeControl(ci.logic, ci.drawer, ci.contextId);
                window.KeyDown += nativeControl.Window_KeyDown;
                if (ci.nativeName == null) {
                    window.Content = nativeControl; // set the whole window content
                } else {
                    var node = LogicalTreeHelper.FindLogicalNode(window, ci.nativeName);
                    if (node is Panel panel) {
                        panel.Children.Add(nativeControl);
                    } else {
                        //!!! report
                    }
                }
            }

            window.Show();
            
            // Run application
            var app = new Application();
            app.MainWindow = window;
            app.Run();
        }
    }


    internal class NativeControl : UIElement
    {
        IInteractiveControl _model = null;
        IDrawer<Image> _drawer = null;
        int _contextId = 0;

        WriteableBitmap _wb = null;

        internal NativeControl(IInteractiveControl model, IDrawer<Image> drawer, int contextId = 0) {
            _model     = model;
            _drawer    = drawer;
            _contextId = contextId;
            //

            bool isIdleNeeded = true;
            if (isIdleNeeded) {
                ComponentDispatcher.ThreadIdle += (object sender, EventArgs e) => {
                    _model.DoIdle();
                };
            }

            //
            _drawer.UpdateImage += this.Redraw;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo info) {
            //System.Diagnostics.Debug.WriteLine("OnRenderSizeChanged -> {0}", info.NewSize);
            base.OnRenderSizeChanged(info);

            int w = (int)info.NewSize.Width;
            int h = (int)info.NewSize.Height;
            _wb = new WriteableBitmap(w,h, 96,96, PixelFormats.Pbgra32, null);

            Redraw();
        }

        protected KeyModifiers MakeKeyModifiers(ModifierKeys k) { // System.Windows.Input -> Torec.UI
            var mods = new KeyModifiers();
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))     mods |= KeyModifiers.Alt;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) mods |= KeyModifiers.Ctrl;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))   mods |= KeyModifiers.Shift;
            return mods;
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            //System.Diagnostics.Debug.WriteLine("OnMouseMove {0} {1} {2}", e.LeftButton, e.RightButton, e.GetPosition(this));
            base.OnMouseMove(e);

            if (_model != null && _wb != null)
            {
                var mouse = new MouseButtons();
                if (e.LeftButton  == MouseButtonState.Pressed) mouse |= MouseButtons.Left;
                if (e.RightButton == MouseButtonState.Pressed) mouse |= MouseButtons.Right;
                var mods = MakeKeyModifiers(Keyboard.Modifiers);

                Point p = e.GetPosition(this);
                double x01 = p.X / _wb.Width; //!!! or _wb.PixelWidth ?
                double y01 = p.Y / _wb.Height;

                _model.OnMouseMove(x01, y01, mouse, mods); // ImageUpdated event will be raised there if needed
            }
        }

        internal void Window_KeyDown(object sender, KeyEventArgs e) {
            if (_model != null) {
                int keyCode = (int)e.Key;
                var mods = MakeKeyModifiers(Keyboard.Modifiers);
                _model.OnKeyDown(keyCode, mods);
            }
        }

        protected void Redraw() {
            UpdateInternalBitmap();
            base.InvalidateVisual();
        }

        protected void UpdateInternalBitmap() {
            if (_drawer == null || _wb == null) return;

            // Update inner bitmap
            int w = _wb.PixelWidth;
            int h = _wb.PixelHeight;

            // get updated image from control
            var image = _drawer.GetImage(w,h, _contextId);
            if (image == null) return;

#if ALLOW_SKIA
            var imageInfo = new SKImageInfo(w,h, SKColorType.Bgra8888, SKAlphaType.Premul);
            _wb.Lock();
            using (var surface = SKSurface.Create(imageInfo, _wb.BackBuffer, _wb.BackBufferStride)) {
                if (surface != null) {
                    // render image to the bitmap
                    image.Draw(surface.Canvas, true);
                } else {
                }
            }
            _wb.AddDirtyRect(new Int32Rect(0,0, w,h));
            _wb.Unlock();
#elif ALLOW_GDI
            throw new Exception("Implement!!!");
#endif
        }


        protected override void OnRender(DrawingContext drawingContext)
        {
            //System.Diagnostics.Debug.WriteLine("OnRender");
            //base.OnRender(drawingContext);

            if (_wb != null) {
                drawingContext.DrawImage(_wb, new Rect(0, 0, _wb.Width, _wb.Height));
            }
        }
    }

}