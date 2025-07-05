using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DesktopEye.Common.Application.Views.ScreenCapture;

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