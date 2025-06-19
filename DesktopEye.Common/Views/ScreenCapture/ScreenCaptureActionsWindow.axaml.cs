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
    
    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed) 
            BeginMoveDrag(e);
    }

    private void MinimizeWindow(object? sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void CloseWindow(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}