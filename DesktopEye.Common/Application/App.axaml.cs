using System;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using DesktopEye.Common.Application.Resources;
using DesktopEye.Common.Application.ViewModels.Setup;
using DesktopEye.Common.Application.Views;
using DesktopEye.Common.Application.Views.ScreenCapture;
using DesktopEye.Common.Application.Views.Setup;
using DesktopEye.Common.Domain.Features.OpticalCharacterRecognition.Interfaces;
using DesktopEye.Common.Domain.Features.TextClassification.Interfaces;
using DesktopEye.Common.Domain.Features.TextToSpeech.Interfaces;
using DesktopEye.Common.Domain.Features.TextTranslation.Interfaces;
using DesktopEye.Common.Infrastructure.Configuration;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using MainViewModel = DesktopEye.Common.Application.ViewModels.MainViewModel;
using ScreenCaptureViewModel = DesktopEye.Common.Application.ViewModels.ScreenCapture.ScreenCaptureViewModel;

namespace DesktopEye.Common.Application;

public class App : Avalonia.Application
{
    public static IServiceProvider Services = null!;
    private Window? _mainWindow;
    private TrayIcon? _trayIcon;

    public App(IServiceProvider services)
    {
        Services = services;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void PreloadServices()
    {
        // Preload services, obviously only pass singletons here
        var preloader = Services.GetRequiredService<ServicesLoader>();
        preloader.PreloadServices(typeof(IOcrOrchestrator), typeof(ITextClassifierOrchestrator),
            typeof(ITranslationOrchestrator), typeof(ITtsOrchestrator));
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();
            _mainWindow = new MainWindow
            {
                DataContext = Services.GetService<MainViewModel>()
            };
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var config = Services.GetRequiredService<IAppConfigService>();

            if (!config.IsSetupFinished())
            {
                // Setup not finished - show setup window as main window
                var setupViewModel = Services.GetRequiredService<SetupViewModel>();
                var setupWindow = new SetupWindow
                {
                    DataContext = setupViewModel
                };

                // Subscribe to setup completion event
                setupViewModel.SetupCompleted += OnSetupCompleted;

                desktop.MainWindow = setupWindow;
                setupWindow.Show();
            }
            else
            {
                // Setup already finished - show main window and preload services
                ShowMainWindowAndPreload(desktop);
            }

            InitializeTrayIcon();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void OnSetupCompleted(object? sender, EventArgs e)
    {
        // Setup is now complete, switch to main window
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Close setup window
            desktop.MainWindow?.Close();

            // Show main window and preload services
            ShowMainWindowAndPreload(desktop);
        }
    }

    private void ShowMainWindowAndPreload(IClassicDesktopStyleApplicationLifetime desktop)
    {
        _mainWindow = new MainWindow
        {
            DataContext = Services.GetService<MainViewModel>()
        };

        desktop.MainWindow = _mainWindow;
        _mainWindow.Show();

        // Now preload services
        PreloadServices();
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
            Icon = new WindowIcon(
                AssetLoader.Open(new Uri("avares://DesktopEye.Common/Application/Assets/avalonia-logo.ico"))),
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
            DataContext = Services.GetService<ScreenCaptureViewModel>()
        };
        fullScreenWindow.Show();
    }

    private static void GarbageCollect(object? sender, EventArgs e)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}