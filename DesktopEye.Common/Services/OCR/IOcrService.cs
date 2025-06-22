using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DesktopEye.Common.Enums;
using DesktopEye.Common.Interfaces;

namespace DesktopEye.Common.Services.OCR;

public interface IOcrService : ILoadable
{
    Task<string> GetTextFromBitmapAsync(Bitmap bitmap, List<Language> languages);
    Task<string> GetTextFromBitmapTwoPassAsync(Bitmap bitmap);
}