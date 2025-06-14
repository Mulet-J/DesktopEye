using System;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Common.Services.Translation;

public class TranslationManager : ITranslationManager
{
    private readonly object _lock = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IServiceProvider _services;
    private ITranslationService? _currentTranslator;
    private TranslationType _currentTranslatorType;

    public TranslationManager(IServiceProvider services)
    {
        _services = services;
        SwitchTo(TranslationType.Nllb);
    }

    public async Task SwitchToAsync(TranslationType translatorType)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_currentTranslatorType == translatorType && _currentTranslator != null)
                return;

            if (_currentTranslator is IDisposable disposable)
                disposable.Dispose();

            _currentTranslator = translatorType switch
            {
                TranslationType.Nllb => _services.GetService<NllbPyTorchTranslationService>(),
                _ => throw new ArgumentException($"Unsupported ocr type: {translatorType}")
            };

            _currentTranslatorType = translatorType;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<string> TranslateAsync(string input, Language sourceLanguage, Language targetLanguage)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_currentTranslator == null)
                throw new InvalidOperationException("No translator is currently selected");

            return await _currentTranslator.TranslateAsync(input, sourceLanguage, targetLanguage);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public TranslationType GetCurrentTranslatorType()
    {
        return _currentTranslatorType;
    }

    public async Task InitializeService()
    {
        await _currentTranslator.LoadRequiredAsync();
    }

    public string Translate(string input, Language sourceLanguage, Language targetLanguage)
    {
        lock (_lock)
        {
            if (_currentTranslator == null)
                throw new InvalidOperationException("No translator is currently selected");

            return _currentTranslator.Translate(input, sourceLanguage, targetLanguage);
        }
    }

    private void SwitchTo(TranslationType translatorType)
    {
        lock (_lock)
        {
            if (_currentTranslatorType == translatorType && _currentTranslator != null)
                return;

            if (_currentTranslator is IDisposable disposable)
                disposable.Dispose();

            _currentTranslator = translatorType switch
            {
                TranslationType.Nllb => _services.GetService<NllbPyTorchTranslationService>(),
                _ => throw new ArgumentException($"Unsupported ocr type: {translatorType}")
            };

            _currentTranslatorType = translatorType;
        }
    }
}