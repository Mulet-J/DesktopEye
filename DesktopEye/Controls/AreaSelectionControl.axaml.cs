using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace DesktopEye.Controls;

//Fait en grande partie par Claude, potentiellement éclaté au sol
// TODO: Exclure la zone sélectionnée de l'effet d'atténuation
// TODO: Ajouter une toolbar avec les actions possibles sous la zone de sélection
// TODO?: Ajouter un encadré d'aide à l'écran
public partial class AreaSelectionControl : UserControl
{
    private const int HandleSize = 10;

    public static readonly StyledProperty<Rect> SelectionRectProperty =
        AvaloniaProperty.Register<AreaSelectionControl, Rect>(nameof(SelectionRect));

    private int _dragHandleIndex = -1;
    private bool _isMoving;
    private bool _isSelecting;
    private Point _moveStartPoint;
    private Rect _selectionRect;
    private Point _startPoint;

    public AreaSelectionControl()
    {
        // Background = new SolidColorBrush(Color.FromArgb(100, 30, 30, 30));
        Background = new SolidColorBrush(Color.FromArgb(100, 0, 0, 0), 80.0);
        _selectionRect = new Rect(0, 0, 0, 0);
    }

    public Rect SelectionRect => GetValue(SelectionRectProperty);

    private string SelectionDimensions => $"{(int)_selectionRect.Width}×{(int)_selectionRect.Height}";

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);

        var position = e.GetPosition(this);
        _dragHandleIndex = GetHandleAtPosition(position);

        // Check if we're on a handle
        if (_dragHandleIndex >= 0)
        {
            _isSelecting = true;
            _isMoving = false;
            _startPoint = position;
            e.Handled = true;
            return;
        }

        // Check if we're inside the selection area
        if (_selectionRect.Contains(position))
        {
            _isMoving = true;
            _isSelecting = false;
            _moveStartPoint = position;
            e.Handled = true;
            return;
        }

        // Start new selection
        _isSelecting = true;
        _isMoving = false;
        _startPoint = position;
        _selectionRect = new Rect(position, new Size(0, 0));
        e.Handled = true;
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        var position = e.GetPosition(this);

        if (_isSelecting && _dragHandleIndex >= 0)
        {
            // Resize using handles
            UpdateSelectionByHandle(_dragHandleIndex, position);
            InvalidateVisual();
            e.Handled = true;
        }
        else if (_isSelecting)
        {
            // Create new selection
            var left = Math.Min(_startPoint.X, position.X);
            var top = Math.Min(_startPoint.Y, position.Y);
            var width = Math.Abs(position.X - _startPoint.X);
            var height = Math.Abs(position.Y - _startPoint.Y);

            _selectionRect = new Rect(left, top, width, height);
            InvalidateVisual();
            e.Handled = true;
        }
        else if (_isMoving)
        {
            // Move existing selection
            var deltaX = position.X - _moveStartPoint.X;
            var deltaY = position.Y - _moveStartPoint.Y;

            // Check out of bounds
            // TODO: Améliorer le retour utilisateur quand la sélection est bloqué à un bord de l'écran
            var newX = _selectionRect.X + deltaX;
            var newY = _selectionRect.Y + deltaY;

            var rectX = Math.Clamp(newX, 0, Width - _selectionRect.Width);
            var rectY = Math.Clamp(newY, 0, Height - _selectionRect.Height);

            // Create a new selection rectangle with the updated position
            _selectionRect = new Rect(
                rectX,
                rectY,
                _selectionRect.Width,
                _selectionRect.Height
            );

            // Update the start point for the next move
            _moveStartPoint = position;

            InvalidateVisual();
            e.Handled = true;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        _isSelecting = false;
        _isMoving = false;
        _dragHandleIndex = -1;
        e.Handled = true;
    }

    private int GetHandleAtPosition(Point position)
    {
        // Check all 8 handles (corners and midpoints)
        var handles = GetHandlePositions();

        for (var i = 0; i < handles.Length; i++)
            if (new Rect(handles[i].X - HandleSize / 2, handles[i].Y - HandleSize / 2,
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
        if (_selectionRect.Width > 0 && _selectionRect.Height > 0)
        {
            context.FillRectangle(Background,
                new Rect(0, 0, Bounds.Width, _selectionRect.Top));

            // Left rectangle
            context.FillRectangle(Background,
                new Rect(0, _selectionRect.Top, _selectionRect.Left, _selectionRect.Height));

            // Right rectangle
            context.FillRectangle(Background,
                new Rect(_selectionRect.Right, _selectionRect.Top,
                    Bounds.Width - _selectionRect.Right, _selectionRect.Height));

            // Bottom rectangle
            context.FillRectangle(Background,
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
                    new Rect(handle.X - HandleSize / 2, handle.Y - HandleSize / 2, HandleSize, HandleSize));
        }
        else
        {
            context.FillRectangle(Background, new Rect(0, 0, Bounds.Width, Bounds.Height));
        }
    }
}