using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopEye.Extensions;
using DesktopEye.Services.OCRService;
using SkiaSharp;
using TesseractOCR.Enums;

namespace DesktopEye.ViewModels;

public partial class ScreenCaptureActionsViewModel : ViewModelBase
{
    [ObservableProperty] private Bitmap? _bitmap;
    [ObservableProperty] private string? _ocrText;
    [ObservableProperty] private SKBitmap? _skBitmap;

    public ScreenCaptureActionsViewModel(SKBitmap bitmap)
    {
        SkBitmap = bitmap;
        Bitmap = bitmap.ToAvaloniaBitmap();
        var ocr = new TesseractOcrService([Language.English]);
        OcrText = ocr.BitmapToText(bitmap);
    }
}