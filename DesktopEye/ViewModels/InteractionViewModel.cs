using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopEye.Services.OCRService;
using TesseractOCR.Enums;

namespace DesktopEye.ViewModels;

public partial class InteractionViewModel : ViewModelBase
{
    [ObservableProperty] private Bitmap? _bitmap;
    [ObservableProperty] private string? _ocrText;

    public InteractionViewModel(Bitmap bitmap)
    {
        Bitmap = bitmap;
        var ocr = new TesseractOcrService([Language.English]);
        OcrText = ocr.BitmapToText(bitmap);
    }
}