using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace DesktopEye.Common.Views.ScreenCapture;

public partial class ScreenCaptureActionsWindow : Window
{
    public ScreenCaptureActionsWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}