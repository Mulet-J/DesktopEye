using System;
using System.Globalization;
using System.Linq;
using System.Resources;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using DesktopEye.Services;
using DesktopEye.Services.ScreenCaptureService;
using DesktopEye.ViewModels;
using DesktopEye.Views;

namespace DesktopEye;

public partial class App : Application
{
    // private Window _mainWindow;
    private TrayIcon _trayIcon;
    public static IServiceProvider Services { get; protected set; }

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

            // Dependency injection
            ServiceCollection collection = new();
            collection.AddCommonServices();

            collection.AddTransient<ImageViewModel>();

            Services = collection.BuildServiceProvider();

            // var serviceProvider = collection.BuildServiceProvider();

            // _mainWindow = new SettingsWindow
            // {
            //     DataContext = new SettingsViewModel()
            // };
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
        var resourceManager = new ResourceManager("DesktopEye.Resources.Strings", typeof(MainWindow).Assembly);
        _trayIcon = new TrayIcon
        {
            Icon = new WindowIcon(AssetLoader.Open(new Uri("avares://DesktopEye/Assets/avalonia-logo.ico"))),
            ToolTipText = "DesktopEye",
            IsVisible = true
        };

        var menu = new NativeMenu();

        var gcMenuItem = new NativeMenuItem("GC");
        gcMenuItem.Click += GarbageCollect;

        var settingsString = resourceManager.GetString("Tray.Settings", CultureInfo.CurrentUICulture);
        var settingsMenuItem = new NativeMenuItem(settingsString ?? "Settings");
        // settingsMenuItem.Click += ShowMainWindow;

        var exitString = resourceManager.GetString("Tray.Exit", CultureInfo.CurrentUICulture);
        var exitMenuItem = new NativeMenuItem(exitString ?? "Exit");
        exitMenuItem.Click += ExitApp;

        menu.Add(gcMenuItem);
        menu.Add(settingsMenuItem);
        menu.Add(exitMenuItem);

        _trayIcon.Menu = menu;
        _trayIcon.Clicked += TriggerCapture;
    }

    // private void ShowMainWindow(object? sender, EventArgs e)
    // {
    //     if (_mainWindow != null)
    //     {
    //         _mainWindow.Show();
    //         _mainWindow.Activate();
    //     }
    // }

    private void ExitApp(object? sender, EventArgs e)
    {
        _trayIcon.IsVisible = false;
        Environment.Exit(0);
    }

    private void TriggerCapture(object? sender, EventArgs e)
    {
        var bitmap = Services.GetService<IScreenCaptureService>()?.CaptureScreen();
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