using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DesktopEye.Common.Enums;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DesktopEye.Common.Services.OCR;

public class OcrManager : IOcrManager
{
    private readonly object _lock = new();
    private readonly ILogger<OcrManager> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IServiceProvider _services;
    private IOcrService? _currentOcr;
    private OcrType _currentOcrType;

    public OcrManager(IServiceProvider services, ILogger<OcrManager> logger)
    {
        _services = services;
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("OcrManager initialized");
        SwitchTo(OcrType.Tesseract);
    }

    public async Task SwitchToAsync(OcrType ocrType)
    {
        await _semaphore.WaitAsync();
        try
        {
            _logger.LogDebug("Attempting to switch OCR to type: {OcrType}", ocrType);

            if (_currentOcrType == ocrType && _currentOcr != null)
            {
                _logger.LogDebug("OCR type {OcrType} is already active, skipping switch", ocrType);
                return;
            }

            // Dispose current OCR service if it exists
            if (_currentOcr is IDisposable disposable)
            {
                _logger.LogDebug("Disposing current OCR service of type: {CurrentOcrType}", _currentOcrType);
                try
                {
                    disposable.Dispose();
                    _logger.LogDebug("Successfully disposed OCR service of type: {CurrentOcrType}", _currentOcrType);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing OCR service of type: {CurrentOcrType}", _currentOcrType);
                }
            }

            // Create new OCR service
            _currentOcr = ocrType switch
            {
                OcrType.Tesseract => _services.GetService<TesseractOcrService>(),
                _ => throw new ArgumentException($"Unsupported ocr type: {ocrType}")
            };

            if (_currentOcr == null)
            {
                _logger.LogError("Failed to create OCR service for type: {OcrType}. Service returned null",
                    ocrType);
                throw new InvalidOperationException($"Failed to create OCR service for type: {ocrType}");
            }

            _currentOcrType = ocrType;
            _logger.LogInformation("Successfully switched to OCR type: {OcrType}", ocrType);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to switch to OCR type: {OcrType}", ocrType);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string> GetTextFromBitmapAsync(Bitmap bitmap, List<Language> languages)
    {
        await _semaphore.WaitAsync();

        try
        {
            if (_currentOcr == null)
            {
                _logger.LogError("Attempted to perform OCR but no OCR service is currently selected");
                throw new InvalidOperationException("No ocr is currently selected");
            }

            if (bitmap == null)
            {
                _logger.LogError("Attempted to perform OCR with null bitmap");
                throw new ArgumentNullException(nameof(bitmap));
            }

            _logger.LogDebug(
                "Starting OCR operation with {OcrType}. Bitmap size: {Width}x{Height}, Languages: {Languages}",
                _currentOcrType, bitmap.PixelSize.Width, bitmap.PixelSize.Height, string.Join(", ", languages));

            var stopwatch = Stopwatch.StartNew();

            try
            {
                var result = await _currentOcr.GetTextFromBitmapAsync(bitmap, languages);
                stopwatch.Stop();

                _logger.LogInformation(
                    "OCR operation completed successfully in {ElapsedMs}ms. Result length: {ResultLength} characters",
                    stopwatch.ElapsedMilliseconds, result.Length);

                if (string.IsNullOrEmpty(result))
                    _logger.LogWarning("OCR operation returned empty or null result");
                else
                    _logger.LogDebug("OCR result preview: {ResultPreview}",
                        result.Length > 100 ? result.Substring(0, 100) + "..." : result);

                return result;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                _logger.LogError(ex, "OCR operation failed after {ElapsedMs}ms using {OcrType}",
                    stopwatch.ElapsedMilliseconds, _currentOcrType);
                throw;
            }
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public OcrType GetCurrentOcrType()
    {
        _logger.LogDebug("Current OCR type requested: {OcrType}", _currentOcrType);
        return _currentOcrType;
    }

    private void SwitchTo(OcrType ocrType)
    {
        lock (_lock)
        {
            _logger.LogDebug("Attempting to switch OCR to type: {OcrType}", ocrType);

            if (_currentOcrType == ocrType && _currentOcr != null)
            {
                _logger.LogDebug("OCR type {OcrType} is already active, skipping switch", ocrType);
                return;
            }

            // Dispose current OCR service if it exists
            if (_currentOcr is IDisposable disposable)
            {
                _logger.LogDebug("Disposing current OCR service of type: {CurrentOcrType}", _currentOcrType);
                try
                {
                    disposable.Dispose();
                    _logger.LogDebug("Successfully disposed OCR service of type: {CurrentOcrType}", _currentOcrType);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing OCR service of type: {CurrentOcrType}", _currentOcrType);
                }
            }

            // Create new OCR service
            try
            {
                _currentOcr = ocrType switch
                {
                    OcrType.Tesseract => _services.GetService<TesseractOcrService>(),
                    _ => throw new ArgumentException($"Unsupported ocr type: {ocrType}")
                };

                if (_currentOcr == null)
                {
                    _logger.LogError("Failed to create OCR service for type: {OcrType}. Service returned null",
                        ocrType);
                    throw new InvalidOperationException($"Failed to create OCR service for type: {ocrType}");
                }

                _currentOcrType = ocrType;
                _logger.LogInformation("Successfully switched to OCR type: {OcrType}", ocrType);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to switch to OCR type: {OcrType}", ocrType);
                throw;
            }
        }
    }
}