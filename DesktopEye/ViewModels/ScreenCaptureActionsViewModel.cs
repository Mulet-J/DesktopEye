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

    public ScreenCaptureActionsViewModel(Bitmap bitmap)
    {
        Bitmap = bitmap;
        var ocr = new TesseractOcrService([Language.English]);
        OcrText = ocr.BitmapToText(bitmap);
    }
}