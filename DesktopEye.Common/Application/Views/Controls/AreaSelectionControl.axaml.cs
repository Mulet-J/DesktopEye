using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;

namespace DesktopEye.Common.Application.Views.Controls;

// TODO: Ajouter une toolbar avec les actions possibles sous la zone de sélection
// TODO?: Ajouter un encadré d'aide à l'écran
public class AreaSelectionControl : UserControl
{
    private const int HandleSize = 10;

    public static readonly StyledProperty<Rect> SelectionRectProperty =
        AvaloniaProperty.Register<AreaSelectionControl, Rect>(nameof(SelectionRect));

    private int _dragHandleIndex = -1;
    private bool _isMovingArea;
    private bool _isMovingHandle;
    private bool _isSelectingArea;
    private Point _moveStartPoint;
    private Point _position;
    private Rect _selectionRect;
    private Point _startPoint;

    public AreaSelectionControl()
    {
        AvaloniaXamlLoader.Load(this);
        // Does not work at all without the following line
        Background = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0), 100.0);
        _selectionRect = new Rect(0, 0, 0, 0);
    }

    public Rect SelectionRect => GetValue(SelectionRectProperty);

    private string SelectionDimensions => $"{(int)_selectionRect.Width}×{(int)_selectionRect.Height}";

    //TODO modify to adapt to users's screen
    public double ScaleFactor => 1.0;

    /*private double GetScaleFactor()
    {
        if (TopLevel.GetTopLevel(this) is { } topLevel)
        {
            var screen = topLevel.Screens.ScreenFromVisual(this);
            if (screen != null)
            {
                return screen.Scaling;
            }
        }
        return 1.0; // Valeur par défaut si l'écran n'est pas détectable
    }*/
    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        // Check if we're on a handle
        if (_dragHandleIndex >= 0)
        {
            _isSelectingArea = false;
            _isMovingArea = false;
            _isMovingHandle = true;
            _startPoint = _position;
            e.Handled = true;
            return;
        }

        // Check if we're inside the selection area
        if (_selectionRect.Contains(_position))
        {
            _isMovingArea = true;
            _isSelectingArea = false;
            _isMovingHandle = false;
            _moveStartPoint = _position;
            e.Handled = true;
            return;
        }

        // Start new selection
        _isSelectingArea = true;
        _isMovingArea = false;
        _isMovingHandle = false;
        _startPoint = _position;
        _selectionRect = new Rect(_position, new Size(0, 0));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        _position = e.GetPosition(this);

        if (_isMovingHandle && _dragHandleIndex >= 0)
        {
            // Resize using handles
            UpdateSelectionByHandle(_dragHandleIndex, _position);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_isSelectingArea)
        {
            // Create new selection
            var left = Math.Min(_startPoint.X, _position.X);
            var top = Math.Min(_startPoint.Y, _position.Y);
            var width = Math.Abs(_position.X - _startPoint.X);
            var height = Math.Abs(_position.Y - _startPoint.Y);

            _selectionRect = new Rect(left, top, width, height);
            InvalidateVisual();
            e.Handled = true;
            return;
        }

        if (_isMovingArea)
        {
            // Move existing selection
            var deltaX = _position.X - _moveStartPoint.X;
            var deltaY = _position.Y - _moveStartPoint.Y;

            // Check out of bounds
            // TODO: Améliorer le retour utilisateur quand la sélection est bloqué à un bord de l'écran
            var newX = _selectionRect.X + deltaX;
            var newY = _selectionRect.Y + deltaY;

            var rectX = Math.Clamp(newX, 0, Width - _selectionRect.Width);
            var rectY = Math.Clamp(newY, 0, Height - _selectionRect.Height);

            // Create a new selection rectangle with the updated _position
            _selectionRect = new Rect(
                rectX,
                rectY,
                _selectionRect.Width,
                _selectionRect.Height
            );

            // Update the start point for the next move
            _moveStartPoint = _position;

            InvalidateVisual();
            e.Handled = true;
            return;
        }

        _dragHandleIndex = GetHandleAtPosition(_position);

        if (_dragHandleIndex >= 0)
            Cursor = _dragHandleIndex switch
            {
                0 => new Cursor(StandardCursorType.TopLeftCorner),
                1 => new Cursor(StandardCursorType.TopSide),
                2 => new Cursor(StandardCursorType.TopRightCorner),
                3 => new Cursor(StandardCursorType.LeftSide),
                4 => new Cursor(StandardCursorType.RightSide),
                5 => new Cursor(StandardCursorType.BottomLeftCorner),
                6 => new Cursor(StandardCursorType.BottomSide),
                7 => new Cursor(StandardCursorType.BottomRightCorner),
                _ => Cursor
            };
        else if (!_isSelectingArea && _selectionRect.Contains(_position))
            Cursor = new Cursor(StandardCursorType.DragMove);
        else
            Cursor = new Cursor(StandardCursorType.Cross);
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isSelectingArea = false;
        _isMovingArea = false;
        _isMovingHandle = false;
        _dragHandleIndex = -1;
        e.Handled = true;
    }

    private int GetHandleAtPosition(Point position)
    {
        // Check all 8 handles (corners and midpoints)
        var handles = GetHandlePositions();

        for (var i = 0; i < handles.Length; i++)
            if (new Rect(handles[i].X - HandleSize / 2d, handles[i].Y - HandleSize / 2d,
                    HandleSize, HandleSize).Contains(position))
                return i;

        return -1;
    }

    private Point[] GetHandlePositions()
    {
        // Order: top-left, top-center, top-right, middle-left, middle-right, bottom-left, bottom-center, bottom-right
        return new[]
        {
            new Point(_selectionRect.Left, _selectionRect.Top),
            new Point(_selectionRect.Left + _selectionRect.Width / 2, _selectionRect.Top),
            new Point(_selectionRect.Right, _selectionRect.Top),
            new Point(_selectionRect.Left, _selectionRect.Top + _selectionRect.Height / 2),
            new Point(_selectionRect.Right, _selectionRect.Top + _selectionRect.Height / 2),
            new Point(_selectionRect.Left, _selectionRect.Bottom),
            new Point(_selectionRect.Left + _selectionRect.Width / 2, _selectionRect.Bottom),
            new Point(_selectionRect.Right, _selectionRect.Bottom)
        };
    }

    private void UpdateSelectionByHandle(int handleIndex, Point position)
    {
        var left = _selectionRect.Left;
        var top = _selectionRect.Top;
        var right = _selectionRect.Right;
        var bottom = _selectionRect.Bottom;

        switch (handleIndex)
        {
            case 0: // top-left
                left = position.X;
                top = position.Y;
                break;
            case 1: // top-center
                top = position.Y;
                break;
            case 2: // top-right
                right = position.X;
                top = position.Y;
                break;
            case 3: // middle-left
                left = position.X;
                break;
            case 4: // middle-right
                right = position.X;
                break;
            case 5: // bottom-left
                left = position.X;
                bottom = position.Y;
                break;
            case 6: // bottom-center
                bottom = position.Y;
                break;
            case 7: // bottom-right
                right = position.X;
                bottom = position.Y;
                break;
        }

        _selectionRect = new Rect(
            Math.Min(left, right),
            Math.Min(top, bottom),
            Math.Abs(right - left),
            Math.Abs(bottom - top)
        );
    }

    public override void Render(DrawingContext context)
    {
        SetValue(SelectionRectProperty, _selectionRect * ScaleFactor);
        var brush = new SolidColorBrush(Color.FromArgb(100, 30, 30, 30), 60);
        if (_selectionRect is { Width: > 0, Height: > 0 })
        {
            context.FillRectangle(brush,
                new Rect(0, 0, Bounds.Width, _selectionRect.Top));

            // Left rectangle
            context.FillRectangle(brush,
                new Rect(0, _selectionRect.Top, _selectionRect.Left, _selectionRect.Height));

            // Right rectangle
            context.FillRectangle(brush,
                new Rect(_selectionRect.Right, _selectionRect.Top,
                    Bounds.Width - _selectionRect.Right, _selectionRect.Height));

            // Bottom rectangle
            context.FillRectangle(brush,
                new Rect(0, _selectionRect.Bottom,
                    Bounds.Width, Bounds.Height - _selectionRect.Bottom));

            // Draw selection rectangle border
            context.DrawRectangle(new Pen(Brushes.LightBlue), _selectionRect);

            // Draw dimension text
            var text = new FormattedText(
                SelectionDimensions,
                new CultureInfo("en-us"),
                FlowDirection.LeftToRight,
                new Typeface("Arial"),
                14.0,
                Brushes.White
            );

            // Position the dimension text above the selection
            var textBackground = new Rect(
                _selectionRect.Left + (_selectionRect.Width - text.Width) / 2 - 4,
                _selectionRect.Top - text.Height - 8,
                text.Width + 8,
                text.Height + 6
            );

            context.FillRectangle(new SolidColorBrush(Color.FromRgb(50, 50, 50)), textBackground);
            context.DrawText(text, new Point(textBackground.X + 4, textBackground.Y + 3));

            // Draw handle points
            var handles = GetHandlePositions();
            foreach (var handle in handles)
                context.FillRectangle(Brushes.LightBlue,
                    new Rect(handle.X - HandleSize / 2d, handle.Y - HandleSize / 2d, HandleSize, HandleSize));
        }
        else
        {
            context.FillRectangle(brush, new Rect(0, 0, Bounds.Width, Bounds.Height));
        }
    }
}