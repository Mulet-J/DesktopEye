using System.ComponentModel;
using Avalonia.Controls;

namespace DesktopEye.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        // Closing += WindowClosing;
    }

    private void WindowClosing(object? sender, CancelEventArgs e)
    {
        e.Cancel = true;
        Hide();
    }
}