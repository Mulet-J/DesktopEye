using System;
using System.IO;
using Avalonia;
using DesktopEye.Common.Application;
using DesktopEye.Common.Infrastructure.Configuration;
using DesktopEye.Common.Infrastructure.Services.ScreenCapture;
using DesktopEye.Desktop.MacOS.Services;
using Microsoft.Extensions.DependencyInjection;
using TesseractOCR;
using TesseractOCR.Enums;
using TesseractOCR.InteropDotNet;

namespace DesktopEye.Desktop.MacOS;

internal static class Program
{
    private static void AddPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IScreenCaptureService, MacOsScreenCaptureService>();
        services.AddSingleton<IScaleFactoringService, MacOsScaleFactoringService>();
    }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp()
    {
        // Dependency Injection
        var services = new ServiceCollection();
        services.InjectApplicationServices();
        services.AddPlatformServices();

        var serviceProvider = services.BuildServiceProvider();
        
        // Dirty fix to force tesseract's library loading before others
        try
        {
            var baseDirectory = AppContext.BaseDirectory;
            LibraryLoader.Instance.CustomSearchPath = Path.Combine(baseDirectory, "libs");
            using var engine = new Engine("", Language.English);
        }
        catch (Exception e)
        {
            ;
        }

        return AppBuilder.Configure(() => new App(serviceProvider))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}