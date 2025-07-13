using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.OpticalCharacterRecognition;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;

namespace DesktopEye.Common.Domain.Features.OpticalCharacterRecognition.Interfaces;

public interface IOcrOrchestrator : IServiceOrchestrator<IOcrService, OcrType>
{
    Task<OcrResult> GetTextFromBitmapAsync(Bitmap bitmap, CancellationToken cancellationToken = default,
        bool preprocess = true);

    Task<OcrResult> GetTextFromBitmapAsync(Bitmap bitmap, List<Language> languages,
        CancellationToken cancellationToken = default, bool preprocess = true);
}