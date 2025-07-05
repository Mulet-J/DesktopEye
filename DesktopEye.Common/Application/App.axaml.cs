using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using DesktopEye.Common.Application.Views;
using DesktopEye.Common.Application.Views.ScreenCapture;
using DesktopEye.Common.Domain.Features.OpticalCharacterRecognition.Interfaces;
using DesktopEye.Common.Domain.Features.TextClassification.Interfaces;
using DesktopEye.Common.Domain.Features.TextTranslation.Interfaces;
using DesktopEye.Common.Infrastructure.Services.Core;
using DesktopEye.Common.Resources;
using Microsoft.Extensions.DependencyInjection;
using MainViewModel = DesktopEye.Common.Application.ViewModels.MainViewModel;
using ScreenCaptureViewModel = DesktopEye.Common.Application.ViewModels.ScreenCapture.ScreenCaptureViewModel;

namespace DesktopEye.Common.Application;

public class App : Avalonia.Application
{
    private readonly IServiceProvider _services;
    private Window? _mainWindow;
    private TrayIcon? _trayIcon;

    public App(IServiceProvider services)
    {
        _services = services;
        PreloadServices();
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void PreloadServices()
    {
        // Preload services, obviously only pass singletons here
        var preloader = _services.GetRequiredService<ServicesPreloader>();
        preloader.PreloadServices(typeof(IOcrManager), typeof(ITextClassifierManager), typeof(ITranslationManager));
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            _mainWindow = new MainWindow
            {
                DataContext = _services.GetService<MainViewModel>()
            };
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            InitializeTrayIcon();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();
        foreach (var plugin in dataValidationPluginsToRemove) BindingPlugins.DataValidators.Remove(plugin);
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://DesktopEye.Common/Application/Assets/avalonia-logo.ico"))),
            ToolTipText = "DesktopEye.Common",
            IsVisible = true
        };

        var menu = new NativeMenu();

        var mainWindowString = ResourcesTray.OpenMainWindow;
        var mainWindowMenuItem = new NativeMenuItem(mainWindowString);
        mainWindowMenuItem.Click += ShowMainWindow;

        var gcMenuItem = new NativeMenuItem("GC");
        gcMenuItem.Click += GarbageCollect;

        var settingsString = ResourcesTray.Settings;
        var settingsMenuItem = new NativeMenuItem(settingsString);

        var exitString = ResourcesTray.Exit;
        var exitMenuItem = new NativeMenuItem(exitString);
        exitMenuItem.Click += ExitApp;

        var triggerItem = new NativeMenuItem("Screen Capture");
        triggerItem.Click += TriggerCapture;

        menu.Add(mainWindowMenuItem);
        menu.Add(gcMenuItem);
        menu.Add(settingsMenuItem);
        menu.Add(exitMenuItem);
        menu.Add(triggerItem);

        _trayIcon.Menu = menu;
        _trayIcon.Clicked += TriggerCapture;
    }

    private void ShowMainWindow(object? sender, EventArgs e)
    {
        if (_mainWindow == null) return;
        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void ExitApp(object? sender, EventArgs e)
    {
        _trayIcon!.IsVisible = false;
        Environment.Exit(0);
    }

    private void TriggerCapture(object? sender, EventArgs e)
    {
        var fullScreenWindow = new ScreenCaptureWindow
        {
            DataContext = _services.GetService<ScreenCaptureViewModel>()
        };
        fullScreenWindow.Show();
    }

    private static void GarbageCollect(object? sender, EventArgs e)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}