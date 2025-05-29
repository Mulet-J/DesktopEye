using System;
using System.IO;
using System.Runtime.InteropServices;
using Avalonia.Media.Imaging;
using SkiaSharp;
using DesktopEye.Services.ScreenCaptureService;

namespace DesktopEye.MacOS
{
    public class MacOsScreenCaptureService : IScreenCaptureService
    {
        // API CoreGraphics pour macOS
        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern IntPtr CGMainDisplayID();

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern IntPtr CGDisplayCreateImage(IntPtr displayId);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern int CGImageGetWidth(IntPtr cgImage);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern int CGImageGetHeight(IntPtr cgImage);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern IntPtr CGImageGetDataProvider(IntPtr cgImage);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern IntPtr CGDataProviderCopyData(IntPtr provider);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern IntPtr CFDataGetBytePtr(IntPtr data);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern long CFDataGetLength(IntPtr data);

        [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
        private static extern void CGImageRelease(IntPtr image);

        [DllImport("/System/Library/Frameworks/CoreFoundation.framework/CoreFoundation")]
        private static extern void CFRelease(IntPtr cf);

        public Bitmap CaptureScreen()
        {
            // Obtenir l'ID de l'affichage principal
            IntPtr displayId = CGMainDisplayID();

            // Créer une image de l'affichage principal
            IntPtr cgImage = CGDisplayCreateImage(displayId);
            if (cgImage == IntPtr.Zero)
            {
                throw new Exception("Échec de la capture d'écran.");
            }

            try
            {
                // Obtenir les dimensions de l'image
                int width = CGImageGetWidth(cgImage);
                int height = CGImageGetHeight(cgImage);


                // Obtenir les données de l'image
                IntPtr provider = CGImageGetDataProvider(cgImage);
                IntPtr dataRef = CGDataProviderCopyData(provider);
                IntPtr bytePtr = CFDataGetBytePtr(dataRef);
                long length = CFDataGetLength(dataRef);


                // Créer une copie des données dans un tableau géré
                byte[] managedArray = new byte[length];
                Marshal.Copy(bytePtr, managedArray, 0, (int)length);

                // Créer un MemoryStream à partir des données
                using (var memoryStream = new MemoryStream(managedArray))
                {
                    // BGRA est le format utilisé par CoreGraphics sur macOS
                    // Créer un Bitmap Avalonia directement à partir des données
                    var bitmap = new Bitmap(
                        Avalonia.Platform.PixelFormat.Bgra8888,
                        Avalonia.Platform.AlphaFormat.Premul,
                        bytePtr,
                        new Avalonia.PixelSize(width, height),
                        new Avalonia.Vector(96, 96),
                        width * 4);

                    // Libérer la référence aux données CoreFoundation
                    CFRelease(dataRef);

                    return bitmap;
                }
            }
            finally
            {
                // Libérer les ressources CoreGraphics
                CGImageRelease(cgImage);
            }
        }
        
    }
}