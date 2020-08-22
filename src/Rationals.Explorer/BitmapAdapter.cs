using System;

using Avalonia;

using SD = System.Drawing;
using AI = Avalonia.Media.Imaging;

//!!! https://github.com/AvaloniaUI/Avalonia/issues/2492 How to render SKCanvas with DrawingContext? [Question]

namespace Rationals.Explorer
{
    // Used to convert 
    //    System.Drawing.Bitmap (GridDrawer draws on it)
    // to 
    //    Avalonia.Media.Imaging.WriteableBitmap (to set to Avalonia.Controls.Image.Source)

    class BitmapAdapter { //!!! IDisposable?
        public AI.WriteableBitmap AvaloniaBitmap = null;
        public SD.Bitmap[] SystemBitmaps;

        public BitmapAdapter(int systemBitmapCount) {
            SystemBitmaps = new SD.Bitmap[systemBitmapCount];
        }

        public void DisposeAll() {
            if (AvaloniaBitmap != null) {
                AvaloniaBitmap.Dispose();
                AvaloniaBitmap = null;
            }
            for (int i = 0; i < SystemBitmaps.Length; ++i) { 
                if (SystemBitmaps[i] != null) {
                    SystemBitmaps[i].Dispose();
                    SystemBitmaps[i] = null;
                }
            }
        }

        public static bool EnsureBitmapSize(ref SD.Bitmap systemBitmap, SD.Size size) {
            bool invalid = systemBitmap != null && systemBitmap.Size != size;
            bool create = systemBitmap == null || invalid;
            if (invalid) {
                systemBitmap.Dispose();
                systemBitmap = null;
            }
            if (create) {
                systemBitmap = new SD.Bitmap(
                    size.Width, size.Height,
                    SD.Imaging.PixelFormat.Format32bppArgb
                );
            }
            return create;
        }

        public static bool EnsureBitmapSize(ref AI.WriteableBitmap avaloniaBitmap, PixelSize size) {
            bool invalid = avaloniaBitmap != null && avaloniaBitmap.PixelSize != size;
            bool create = avaloniaBitmap == null || invalid;
            if (invalid) {
                avaloniaBitmap.Dispose();
                avaloniaBitmap = null;
            }
            if (create) {
                avaloniaBitmap = new AI.WriteableBitmap(
                    size,
                    new Vector(1, 1), // DPI scale ?
                    //Avalonia.Platform.PixelFormat.Rgba8888
                    Avalonia.Platform.PixelFormat.Bgra8888 // seems faster for me! // like System.Drawing.Imaging.PixelFormat.Format32bppArgb
                );
            }
            return create;
        }

        /*
        public void Resize(PixelSize size) {
            if (size == Size) return;
            Size = size;

            Dispose();

            if (IsEmpty()) return;

            AvaloniaBitmap = new AI.WriteableBitmap(
                Size,
                new Vector(1, 1), // DPI scale ?
                //Avalonia.Platform.PixelFormat.Rgba8888
                Avalonia.Platform.PixelFormat.Bgra8888 // seems faster for me! // like System.Drawing.Imaging.PixelFormat.Format32bppArgb
            );

            for (int i = 0; i < SystemBitmaps.Length; ++i) {
                SystemBitmaps[i] = new SD.Bitmap(
                    Size.Width, Size.Height,
                    SD.Imaging.PixelFormat.Format32bppArgb
                );
            }
        }
        */

        public bool CopyPixels(int sourceIndex) // copy pixels from SystemBitmap to AvaloniaBitmap. UI thread
        {
            //Debug.WriteLine("CopyPixels begin");

            SD.Bitmap systemBitmap = SystemBitmaps[sourceIndex];
            if (systemBitmap == null) return false;
            if (AvaloniaBitmap == null) return false;

            using (var buf1 = AvaloniaBitmap.Lock())
            {
                long length1 = buf1.Size.Height * buf1.RowBytes;

                var buf0 = systemBitmap.LockBits(
                    new SD.Rectangle(SD.Point.Empty, systemBitmap.Size),
                    SD.Imaging.ImageLockMode.ReadOnly,
                    systemBitmap.PixelFormat
                );

                long length0 = buf0.Height * buf0.Stride;

                if (length1 == length0) {
                    // quick. just copy memory
                    unsafe {
                        System.Buffer.MemoryCopy(
                            buf0.Scan0.ToPointer(), 
                            buf1.Address.ToPointer(), 
                            length1, length0
                        );
                    }
                } else {
                    // slow. copy by line. may occure on resizing
                    int h = Math.Min(buf0.Height, buf1.Size.Height); // in pixels
                    int w = Math.Min(buf0.Stride, buf1.RowBytes); // in bytes
                    unsafe {
                        for (int i = 0; i < h; ++i) {
                            System.Buffer.MemoryCopy(
                                (buf0.Scan0 + buf0.Stride * i).ToPointer(),
                                (buf1.Address + buf1.RowBytes * i).ToPointer(),
                                w, w
                            );
                        }
                    }
                }

                systemBitmap.UnlockBits(buf0);
            }

            //Debug.WriteLine("CopyPixels end");

            return true;
        }
    }
}
