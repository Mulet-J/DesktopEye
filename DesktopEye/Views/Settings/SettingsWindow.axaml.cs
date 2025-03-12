using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DesktopEye.Views.Settings;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
        
        // Positionner au centre de l'écran
        this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}