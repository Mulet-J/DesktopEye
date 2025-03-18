using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopEye.Helpers;
using DesktopEye.Services.OCRService;
using SkiaSharp;

namespace DesktopEye.ViewModels;

public partial class InteractionViewModel : ViewModelBase
{
    [ObservableProperty] private Bitmap? _bitmap;
    [ObservableProperty] private SKBitmap? _skBitmap;

    public InteractionViewModel(SKBitmap bitmap)
    {
        SkBitmap = bitmap;
        Bitmap = bitmap.ToAvaloniaBitmap();
        TesseractOcrService.a(bitmap);
    }
}