using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DesktopEye.Common.Enums;

namespace DesktopEye.Common.Services.OCR;

public interface IOcrManager
{
    Task SwitchToAsync(OcrType ocrType);
    Task<string> GetTextFromBitmapAsync(Bitmap bitmap, List<Language> languages);
    OcrType GetCurrentOcrType();
}