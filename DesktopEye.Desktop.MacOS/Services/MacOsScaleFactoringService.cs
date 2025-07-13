using System;
using System.Runtime.InteropServices;
using Avalonia;
using DesktopEye.Common.Application.Views.Controls;
using DesktopEye.Common.Infrastructure.Services.ScreenCapture;

namespace DesktopEye.Desktop.MacOS.Services;

public class MacOsScaleFactoringService : IScaleFactoringService
{
    public double GetOsScaleFactor(AreaSelectionControl areaSelectionControl)
    {
        try
        {
            // Méthode 1: Via NSScreen (le plus fiable)
            var scaleFactor = GetNsScreenBackingScaleFactor();
            if (scaleFactor > 0)
            {
                return scaleFactor;
            }

            // Méthode 2: Via CoreGraphics comme fallback
            return GetCoreGraphicsScaleFactor();
        }
        catch (Exception)
        {
            // Fallback sécurisé - la plupart des Mac modernes sont Retina
            return 2.0;
        }
    }
    
    private double GetNsScreenBackingScaleFactor()
    {
        try
        {
            // Obtenir la classe NSScreen
            var nsScreenClass = objc_getClass("NSScreen");
            if (nsScreenClass == IntPtr.Zero) return 0;

            // Obtenir l'écran principal
            var mainScreenSelector = sel_registerName("mainScreen");
            var mainScreen = objc_msgSend(nsScreenClass, mainScreenSelector);
            if (mainScreen == IntPtr.Zero) return 0;

            // Obtenir le facteur d'échelle
            var backingScaleFactorSelector = sel_registerName("backingScaleFactor");
            var scaleFactorDouble = objc_msgSend_fpret(mainScreen, backingScaleFactorSelector);

            return scaleFactorDouble;
        }
        catch
        {
            return 0; // Indique un échec
        }
    }
    
    private double GetCoreGraphicsScaleFactor()
    {
        try
        {
            var displayId = CGMainDisplayID();

            // Obtenir le mode d'affichage actuel
            var mode = CGDisplayCopyDisplayMode(displayId);
            if (mode == IntPtr.Zero) return 1.0;

            try
            {
                // Obtenir les dimensions en pixels et en points
                var pixelWidth = CGDisplayModeGetPixelWidth(mode);
                var pointWidth = CGDisplayModeGetWidth(mode);

                if (pointWidth > 0 && pixelWidth > 0)
                {
                    return (double)pixelWidth / pointWidth;
                }
            }
            finally
            {
                CGDisplayModeRelease(mode);
            }

            return 1.0;
        }
        catch
        {
            return 1.0;
        }
    }
    
    #region macOS APIs

    // APIs Objective-C Runtime pour NSScreen
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr objc_getClass(string name);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr sel_registerName(string name);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend_fpret")]
    private static extern double objc_msgSend_fpret(IntPtr receiver, IntPtr selector);

    // APIs CoreGraphics pour l'affichage
    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGMainDisplayID();

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern IntPtr CGDisplayCopyDisplayMode(IntPtr displayId);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern int CGDisplayModeGetPixelWidth(IntPtr mode);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern int CGDisplayModeGetWidth(IntPtr mode);

    [DllImport("/System/Library/Frameworks/CoreGraphics.framework/CoreGraphics")]
    private static extern void CGDisplayModeRelease(IntPtr mode);

    #endregion
}