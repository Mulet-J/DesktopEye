using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace DesktopEye.Views;

public partial class ScreenCaptureWindow : Window
{
    public ScreenCaptureWindow()
    {
        InitializeComponent();
        Opened += SpanAcrossAllScreens;
        KeyDown += OnKeyDown;
        LostFocus += OnLostFocus;
    }
    
    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape) Close();
    }


    private void OnLostFocus(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void SpanAcrossAllScreens(object? sender, EventArgs e)
    {
        var screens = Screens.All;

        // Calculate the combined bounds in PHYSICAL PIXELS (PixelRect)
        var unitedPixelRect = new PixelRect();
        foreach (var screen in screens) unitedPixelRect = unitedPixelRect.Union(screen.Bounds);

        // Get the primary screen's scaling factor (for unit conversion)
        var primaryScreen = Screens.Primary ?? screens.First();
        var scaling = primaryScreen.Scaling;

        // Convert the united PixelRect to a Rect in DEVICE-INDEPENDENT UNITS
        var unitedRect = new Rect(
            unitedPixelRect.X / scaling,
            unitedPixelRect.Y / scaling,
            unitedPixelRect.Width / scaling,
            unitedPixelRect.Height / scaling
        );

        // Configure the window
        WindowState = WindowState.Normal;
        CanResize = false;
        SystemDecorations = SystemDecorations.None;
        // Topmost = true;

        // Set position (in physical pixels) and size (in device-independent units)
        Position = new PixelPoint(unitedPixelRect.X, unitedPixelRect.Y);
        Width = unitedRect.Width;
        Height = unitedRect.Height;
    }
}