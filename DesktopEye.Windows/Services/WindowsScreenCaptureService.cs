using System;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using DesktopEye.Services.ScreenCaptureService;
using SkiaSharp;

namespace DesktopEye.Windows.Services;

public class WindowsScreenCaptureService : IScreenCaptureService
{
    // Windows API constants
    private const int SRCCOPY = 0x00CC0020;
    private const uint DIB_RGB_COLORS = 0;

    // System metrics constants
    private const int SM_CXSCREEN = 0;
    private const int SM_CYSCREEN = 1;
    private const int SM_XVIRTUALSCREEN = 76;
    private const int SM_YVIRTUALSCREEN = 77;
    private const int SM_CXVIRTUALSCREEN = 78;
    private const int SM_CYVIRTUALSCREEN = 79;

    /// <summary>
    ///     Captures the entire user's workspace (all monitors) as a SkiaSharp bitmap
    /// </summary>
    /// <returns>SKBitmap containing the captured workspace</returns>
    public Bitmap CaptureScreen()
    {
        // Get the virtual screen dimensions (all monitors combined)
        var x = GetSystemMetrics(SM_XVIRTUALSCREEN);
        var y = GetSystemMetrics(SM_YVIRTUALSCREEN);
        var width = GetSystemMetrics(SM_CXVIRTUALSCREEN);
        var height = GetSystemMetrics(SM_CYVIRTUALSCREEN);

        return CaptureScreen(x, y, width, height);
    }

    // Windows API declarations
    [DllImport("user32.dll")]
    private static extern IntPtr GetDesktopWindow();

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindowDC(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr ReleaseDC(IntPtr hWnd, IntPtr hDC);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    private static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    private static extern bool BitBlt(IntPtr hdc, int nXDest, int nYDest, int nWidth, int nHeight,
        IntPtr hdcSrc, int nXSrc, int nYSrc, uint dwRop);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    private static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    private static extern int GetSystemMetrics(int nIndex);

    [DllImport("gdi32.dll")]
    private static extern int GetDIBits(IntPtr hdc, IntPtr hbmp, uint uStartScan, uint cScanLines,
        IntPtr lpvBits, ref BITMAPINFO lpbmi, uint uUsage);

    /// <summary>
    ///     Internal method to capture a screen region directly to SkiaSharp bitmap
    /// </summary>
    private static Bitmap CaptureScreen(int x, int y, int width, int height)
    {
        // Get desktop window and device context
        var desktopWindow = GetDesktopWindow();
        var desktopDC = GetWindowDC(desktopWindow);

        try
        {
            // Create compatible DC and bitmap
            var memoryDC = CreateCompatibleDC(desktopDC);
            var bitmap = CreateCompatibleBitmap(desktopDC, width, height);
            var oldBitmap = SelectObject(memoryDC, bitmap);

            try
            {
                // Copy the screen to our bitmap
                var success = BitBlt(memoryDC, 0, 0, width, height, desktopDC, x, y, SRCCOPY);

                if (!success) throw new InvalidOperationException("Failed to capture screen content");

                // Convert Windows bitmap directly to SkiaSharp bitmap
                return ConvertToSkiaBitmapDirect(desktopDC, bitmap, width, height);
            }
            finally
            {
                // Clean up GDI objects
                SelectObject(memoryDC, oldBitmap);
                DeleteObject(bitmap);
                DeleteDC(memoryDC);
            }
        }
        finally
        {
            // Release desktop DC
            ReleaseDC(desktopWindow, desktopDC);
        }
    }

    /// <summary>
    ///     Converts a Windows HBITMAP directly to a SkiaSharp bitmap using GetDIBits
    /// </summary>
    private static Bitmap ConvertToSkiaBitmapDirect(IntPtr hdc, IntPtr hBitmap, int width, int height)
    {
        // Create SkiaSharp bitmap
        var bitmap = new SKBitmap(width, height, SKColorType.Bgra8888, SKAlphaType.Opaque);

        // Setup BITMAPINFO structure
        var bmi = new BITMAPINFO();
        bmi.bmiHeader.biSize = (uint)Marshal.SizeOf<BITMAPINFOHEADER>();
        bmi.bmiHeader.biWidth = width;
        bmi.bmiHeader.biHeight = -height; // Negative height for top-down bitmap
        bmi.bmiHeader.biPlanes = 1;
        bmi.bmiHeader.biBitCount = 32; // 32 bits per pixel (BGRA)
        bmi.bmiHeader.biCompression = 0; // BI_RGB
        bmi.bmiHeader.biSizeImage = 0;
        bmi.bmiHeader.biXPelsPerMeter = 0;
        bmi.bmiHeader.biYPelsPerMeter = 0;
        bmi.bmiHeader.biClrUsed = 0;
        bmi.bmiHeader.biClrImportant = 0;

        // Get pixel data directly from Windows bitmap
        var pixelPtr = bitmap.GetPixels();

        var result = GetDIBits(hdc, hBitmap, 0, (uint)height, pixelPtr, ref bmi, DIB_RGB_COLORS);

        if (result == 0)
        {
            bitmap.Dispose();
            throw new InvalidOperationException("Failed to get bitmap pixel data");
        }

        // The pixel data is now directly in the SkiaSharp bitmap
        // Windows gives us BGRA format which matches SkiaSharp's Bgra8888 format
        return bitmap;
    }

    // BITMAPINFO structure
    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFO
    {
        public BITMAPINFOHEADER bmiHeader;

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
        public uint[] bmiColors;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct BITMAPINFOHEADER
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
}