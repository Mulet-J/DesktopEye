using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using DesktopEye.Services.ScreenCaptureService;

namespace DesktopEye.Windows.Services;

public class WindowsScreenCaptureService : IScreenCaptureService
{
    private const uint DIB_RGB_COLORS = 0;

    public Bitmap CaptureScreen()
    {
        // Get the total screen size
        var screenLeft = GetSystemMetrics(SystemMetric.SM_XVIRTUALSCREEN);
        var screenTop = GetSystemMetrics(SystemMetric.SM_YVIRTUALSCREEN);
        var screenWidth = GetSystemMetrics(SystemMetric.SM_CXVIRTUALSCREEN);
        var screenHeight = GetSystemMetrics(SystemMetric.SM_CYVIRTUALSCREEN);

        // Create device contexts and bitmap
        var hdcScreen = GetDC(IntPtr.Zero);
        var hdcDest = CreateCompatibleDC(hdcScreen);
        var hBitmap = CreateCompatibleBitmap(hdcScreen, screenWidth, screenHeight);
        var hOld = SelectObject(hdcDest, hBitmap);

        // Copy screen to bitmap
        BitBlt(hdcDest, 0, 0, screenWidth, screenHeight, hdcScreen, screenLeft, screenTop,
            TernaryRasterOperations.SRCCOPY);

        // Create Avalonia bitmap
        var bitmap = new WriteableBitmap(
            new PixelSize(screenWidth, screenHeight),
            new Vector(96, 96),
            PixelFormat.Bgra8888,
            AlphaFormat.Premul);

        using (var buffer = bitmap.Lock())
        {
            // Get bitmap info
            var bmpInfo = new BITMAPINFO();
            bmpInfo.biSize = (uint)Marshal.SizeOf<BITMAPINFO>();
            bmpInfo.biWidth = screenWidth;
            bmpInfo.biHeight = -screenHeight; // Negative to get top-down bitmap
            bmpInfo.biPlanes = 1;
            bmpInfo.biBitCount = 32;
            bmpInfo.biCompression = 0; // BI_RGB

            // Copy pixel data directly to Avalonia bitmap
            GetDIBits(
                hdcDest,
                hBitmap,
                0,
                (uint)screenHeight,
                buffer.Address,
                ref bmpInfo,
                DIB_RGB_COLORS);
        }

        // Cleanup
        SelectObject(hdcDest, hOld);
        DeleteDC(hdcDest);
        ReleaseDC(IntPtr.Zero, hdcScreen);
        DeleteObject(hBitmap);

        return bitmap;
    }

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(SystemMetric nIndex);

    [DllImport("user32.dll")]
    private static extern IntPtr GetDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern int ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
        IntPtr hdcSrc, int nXSrc, int nYSrc, TernaryRasterOperations dwRop);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan,
        uint cScanLines, IntPtr lpvBits, ref BITMAPINFO lpbi, uint uUsage);

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
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

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 256)]
        public uint[] cols;
    }

    private enum SystemMetric
    {
        SM_XVIRTUALSCREEN = 76,
        SM_YVIRTUALSCREEN = 77,
        SM_CXVIRTUALSCREEN = 78,
        SM_CYVIRTUALSCREEN = 79
    }

    private enum TernaryRasterOperations : uint
    {
        SRCCOPY = 0x00CC0020
    }
}