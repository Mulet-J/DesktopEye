using System;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Media;
using DesktopEye.ViewModels;
using Point = Avalonia.Point;

namespace DesktopEye.Views;

public partial class ImageView : UserControl
{
    private Rectangle? _selectionRectangle;
    private Point _startPoint;

    public ImageView()
    {
        InitializeComponent();

        ImageControl.PointerPressed += OnPointerPressed;
        ImageControl.PointerMoved += OnPointerMoved;
        ImageControl.PointerReleased += OnPointerReleased;
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
        if (_selectionRectangle != null)
        {
            // Get the final mouse position
            var startPoint = _startPoint;
            var endPoint = e.GetPosition(ImageControl);

            // Do something with the selected area (e.g., crop the image)
            // Console.WriteLine($"Selected area: X={rect.X}, Y={rect.Y}, Width={rect.Width}, Height={rect.Height}");
            Console.WriteLine($"SP: {_startPoint.X}, {_startPoint.Y} EP: {endPoint.X}, {endPoint.Y}");

            if (DataContext is ImageViewModel viewModel)
                viewModel.ProcessSelectionCommand.Execute([startPoint, endPoint]);

            // Remove the rectangle from the canvas
            SelectionCanvas.Children.Remove(_selectionRectangle);
        }
    }
}