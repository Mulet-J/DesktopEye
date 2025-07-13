using System;
using Avalonia.Controls;

namespace DesktopEye.Common.Application;

// TODO mettre le code de app.axaml.cs ici
public class TrayIconManager : IDisposable
{
    private readonly Window _window;
    private TrayIcon? _trayIcon;

    public TrayIconManager(Window window)
    {
        _window = window;

        InitializeTrayIcon();
    }

    public void Dispose()
    {
        if (_trayIcon == null) return;
        _trayIcon.IsVisible = false;
        _trayIcon.Dispose();
        _trayIcon = null;
    }

    private void InitializeTrayIcon()
    {
        try
        {
            var menu = new NativeMenu();

            var showItem = new NativeMenuItem("Afficher");
            showItem.Click += (_, _) => ShowApplication();
            menu.Add(showItem);

            var exitItem = new NativeMenuItem("Quitter");
            exitItem.Click += ExitApp;
            menu.Add(exitItem);

            _trayIcon = new TrayIcon
            {
                Icon = _window.Icon,
                ToolTipText = "Desktop Eye",
                Menu = menu
            };

            _trayIcon.Clicked += (_, _) => ShowApplication();

            _trayIcon.IsVisible = true;
        }
        catch (Exception ex)
        {
            ;
        }
    }

    private void ShowApplication()
    {
        _window.WindowState = WindowState.Normal;
        _window.Show();
        _window.Activate();
    }

    public void HideToTray()
    {
        _window.Hide();
    }

    private void ExitApp(object? sender, EventArgs e)
    {
        _trayIcon!.IsVisible = false;
        Environment.Exit(0);
    }
}