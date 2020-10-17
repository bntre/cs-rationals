using System;
using System.Collections.Generic;
//using System.Linq;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
        InteractiveControl _logic = null;
        IDrawer<Image> _drawer = null;
        int _contextId = 0;

        WriteableBitmap _wb = null;

        internal NativeControl(InteractiveControl logic, IDrawer<Image> drawer, int contextId = 0) {
            _logic     = logic;
            _drawer    = drawer;
            _contextId = contextId;
            //
            _logic.Invalidated += this.Redraw;
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo info) {
            System.Diagnostics.Debug.WriteLine("OnRenderSizeChanged -> {0}", info.NewSize);
            base.OnRenderSizeChanged(info);

            int w = (int)info.NewSize.Width;
            int h = (int)info.NewSize.Height;
            _wb = new WriteableBitmap(w,h, 96,96, PixelFormats.Pbgra32, null);

            Redraw();
        }

        protected override void OnMouseMove(MouseEventArgs e) {
            System.Diagnostics.Debug.WriteLine("OnMouseMove {0} {1} {2}", e.LeftButton, e.RightButton, e.GetPosition(this));
            base.OnMouseMove(e);

            if (_logic != null && _wb != null)
            {
                var buttons = new InteractiveControl.MouseButtons();
                if (e.LeftButton  == MouseButtonState.Pressed)        buttons |= InteractiveControl.MouseButtons.Left;
                if (e.RightButton == MouseButtonState.Pressed)        buttons |= InteractiveControl.MouseButtons.Right;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Alt))     buttons |= InteractiveControl.MouseButtons.Alt;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Control)) buttons |= InteractiveControl.MouseButtons.Ctrl;
                if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))   buttons |= InteractiveControl.MouseButtons.Shift;

                Point p = e.GetPosition(this);
                double x01 = p.X / _wb.Width; //!!! or _wb.PixelWidth ?
                double y01 = p.Y / _wb.Height;

                _logic.SetMouseMove(x01, y01, buttons); // ImageUpdated event will be raised there if needed
            }
        }

        protected override void OnKeyDown(KeyEventArgs e) {
            /*
            Key newKey = e.Key;

            if (e.Key == Key.A)
            {
                //handle the event and cancel the original key
                e.Handled = true;

                //get caret position
                int tbPos = this.SelectionStart;

                //insert the new text at the caret position
                this.Text = this.Text.Insert(tbPos, "b");

                newKey = Key.B;

                //replace the caret back to where it should be 
                //otherwise the insertion call above will reset the position
                this.Select(tbPos + 1, 0);
            }
            */
            base.OnKeyDown(e);
        }

        protected void Redraw() {
            UpdateInternalBitmap();
            base.InvalidateVisual();
        }

        protected void UpdateInternalBitmap() {
            if (_logic == null || _drawer == null || _wb == null) return;

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
            System.Diagnostics.Debug.WriteLine("OnRender");
            //base.OnRender(drawingContext);

            if (_wb != null) {
                drawingContext.DrawImage(_wb, new Rect(0, 0, _wb.Width, _wb.Height));
            }
        }
    }

}