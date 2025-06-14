using System;
using Avalonia;
using DesktopEye.Common;
using DesktopEye.Common.Services.Core;
using DesktopEye.Common.Services.OCR;
using DesktopEye.Common.Services.ScreenCapture;
using DesktopEye.Common.Services.TextClassifier;
using DesktopEye.Common.Services.Translation;
using DesktopEye.Desktop.Linux.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Desktop.Linux;

internal static class Program
{
    private static void AddPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IScreenCaptureService, LinuxScreenCaptureService>();
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
        services.AddCommonServices();
        services.AddPlatformServices();

        var serviceProvider = services.BuildServiceProvider();

        // Warm up services
        // TODO find a proper way to force load the models without blocking the thread
        var warmer = serviceProvider.GetService<ServicesWarmer>();
        warmer?
            .AddService<IOcrManager>()
            .AddService<ITextClassifierManager>()
            .AddService<ITranslationManager>();

        return AppBuilder.Configure(() => new App(serviceProvider))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}