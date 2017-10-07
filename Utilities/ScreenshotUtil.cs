using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Imaging;

using Drawing = System.Drawing;

namespace Screenshotter
{
    public static class ScreenshotUtil
    {
        public static Bitmap GetScreenshot()
        {
            var bmp = new Bitmap(
                           Screen.PrimaryScreen.Bounds.Width,
                           Screen.PrimaryScreen.Bounds.Height
                      );
            var g = Graphics.FromImage(bmp);

            g.CopyFromScreen(new Drawing.Point(0, 0), new Drawing.Point(0, 0), bmp.Size);
            g.Dispose();

            return bmp;
        }


        [DllImport("gdi32.dll")]
        public static extern bool DeleteObject(IntPtr hObject);

        public static BitmapSource ToBitmapSource(this Bitmap bmp)
        {
            BitmapSource source;
            var hBitmap = bmp.GetHbitmap();

            try
            {
                source = Imaging.CreateBitmapSourceFromHBitmap(
                    hBitmap, IntPtr.Zero, Int32Rect.Empty,
                    BitmapSizeOptions.FromEmptyOptions()
                );
            }
            finally
            {
                DeleteObject(hBitmap);
            }

            return source;
        }
    }
}