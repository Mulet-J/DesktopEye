using System;
using System.Runtime.InteropServices;
using DesktopEye.Services.OCRService;
using DesktopEye.Services.ScreenCaptureService;
using DesktopEye.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Services.Core;

public static class ServiceCollectionExtensions
{
    // https://docs.avaloniaui.net/docs/guides/implementation-guides/how-to-implement-dependency-injection
    public static void AddCommonServices(this IServiceCollection collection)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            collection.AddSingleton<IScreenCaptureService, WindowsScreenCaptureService>();
        // collection.AddSingleton<IRepository, Repository>();
        // collection.AddTransient<BusinessService>();
        // collection.AddTransient<MainViewModel>();
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            collection.AddSingleton<IScreenCaptureService, LinuxScreenCaptureService>();
        // collection.AddSingleton<IRepository, Repository>();
        // collection.AddTransient<BusinessService>();
        // collection.AddTransient<MainViewModel>();
        else
            throw new PlatformNotSupportedException();

        collection.AddSingleton<IOcrService, TesseractOcrService>();

        collection.AddTransient<ImageViewModel>();
        collection.AddTransient<MainViewModel>();
        collection.AddTransient<SettingsViewModel>();
        collection.AddTransient<InteractionViewModel>();
    }
}