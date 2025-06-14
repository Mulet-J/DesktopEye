using System;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Enums;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Common.Services.TextClassifier;

public class TextClassifierManager : ITextClassifierManager
{
    private readonly object _lock = new();
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly IServiceProvider _services;
    private ITextClassifierService? _currentClassifier;
    private ClassifierType _currentClassifierType;

    public TextClassifierManager(IServiceProvider services)
    {
        _services = services;
        // Initialize with a default classifier
        SwitchTo(ClassifierType.NTextCat);
    }

    public async Task SwitchToAsync(ClassifierType classifierType)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_currentClassifierType == classifierType && _currentClassifier != null)
                return; // Already using this classifier

            // Dispose previous classifier if it implements IDisposable
            if (_currentClassifier is IDisposable disposable)
                disposable.Dispose();

            // Create new classifier instance
            _currentClassifier = classifierType switch
            {
                ClassifierType.FastText => _services.GetRequiredService<FastTextClassifierService>(),
                ClassifierType.NTextCat => _services.GetRequiredService<NTextCatClassifierService>(),
                _ => throw new ArgumentException($"Unsupported classifier type: {classifierType}")
            };

            _currentClassifierType = classifierType;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public Language ClassifyText(string text)
    {
        lock (_lock)
        {
            if (_currentClassifier == null)
                throw new InvalidOperationException("No classifier is currently selected");

            return _currentClassifier.ClassifyText(text);
        }
    }

    public async Task<Language> ClassifyTextAsync(string text)
    {
        await _semaphore.WaitAsync();
        try
        {
            if (_currentClassifier == null)
                throw new InvalidOperationException("No translator is currently selected");

            return await _currentClassifier.ClassifyTextAsync(text);
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public ClassifierType GetCurrentClassifierType()
    {
        return _currentClassifierType;
    }

    private void SwitchTo(ClassifierType classifierType)
    {
        lock (_lock)
        {
            if (_currentClassifierType == classifierType && _currentClassifier != null)
                return; // Already using this classifier

            // Dispose previous classifier if it implements IDisposable
            if (_currentClassifier is IDisposable disposable)
                disposable.Dispose();

            // Create new classifier instance
            _currentClassifier = classifierType switch
            {
                ClassifierType.FastText => _services.GetRequiredService<FastTextClassifierService>(),
                ClassifierType.NTextCat => _services.GetRequiredService<NTextCatClassifierService>(),
                _ => throw new ArgumentException($"Unsupported classifier type: {classifierType}")
            };

            _currentClassifierType = classifierType;
        }
    }
}