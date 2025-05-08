using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using DesktopEye.Services.ScreenCaptureService;
using DesktopEye.ViewModels;
using DesktopEye.Views;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye;

public class App : Application
{
    private readonly IServiceProvider _serviceProvider;
    private Window? _mainWindow;

    // private Window _mainWindow;
    private TrayIcon _trayIcon;


    public App(IServiceProvider serviceProviderProvider)
    {
        _serviceProvider = serviceProviderProvider;
    }

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // var serviceProvider = collection.BuildServiceProvider();

            _mainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            // desktop.MainWindow = _mainWindow;

            InitializeTrayIcon();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove) BindingPlugins.DataValidators.Remove(plugin);
    }

    private void InitializeTrayIcon()
    {
        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://DesktopEye/Assets/avalonia-logo.ico"))),
            ToolTipText = "DesktopEye",
            IsVisible = true
        };

        var menu = new NativeMenu();

        var mainWindowString = DesktopEye.Resources.Resources.Tray_OpenMainWindow;
        var mainWindowMenuItem = new NativeMenuItem(mainWindowString);
        mainWindowMenuItem.Click += ShowMainWindow;

        var gcMenuItem = new NativeMenuItem("GC");
        gcMenuItem.Click += GarbageCollect;

        var settingsString = DesktopEye.Resources.Resources.Tray_Settings;
        var settingsMenuItem = new NativeMenuItem(settingsString ?? "Settings");
        // settingsMenuItem.Click += ShowMainWindow;

        var exitString = DesktopEye.Resources.Resources.Tray_Exit;
        var exitMenuItem = new NativeMenuItem(exitString ?? "Exit");
        exitMenuItem.Click += ExitApp;

        menu.Add(mainWindowMenuItem);
        menu.Add(gcMenuItem);
        menu.Add(settingsMenuItem);
        menu.Add(exitMenuItem);

        _trayIcon.Menu = menu;
        _trayIcon.Clicked += TriggerCapture;
    }

    private void ShowMainWindow(object? sender, EventArgs e)
    {
        if (_mainWindow != null)
        {
            _mainWindow.Show();
            _mainWindow.Activate();
        }
    }

    private void ExitApp(object? sender, EventArgs e)
    {
        _trayIcon.IsVisible = false;
        Environment.Exit(0);
    }

    private void TriggerCapture(object? sender, EventArgs e)
    {
        var bitmap = _serviceProvider.GetService<IScreenCaptureService>()?.CaptureScreen();
        if (bitmap == null) return;
        var fullScreenWindow = new FullScreenWindow
        {
            DataContext = new ImageViewModel(bitmap)
        };
        fullScreenWindow.Show();
    }

    private void GarbageCollect(object? sender, EventArgs e)
    {
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }
}