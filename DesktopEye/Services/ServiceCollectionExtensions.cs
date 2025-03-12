using System;
using System.Runtime.InteropServices;
using DesktopEye.Services.ScreenCaptureService;
using DesktopEye.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Services;

public static class ServiceCollectionExtensions
{
    // https://docs.avaloniaui.net/docs/guides/implementation-guides/how-to-implement-dependency-injection
    public static void AddCommonServices(this IServiceCollection collection)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            collection.AddSingleton<IScreenCaptureService, WindowsScreenCaptureService>();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            collection.AddSingleton<IScreenCaptureService, LinuxScreenCaptureService>();
        }
        else
        {
            throw new PlatformNotSupportedException();
        }
        collection.AddTransient<ScreenCaptureViewModel>();
        collection.AddTransient<LauncherViewModel>();
        collection.AddTransient<SettingsViewModel>();
    }
}