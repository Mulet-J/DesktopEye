using System;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.ViewModels;
using Point = Avalonia.Point;

namespace DesktopEye.Common.Views.ScreenCapture;

public partial class ScreenCaptureView : UserControl
{
    private Rectangle? _selectionRectangle;
    private Point _startPoint;

    public ScreenCaptureView()
    {
        InitializeComponent();
        KeyBindings.Add(new KeyBinding
        {
            Command = new RelayCommand(ConfirmSelection),
            Gesture = new KeyGesture(Key.Enter)
        });
        ImageControl.PointerPressed += OnPointerPressed;
        ImageControl.PointerMoved += OnPointerMoved;
        ImageControl.PointerReleased += OnPointerReleased;
    }

    private void ConfirmSelection()
    {
        if (DataContext is ScreenCaptureViewModel viewmodel) viewmodel.ProcessSelectionCommand.Execute(this);
    }

    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _startPoint = e.GetPosition(ImageControl);
        _selectionRectangle = new Rectangle
        {
            Stroke = Brushes.Red,
            StrokeThickness = 2,
            Fill = Brushes.Transparent
        };
        SelectionCanvas.Children.Add(_selectionRectangle);

        // Set the initial position of the rectangle
        Canvas.SetLeft(_selectionRectangle, _startPoint.X);
        Canvas.SetTop(_selectionRectangle, _startPoint.Y);
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_selectionRectangle != null)
            try
            {
                // Get the current mouse position
                var currentPoint = e.GetPosition(ImageControl);

                // Calculate the top-left corner of the rectangle
                var x = Math.Min(_startPoint.X, currentPoint.X);
                var y = Math.Min(_startPoint.Y, currentPoint.Y);

                // Calculate the width and height of the rectangle
                var width = Math.Abs(currentPoint.X - _startPoint.X);
                var height = Math.Abs(currentPoint.Y - _startPoint.Y);

                // Update the rectangle's position and size
                Canvas.SetLeft(_selectionRectangle, x);
                Canvas.SetTop(_selectionRectangle, y);
                _selectionRectangle.Width = width;
                _selectionRectangle.Height = height;
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        //Inutile ?
        if (_selectionRectangle != null) SelectionCanvas.Children.Remove(_selectionRectangle);
    }
}