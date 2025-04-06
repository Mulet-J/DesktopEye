using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Helpers;
using DesktopEye.Views;
using SkiaSharp;
using Point = Avalonia.Point;

namespace DesktopEye.ViewModels;

public partial class ImageViewModel : ViewModelBase
{
    [ObservableProperty] private Bitmap? _bitmap;
    [ObservableProperty] private Rect? _selection;
    [ObservableProperty] private SKBitmap? _skBitmap;

    public ImageViewModel(SKBitmap bitmap)
    {
        SkBitmap = bitmap;
        Bitmap = bitmap.ToAvaloniaBitmap();
    }

    [RelayCommand]
    private void ProcessSelection(Point[] points)
    {
        var startPoint = points[0];
        var endPoint = points[1];

        var cropedBitmap = SkBitmap.CropBitmap(startPoint, endPoint);
        var window = new InteractionWindow
        {
            DataContext = new InteractionViewModel(cropedBitmap)
        };
        window.Show();
    }
}