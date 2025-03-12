using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DesktopEye.ViewModels;
using DesktopEye.Views.Settings;

namespace DesktopEye.Views.Launcher;

public partial class LauncherWindow : Window
{
    private LauncherView? _launcherView;
    
    public LauncherWindow() 
    {
        InitializeComponent();
        _launcherView = this.FindControl<LauncherView>("LauncherView");
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
    
    private void settings_Click(object sender, RoutedEventArgs e)
    {
        var settingsWindow = new SettingsWindow
        {
            DataContext = new SettingsViewModel()
        };
        settingsWindow.Show();
    }
    
    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
    
    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized 
            ? WindowState.Normal 
            : WindowState.Maximized;
    }
    
    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Hide();
    }
    
    
    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (e.CloseReason == WindowCloseReason.WindowClosing)
        {
            e.Cancel = true; 
            Hide();
        }
        
        base.OnClosing(e);
    }
}