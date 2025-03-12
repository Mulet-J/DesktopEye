using Avalonia.Controls;
using System;

namespace DesktopEye.Services;

public class TrayIconManager : IDisposable
{
    private TrayIcon? _trayIcon;
    private readonly Window _window;

    public TrayIconManager(Window window)
    {
        _window = window;
            
        InitializeTrayIcon();
    }

    private void InitializeTrayIcon()
    {
        try
        {
            var menu = new NativeMenu();
                
            var showItem = new NativeMenuItem("Afficher");
            showItem.Click += (s, e) => ShowApplication();
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
                
            _trayIcon.Clicked += (s, e) => ShowApplication();
                
            _trayIcon.IsVisible = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de la création de l'icône dans la zone de notification : {ex.Message}");
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

    public void Dispose()
    {
        if (_trayIcon == null) return;
        _trayIcon.IsVisible = false;
        _trayIcon.Dispose();
        _trayIcon = null;
    }

    private void ExitApp(object? sender, EventArgs e)
    {
        _trayIcon!.IsVisible = false;
        Environment.Exit(0);
    }
}