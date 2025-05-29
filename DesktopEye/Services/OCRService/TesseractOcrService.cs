using System.Collections.Generic;
using Avalonia.Media.Imaging;
using DesktopEye.Extensions;
using TesseractOCR;
using TesseractOCR.Enums;

namespace DesktopEye.Services.OCRService;

public class TesseractOcrService : IOcrService
{
    private readonly Engine _engine;

    public TesseractOcrService(List<Language> languages)
    {
        //TODO make tessdata folder dynamic
        _engine = new Engine("C:\\Ecole\\DesktopEye\\DesktopEye.Windows\\.testData", languages);
    }

    public string BitmapToText(Bitmap bitmap)
    {
        var picture = bitmap.ToTesseractImage();
        using var page = _engine.Process(picture);
        var text = page.Text;
        return text;
    }
}