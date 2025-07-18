﻿using System;
using Avalonia;
using DesktopEye.Common.Application;
using DesktopEye.Common.Infrastructure.Configuration;
using DesktopEye.Common.Infrastructure.Services.ScreenCapture;
using DesktopEye.Desktop.Windows.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Desktop.Windows;

internal static class Program
{
    private static void AddPlatformServices(this IServiceCollection services)
    {
        services.AddSingleton<IScreenCaptureService, WindowsScreenCaptureService>();
        services.AddSingleton<IScaleFactoringService, WindowsScaleFactoringService>();
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

        return AppBuilder.Configure(() => new App(serviceProvider))
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
    }
}