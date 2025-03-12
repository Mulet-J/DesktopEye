using System;
using System.Globalization;
using System.Linq;
using System.Resources;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using DesktopEye.Services;
using DesktopEye.ViewModels;
using DesktopEye.Views;
using DesktopEye.Views.Launcher;
using DesktopEye.Views.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye;

public class App : Application
{
    private TrayIcon _trayIcon;
    private LauncherWindow? _launcherWindow;
    private static ServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            DisableAvaloniaDataAnnotationValidation();

            ServiceCollection collection = new();
            
            collection.AddCommonServices();

            var serviceProvider = collection.BuildServiceProvider();
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            
            _launcherWindow = new LauncherWindow
            {
                DataContext = serviceProvider.GetRequiredService<LauncherViewModel>()
            };
            desktop.MainWindow = _launcherWindow;
            desktop.MainWindow.Show();
            desktop.MainWindow.Activate();

            InitializeTrayIcon();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        foreach (var plugin in dataValidationPluginsToRemove) BindingPlugins.DataValidators.Remove(plugin);
    }

    private void InitializeTrayIcon()
    {
        var resourceManager = new ResourceManager("DesktopEye.Resources.Strings", typeof(MainWindow).Assembly);
        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://DesktopEye/Assets/avalonia-logo.ico"))),
            ToolTipText = "DesktopEye",
            IsVisible = true
        };

        var menu = new NativeMenu();
        
        var captureMenuItem = new NativeMenuItem("Capture Screen");
        captureMenuItem.Click += TriggerCapture;

        var launcherMenuItem = new NativeMenuItem("Open Launcher");
        launcherMenuItem.Click += ShowLauncher;
        
        var gcMenuItem = new NativeMenuItem("GC");
        gcMenuItem.Click += GarbageCollect;

        var settingsString = resourceManager.GetString("Tray.Settings", CultureInfo.CurrentUICulture);
        var settingsMenuItem = new NativeMenuItem(settingsString ?? "Settings");
        settingsMenuItem.Click += ShowSettings;

        var exitString = resourceManager.GetString("Tray.Exit", CultureInfo.CurrentUICulture);
        var exitMenuItem = new NativeMenuItem(exitString ?? "Exit");
        exitMenuItem.Click += ExitApp;

        menu.Add(captureMenuItem);
        menu.Add(launcherMenuItem);
        menu.Add(new NativeMenuItemSeparator());
        
        menu.Add(gcMenuItem);
        menu.Add(settingsMenuItem);
        menu.Add(new NativeMenuItemSeparator());
        
        menu.Add(exitMenuItem);

        _trayIcon.Menu = menu;
        _trayIcon.Clicked += TriggerCapture;
    }

    private void ShowLauncher(object? sender, EventArgs e)
    {
        if (_launcherWindow != null)
        {
            _launcherWindow.Show();
            _launcherWindow.Activate();
        }
    }
    
    private static void ShowSettings(object? sender, EventArgs e)
    {
        if (_serviceProvider != null)
        {
            var settingsWindow = new SettingsWindow
            {
                DataContext = _serviceProvider.GetRequiredService<SettingsViewModel>()
            };
            settingsWindow.Show();
        }
    }

    private void ExitApp(object? sender, EventArgs e)
    {
        _trayIcon.IsVisible = false;
        Environment.Exit(0);
    }

    private static void TriggerCapture(object? sender, EventArgs e)
    {
        if (_serviceProvider == null) return;
        var viewModel = _serviceProvider.GetRequiredService<ScreenCaptureViewModel>();
        
        var fullScreenWindow = new ScreenCaptureWindow();
        
        if (fullScreenWindow.Content is ScreenCaptureView captureView)
        {
            captureView.DataContext = viewModel;
        }
        
        fullScreenWindow.Show();
    }

    private static void GarbageCollect(object? sender, EventArgs e)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}