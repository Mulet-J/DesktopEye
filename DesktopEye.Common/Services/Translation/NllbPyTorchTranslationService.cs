﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Enums;
using DesktopEye.Common.Interfaces;
using DesktopEye.Common.Services.ApplicationPath;
using DesktopEye.Common.Services.Conda;
using DesktopEye.Common.Services.Python;
using Microsoft.Extensions.Logging;
using Python.Runtime;
using PythonException = DesktopEye.Common.Exceptions.PythonException;

namespace DesktopEye.Common.Services.Translation;

public class NllbPyTorchTranslationService : ITranslationService, ILoadable
{
    private const string BaseModel = "facebook/nllb-200-distilled-600M";

    private static readonly List<string> PipDependencies = ["transformers", "pytorch"];

    private readonly ICondaService _condaService;
    private readonly SemaphoreSlim _initializationSemaphore = new(1, 1);
    private readonly ILogger<NllbPyTorchTranslationService> _logger;
    private readonly string _modelDirectory;
    private readonly IPythonRuntimeManager _runtimeManager;
    private Task<bool>? _initializationTask;
    private volatile bool _isInitialized;
    private volatile bool _isInitializing;

    private dynamic? _model;
    private dynamic? _tokenizer;

    //TODO find why the interop code randomly crashes

    public NllbPyTorchTranslationService(ICondaService condaService, IPathService pathService,
        IPythonRuntimeManager runtimeManager, ILogger<NllbPyTorchTranslationService> logger)
    {
        _condaService = condaService;
        _modelDirectory = pathService.ModelsDirectory;
        _runtimeManager = runtimeManager;
        _logger = logger;

        _logger.LogInformation("Initializing NllbPyTorchTranslationService with model directory: {ModelDirectory}",
            _modelDirectory);

        try
        {
            _logger.LogDebug("Starting Python runtime for translation service");
            _runtimeManager.StartRuntime(this);
            _logger.LogInformation("Python runtime started successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initialize NllbPyTorchTranslationService");
            throw;
        }
    }

    public void Dispose()
    {
        _logger.LogDebug("Disposing NllbPyTorchTranslationService");

        try
        {
            _logger.LogDebug("Stopping Python runtime");
            _runtimeManager.StopRuntime(this);
            _logger.LogInformation("Python runtime stopped successfully");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while stopping Python runtime during disposal");
            return;
        }

        _logger.LogDebug("NllbPyTorchTranslationService disposed successfully");
        GC.SuppressFinalize(this);
    }

    private async Task<bool> CondaInstallDependenciesAsync()
    {
        _logger.LogInformation("Installing Python dependencies asynchronously");

        try
        {
            // var res = await _condaService.InstallPackageUsingCondaAsync(PythonDependencies);
            // if (!res) throw new PythonException("Could not install dependencies");

            _logger.LogDebug("Installing packages via pip: transformers, torch");
            var res = await _condaService.InstallPackageUsingPipAsync(PipDependencies, "base");

            if (!res)
            {
                _logger.LogError("Failed to install Python dependencies via pip");
                throw new PythonException("Could not install dependencies");
            }

            _logger.LogInformation("Python dependencies installed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while installing Python dependencies");
            throw;
        }
    }

    private string EnumLanguageToLibLanguage(Language language)
    {
        var result = language switch
        {
            Language.English => "eng_Latn",
            Language.French => "fra_Latn",
            Language.German => "deu_Latn",
            Language.Spanish => "spa_Latn",
            Language.Chinese => "zho_Hans",
            Language.Japanese => "jpn_Jpan",
            Language.Korean => "kor_Hang",
            Language.Portuguese => "por_Latn",
            Language.Italian => "ita_Latn",
            Language.Dutch => "nld_Latn",
            Language.Russian => "rus_Cyrl",
            Language.Swedish => "swe_Latn",
            Language.Norwegian => "nob_Latn",
            Language.Danish => "dan_Latn",
            _ => "eng_Latn"
        };

        _logger.LogTrace("Converted language {InputLanguage} to library format: {OutputLanguage}", language, result);
        return result;
    }


    #region Loading

    /// <summary>
    ///     Preloads the model and tokenizer asynchronously without blocking the main thread
    /// </summary>
    /// <param name="modelName">The model name to load (defaults to BaseModel)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successful, false otherwise</returns>
    public async Task<bool> LoadRequiredAsync(string? modelName = null,
        CancellationToken cancellationToken = default)
    {
        modelName ??= BaseModel;

        if (_isInitialized)
        {
            _logger.LogDebug("Model already initialized, skipping preload");
            return true;
        }

        await _initializationSemaphore.WaitAsync(cancellationToken);
        try
        {
            // Double-check pattern
            if (_isInitialized)
            {
                _logger.LogDebug("Model was initialized while waiting, skipping preload");
                return true;
            }

            if (_isInitializing && _initializationTask != null)
            {
                _logger.LogDebug("Model initialization already in progress, waiting for completion");
                return await _initializationTask;
            }

            _logger.LogInformation("Starting async model preloading for: {ModelName}", modelName);
            _isInitializing = true;

            _initializationTask = Task.Run(async () =>
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    // TODO: Uncomment if you want to install dependencies during preload
                    // _logger.LogDebug("Installing dependencies asynchronously");
                    // var dependenciesInstalled = await CondaInstallDependenciesAsync();
                    // if (!dependenciesInstalled)
                    // {
                    //     _logger.LogError("Failed to install dependencies during preload");
                    //     return false;
                    // }

                    cancellationToken.ThrowIfCancellationRequested();

                    _logger.LogDebug("Loading tokenizer asynchronously");
                    var tokenizer = await LoadTokenizerAsync(modelName, cancellationToken);

                    cancellationToken.ThrowIfCancellationRequested();

                    _logger.LogDebug("Loading model asynchronously");
                    var model = await LoadModelAsync(modelName, cancellationToken);

                    // Atomic assignment
                    _tokenizer = tokenizer;
                    _model = model;
                    _isInitialized = true;

                    _logger.LogInformation("Model preloading completed successfully for: {ModelName}", modelName);
                    return true;
                }
                catch (OperationCanceledException)
                {
                    _logger.LogInformation("Model preloading was cancelled");
                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during model preloading");
                    return false;
                }
                finally
                {
                    _isInitializing = false;
                }
            }, cancellationToken);

            return await _initializationTask;
        }
        finally
        {
            _initializationSemaphore.Release();
        }
    }

    public bool LoadRequired(string? modelName = BaseModel)
    {
        _logger.LogInformation("Loading required components synchronously for model: {ModelName}", modelName);
        _logger.LogWarning("Synchronous LoadRequired is deprecated - use PreloadModelAsync instead");

        try
        {
            return LoadRequiredAsync(modelName).GetAwaiter().GetResult();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load required components for model: {ModelName}", modelName);
            return false;
        }
    }

    private async Task<dynamic> LoadTokenizerAsync(string modelName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading tokenizer asynchronously for model: {ModelName}", modelName);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _logger.LogTrace("Acquiring Python GIL for tokenizer loading");
                using (Py.GIL())
                {
                    _logger.LogTrace("Python GIL acquired for tokenizer loading");

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        _logger.LogTrace("Importing transformers module");
                        dynamic transformers = Py.Import("transformers");

                        _logger.LogTrace("Getting AutoTokenizer from transformers");
                        var autoTokenizer = transformers.GetAttr("AutoTokenizer");

                        _logger.LogDebug("Loading tokenizer from pretrained model with cache directory: {CacheDir}",
                            _modelDirectory);
                        var tokenizer = autoTokenizer.from_pretrained(modelName, cache_dir: _modelDirectory);

                        _logger.LogInformation("Tokenizer loaded successfully for model: {ModelName}", modelName);
                        return tokenizer;
                    }
                    finally
                    {
                        _logger.LogTrace("Python GIL released after tokenizer loading");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Tokenizer loading was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load tokenizer for model: {ModelName}", modelName);
                throw;
            }
        }, cancellationToken);
    }

    private async Task<dynamic> LoadModelAsync(string modelName, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Loading model asynchronously: {ModelName}", modelName);

        return await Task.Run(() =>
        {
            cancellationToken.ThrowIfCancellationRequested();

            try
            {
                _logger.LogTrace("Acquiring Python GIL for model loading");
                using (Py.GIL())
                {
                    _logger.LogTrace("Python GIL acquired for model loading");

                    try
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        _logger.LogTrace("Importing transformers module for model loading");
                        dynamic transformers = Py.Import("transformers");

                        _logger.LogTrace("Getting AutoModelForSeq2SeqLM from transformers");
                        var autoModelForSeq2SeqLm = transformers.GetAttr("AutoModelForSeq2SeqLM");

                        _logger.LogDebug("Loading model from pretrained with cache directory: {CacheDir}",
                            _modelDirectory);
                        var model = autoModelForSeq2SeqLm.from_pretrained(modelName,
                            cache_dir: _modelDirectory,
                            torch_dtype: "auto");

                        _logger.LogInformation("Model loaded successfully: {ModelName}", modelName);
                        return model;
                    }
                    finally
                    {
                        _logger.LogTrace("Python GIL released after model loading");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Model loading was cancelled");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load model: {ModelName}", modelName);
                throw;
            }
        }, cancellationToken);
    }

    #endregion

    #region Translation

    public string Translate(string text, Language sourceLanguage, Language targetLanguage)
    {
        // For synchronous calls, we need to block - but we can make this more obvious
        _logger.LogDebug("Synchronous translation requested - will block until model is loaded");

        // Use GetAwaiter().GetResult() to avoid potential deadlocks compared to .Result
        // EnsureInitializedAsync().GetAwaiter().GetResult();

        return TranslateInternal(text, sourceLanguage, targetLanguage);
    }

    public async Task<string> TranslateAsync(string text, Language sourceLanguage, Language targetLanguage,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug(
            "Starting async translation from {SourceLanguage} to {TargetLanguage}, text length: {TextLength}",
            sourceLanguage, targetLanguage, text.Length);

        // await EnsureInitializedAsync(cancellationToken);

        return await Task.Run(() => TranslateInternal(text, sourceLanguage, targetLanguage), cancellationToken);
    }

    private string TranslateInternal(string text, Language sourceLanguage, Language targetLanguage)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _logger.LogWarning("Translation requested with empty or null text");
            return string.Empty;
        }

        var convertedSourceLanguage = EnumLanguageToLibLanguage(sourceLanguage);
        var convertedTargetLanguage = EnumLanguageToLibLanguage(targetLanguage);

        _logger.LogDebug("Converted languages - Source: {ConvertedSource}, Target: {ConvertedTarget}",
            convertedSourceLanguage, convertedTargetLanguage);

        if (_tokenizer == null)
        {
            _logger.LogError("Tokenizer is null, cannot perform translation");
            throw new InvalidOperationException("Tokenizer is not loaded");
        }

        if (_model == null)
        {
            _logger.LogError("Model is null, cannot perform translation");
            throw new InvalidOperationException("Model is not loaded");
        }

        try
        {
            _logger.LogTrace("Acquiring Python GIL for translation");
            using (Py.GIL())
            {
                _logger.LogTrace("Python GIL acquired successfully");

                try
                {
                    _logger.LogTrace("Setting tokenizer source language to: {SourceLang}", convertedSourceLanguage);
                    _tokenizer.src_lang = convertedSourceLanguage;

                    _logger.LogTrace("Encoding input text with tokenizer");
                    var encoded = _tokenizer(text, return_tensors: "pt");

                    _logger.LogTrace("Text encoded successfully, converting target language tokens");
                    var forcedBosTokenId = _tokenizer.convert_tokens_to_ids(convertedTargetLanguage);

                    _logger.LogTrace("Generating translation tokens using model");
                    var generatedTokens = _model.generate(
                        input_ids: encoded["input_ids"],
                        attention_mask: encoded["attention_mask"],
                        forced_bos_token_id: forcedBosTokenId
                    );

                    _logger.LogTrace("Decoding generated tokens to text");
                    var decoded = _tokenizer.batch_decode(generatedTokens, skip_special_tokens: true);

                    var result = decoded[0].ToString();
                    return result;
                }
                finally
                {
                    _logger.LogTrace("Python GIL released after translation");
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred during translation from {SourceLanguage} to {TargetLanguage}",
                sourceLanguage, targetLanguage);
            throw;
        }
    }

    #endregion
}