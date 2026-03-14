using RMF.Core.Screen;
using RMF_Client.Logic;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RMF_Client.Capture
{
    internal class WindowsCapturer : BaseCapturer
    {
        // I don't have any idea what this is, but I didn't want to take a regular screenshot through intermediaries
        // It will be much faster to intercept screen bytes directly through system components, this should be more efficient in terms of memory
        [StructLayout(LayoutKind.Sequential)]
        public struct BITMAPINFOHEADER
        {
            public uint biSize;
            public int biWidth;
            public int biHeight;
            public ushort biPlanes;
            public ushort biBitCount;
            public uint biCompression;
            public uint biSizeImage;
            public int biXPelsPerMeter;
            public int biYPelsPerMeter;
            public uint biClrUsed;
            public uint biClrImportant;
        }

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetWindowDC(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateCompatibleBitmap(IntPtr hDC, int nWidth, int nHeight);

        [DllImport("gdi32.dll")]
        private static extern IntPtr SelectObject(IntPtr hDC, IntPtr hObject);

        [DllImport("gdi32.dll")]
        private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight, IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

        [DllImport("gdi32.dll")]
        private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines, IntPtr lpvBits, ref BITMAPINFOHEADER lpbmi, uint uUsage);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteDC(IntPtr hDC);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        private const uint SrcCopyCode = 0x00CC0020;
        private int IterationCounter = 0;

        protected override void UpdateScreenMetrics()
        {
            lock (this.ScreenGetterLock)
            {
                this.ScreenWidth = GetSystemMetrics(0);
                this.ScreenHeight = GetSystemMetrics(1);

                this.ScreenBitmap?.Dispose();
                this.ScreenBitmap = new(this.ScreenWidth, this.ScreenHeight, SKColorType.Bgra8888, SKAlphaType.Premul);
            }
        }

        protected override SKBitmap? GetScreenBitmap()
        {
            // Automatic metrics update is called once every X iterations to avoid hogging the processor with useless calls
            if (this.IterationCounter++ % ConfigurationManager.AutoMetricsUpdateRate == 0 &&
                (GetSystemMetrics(0) != this.ScreenWidth || GetSystemMetrics(1) != this.ScreenHeight))
            {
                UpdateScreenMetrics();
            }

            lock (this.ScreenGetterLock)
            {
                IntPtr hwnd = GetDesktopWindow();
                IntPtr hdcSrc = GetWindowDC(hwnd);
                if (hdcSrc == IntPtr.Zero)
                {
                    return null;
                }

                IntPtr hdcDest = CreateCompatibleDC(hdcSrc);
                IntPtr hBitmap = CreateCompatibleBitmap(hdcSrc, this.ScreenWidth, this.ScreenHeight);
                IntPtr hOld = SelectObject(hdcDest, hBitmap);

                BitBlt(hdcDest, 0, 0, this.ScreenWidth, this.ScreenHeight, hdcSrc, 0, 0, SrcCopyCode);

                BITMAPINFOHEADER bmi = new()
                {
                    biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>(),
                    biWidth = this.ScreenWidth,
                    biHeight = -this.ScreenHeight,
                    biPlanes = 1,
                    biBitCount = 32,
                    biCompression = 0
                };

                _ = GetDIBits(hdcDest, hBitmap, 0, (uint)this.ScreenHeight, this.ScreenBitmap!.GetPixels(), ref bmi, 0);

                SelectObject(hdcDest, hOld);
                DeleteObject(hBitmap);
                DeleteDC(hdcDest);
                _ = ReleaseDC(hwnd, hdcSrc);

                return this.ScreenBitmap;
            }
        }
    }
}
