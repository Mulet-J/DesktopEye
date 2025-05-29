using System;
using Avalonia;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Extensions;
using DesktopEye.Helpers;
using DesktopEye.Views;
using SkiaSharp;

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
    private void ProcessSelection()
    {
        if (!Selection.HasValue) return;
        var cropedBitmap = SkBitmap.CropBitmap(Selection.Value.TopLeft, Selection.Value.BottomRight);
        var window = new InteractionWindow
        {
            DataContext = new InteractionViewModel(cropedBitmap)
        };
        window.Show();
    }

    [RelayCommand]
    private void Test()
    {
        Console.WriteLine("aze");
    }
}