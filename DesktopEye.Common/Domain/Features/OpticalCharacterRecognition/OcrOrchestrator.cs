using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DesktopEye.Common.Domain.Features.OpticalCharacterRecognition.Interfaces;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.OpticalCharacterRecognition;
using DesktopEye.Common.Infrastructure.Configuration;
using Microsoft.Extensions.Logging;

namespace DesktopEye.Common.Domain.Features.OpticalCharacterRecognition;

public class OcrOrchestrator : ServiceOrchestrator<IOcrService, OcrType>, IOcrOrchestrator
{
    private readonly object _lock = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly Bugsnag.IClient _bugsnag;

    public OcrOrchestrator(IServiceProvider services, Bugsnag.IClient bugsnag, ILogger<OcrOrchestrator>? logger = null) : base(services, bugsnag, logger)
    {
        _bugsnag = bugsnag;
    }

    public async Task<OcrResult> GetTextFromBitmapAsync(Bitmap bitmap, List<Language> languages,
        CancellationToken cancellationToken = default, bool preprocess = true)
    {
        return await ExecuteWithServiceAsync(
            async (services, ct) =>
            {
                Logger?.LogDebug("Extracting text from bitmap");
                return await services.GetTextFromBitmapAsync(bitmap, languages, ct, preprocess);
            },
            cancellationToken);
    }

    public async Task<OcrResult> GetTextFromBitmapAsync(Bitmap bitmap,
        CancellationToken cancellationToken = default,
        bool preprocess = true)
    {
        return await ExecuteWithServiceAsync(async (services, ct) =>
        {
            Logger?.LogDebug("Extracting text from bitmap");
            return await services.GetTextFromBitmapAsync(bitmap, ct, preprocess);
        });
    }

    protected override OcrType GetDefaultServiceType()
    {
        return OcrType.Tesseract;
    }
}