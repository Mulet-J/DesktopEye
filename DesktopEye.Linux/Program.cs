using System;
using Avalonia;
using DesktopEye.Linux.Services;
using DesktopEye.Services.Core;
using DesktopEye.Services.ScreenCaptureService;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Linux;

internal static class Program
{
    private static IServiceCollection AddPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IScreenCaptureService, LinuxScreenCaptureService>();
        return services;
    }

    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Dependency Injection
        var services = new ServiceCollection();
        services.AddCommonServices();
        services.AddPlatformServices();

        var serviceProvider = services.BuildServiceProvider();

        BuildAvaloniaApp(serviceProvider)
            .StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    private static AppBuilder BuildAvaloniaApp(IServiceProvider serviceProvider)
    {
        return AppBuilder.Configure(() => new App(serviceProvider))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}