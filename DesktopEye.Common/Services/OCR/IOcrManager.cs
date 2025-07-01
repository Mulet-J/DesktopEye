using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DesktopEye.Common.Classes;
using DesktopEye.Common.Enums;
using DesktopEye.Common.Services.Base;

namespace DesktopEye.Common.Services.OCR;

public interface IOcrManager : IBaseServiceManager<IOcrService, OcrType>
{
    Task<OcrResult> GetTextFromBitmapAsync(Bitmap bitmap, CancellationToken cancellationToken = default,
        bool preprocess = true);

    Task<OcrResult> GetTextFromBitmapAsync(Bitmap bitmap, List<Language> languages,
        CancellationToken cancellationToken = default, bool preprocess = true);
}