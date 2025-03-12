using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using DesktopEye.ViewModels;

namespace DesktopEye.Views;

public partial class ScreenCaptureView : UserControl
{
    private Rectangle? _selectionRectangle;
    private Canvas? _overlayCanvas;
    private Point _startPoint;
    private Panel? _toolPanel;
    private StackPanel? _bottomToolPanel;
    private Image? _screenshotImage;
    private bool _isSelecting;

    public ScreenCaptureView()
    {
        InitializeComponent();
        
        this.Loaded += OnControlLoaded;
    }
    
    private void OnControlLoaded(object? sender, RoutedEventArgs e)
    {
        // Initialize UI components
        _overlayCanvas = this.FindControl<Canvas>("OverlayCanvas");
        _toolPanel = this.FindControl<Panel>("ToolPanel");
        _bottomToolPanel = this.FindControl<StackPanel>("BottomToolPanel");
        _screenshotImage = this.FindControl<Image>("ScreenshotImage");
        
        if (_overlayCanvas == null || _toolPanel == null || 
            _bottomToolPanel == null || _screenshotImage == null)
        {
            return; // Error state - controls not found
        }
        
        // Set up event handlers
        this.PointerPressed += OnPointerPressed;
        this.PointerMoved += OnPointerMoved;
        this.PointerReleased += OnPointerReleased;
        
        // Initialize ViewModel if needed
        if (DataContext is ScreenCaptureViewModel viewModel)
        {
            // Set up view-specific bindings
            _screenshotImage.Source = viewModel.Bitmap;
            
            // Listen for property changes
            viewModel.PropertyChanged += (sender, args) =>
            {
                if (args.PropertyName == nameof(ScreenCaptureViewModel.Bitmap))
                {
                    _screenshotImage.Source = viewModel.Bitmap;
                }
            };
            
            // Initialize the view model
            viewModel.Initialize();
        }
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_overlayCanvas == null) return;
        
        if (!_isSelecting && e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            _isSelecting = true;
            _startPoint = e.GetPosition(_overlayCanvas);

            // Create selection rectangle
            _selectionRectangle = new Rectangle
            {
                Stroke = new SolidColorBrush(Color.Parse("#304FFE")),
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.Parse("#15304FFE"))
            };

            Canvas.SetLeft(_selectionRectangle, _startPoint.X);
            Canvas.SetTop(_selectionRectangle, _startPoint.Y);
            _selectionRectangle.Width = 0;
            _selectionRectangle.Height = 0;
            _overlayCanvas.Children.Add(_selectionRectangle);
        }
    }
    
    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_overlayCanvas == null) return;
        
        // Selection rectangle logic (moved from Window)
        if (_isSelecting && _selectionRectangle != null)
        {
            var currentPoint = e.GetPosition(_overlayCanvas);

            // Calculate dimensions of selection rectangle
            double left = Math.Min(_startPoint.X, currentPoint.X);
            double top = Math.Min(_startPoint.Y, currentPoint.Y);
            double width = Math.Abs(currentPoint.X - _startPoint.X);
            double height = Math.Abs(currentPoint.Y - _startPoint.Y);

            // Update rectangle
            Canvas.SetLeft(_selectionRectangle, left);
            Canvas.SetTop(_selectionRectangle, top);
            _selectionRectangle.Width = width;
            _selectionRectangle.Height = height;
            
            // Show dimensions
            UpdateSizeDisplay(left, top, width, height);
        }
    }
    
    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_overlayCanvas == null) return;
        
        // Selection completion logic (moved from Window)
        if (_isSelecting && _selectionRectangle != null)
        {
            _isSelecting = false;
            
            // Check if selection is too small
            if (_selectionRectangle.Width < 10 || _selectionRectangle.Height < 10)
            {
                // Reset selection if too small
                _overlayCanvas.Children.Remove(_selectionRectangle);
                _selectionRectangle = null;
                return;
            }
            
            // Position tool panels
            PositionToolPanels();
            
            // Update ViewModel with selection coordinates
            if (DataContext is ScreenCaptureViewModel viewModel)
            {
                double left = Canvas.GetLeft(_selectionRectangle);
                double top = Canvas.GetTop(_selectionRectangle);
                
                viewModel.SelectionX = (int)left;
                viewModel.SelectionY = (int)top;
                viewModel.SelectionWidth = (int)_selectionRectangle.Width;
                viewModel.SelectionHeight = (int)_selectionRectangle.Height;
            }
        }
    }
    
    private void UpdateSizeDisplay(double left, double top, double width, double height)
    {
        if (_overlayCanvas == null) return;
        
        // Size display logic (moved from Window)
        var dimensionText = $"{(int)width} × {(int)height}";
        
        var sizeDisplay = _overlayCanvas.Children.OfType<TextBlock>().FirstOrDefault();
        
        if (sizeDisplay == null)
        {
            sizeDisplay = new TextBlock
            {
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.Parse("#80000000")),
                Padding = new Thickness(5),
                FontSize = 12
            };
            _overlayCanvas.Children.Add(sizeDisplay);
        }
        
        sizeDisplay.Text = dimensionText;
        
        // Position above selection rectangle
        Canvas.SetLeft(sizeDisplay, left);
        Canvas.SetTop(sizeDisplay, top - 25);
    }
    
    private void PositionToolPanels()
    {
        // Tool panel positioning logic (moved from Window)
        if (_selectionRectangle == null || _toolPanel == null || _bottomToolPanel == null) return;
        
        double left = Canvas.GetLeft(_selectionRectangle);
        double top = Canvas.GetTop(_selectionRectangle);
        double width = _selectionRectangle.Width;
        double height = _selectionRectangle.Height;
        
        // Position tool panel to right of selection
        Canvas.SetLeft(_toolPanel, left + width + 20);
        Canvas.SetTop(_toolPanel, top + (height / 2) - (_toolPanel.Bounds.Height / 2));
        
        // Position bottom tool panel below selection
        Canvas.SetLeft(_bottomToolPanel, left + (width / 2) - (_bottomToolPanel.Bounds.Width / 2));
        Canvas.SetTop(_bottomToolPanel, top + height + 20);
        
        // Make panels visible
        _toolPanel.IsVisible = true;
        _bottomToolPanel.IsVisible = true;
    }
    
    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}