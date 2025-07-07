using System;
using System.IO;
using System.Threading.Tasks;
using Bugsnag;
using DesktopEye.Common.Infrastructure.Models;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using DesktopEye.Common.Infrastructure.Services.Download;
using Microsoft.Extensions.Logging;
using Python.Runtime;

namespace DesktopEye.Common.Infrastructure.Services.TrainedModel;

public class ModelDownloadService : IModelDownloadService
{
    private readonly IDownloadService _downloadService;
    private readonly IPathService _pathService;
    private readonly ILogger<ModelDownloadService> _logger;
    private readonly Bugsnag.IClient _bugsnagClient;

    public ModelDownloadService(IDownloadService downloadService, IPathService pathService, ILogger<ModelDownloadService> logger,
        IClient bugsnagClient)
    {
        _downloadService = downloadService;
        _logger = logger;
        _bugsnagClient = bugsnagClient;
        _pathService = pathService;
    }

    public async Task<bool> DownloadModelAsync(Model model)
    {
        try
        {
            // Log the model download information
            _logger.LogInformation("Downloading model {ModelName} from {ModelUrl}", model.ModelName, model.ModelUrl);
            var modelFolderPath = Path.Combine(_pathService.ModelsDirectory, model.ModelFolderName);
            var modelPath = Path.Combine(modelFolderPath, model.ModelName);

            // Ensure the models folder exists
            Directory.CreateDirectory(modelFolderPath);
            _logger.LogDebug("Created models directory: {_modelsFolderPath}", modelPath);
            _logger.LogDebug("Download URL for {Language}: {DownloadUrl}", model.ModelName, model.ModelUrl);

            // Download the model file
            var result = false;
            if (model.Source == ModelSource.HuggingFace && model.Runtime == ModelRuntime.NllbPyTorch)
            {
                _logger.LogDebug("Loading tokenizer asynchronously");
                var tokenizer = await LoadTokenizerAsync(model.ModelName, modelFolderPath);
                _logger.LogDebug("Loading model asynchronously");
                var trainedModel = await LoadModelAsync(model.ModelName, modelFolderPath);
                if (tokenizer != null && trainedModel != null)
                {
                    _logger.LogInformation("Successfully loaded model and tokenizer for language {Language}",
                        model.ModelName);
                    result = true;
                }
                else
                {
                    _logger.LogError("Failed to load model or tokenizer for language {Language}", model.ModelName);
                }
            }
            else
            {
                result = await _downloadService.DownloadFileAsync(model.ModelUrl, modelPath);
                if (result)
                    _logger.LogInformation("Successfully downloaded model for language {Language} to {ModelPath}",
                        model.ModelName,
                        modelPath);
                else
                    _logger.LogError("Failed to download model for language {Language} from {DownloadUrl}", model.ModelName,
                        model.ModelUrl);
            }
            
            return result;
        }
        catch (Exception ex)
        {
            _bugsnagClient.Notify(ex);
            _logger.LogError(ex, "Error logging model download information");
        }

        return false;
    }
    
     private async Task<dynamic> LoadTokenizerAsync(string modelName, string modelDirectory)
    {
        _logger.LogDebug("Loading tokenizer asynchronously for model: {ModelName}", modelName);

        return await Task.Run(() =>
        {
            try
            {
                _logger.LogTrace("Acquiring Python GIL for tokenizer loading");
                using (Py.GIL())
                {
                    _logger.LogTrace("Python GIL acquired for tokenizer loading");

                    try
                    {
                        
                        _logger.LogTrace("Importing transformers module");
                        dynamic transformers = Py.Import("transformers");

                        _logger.LogTrace("Getting AutoTokenizer from transformers");
                        var autoTokenizer = transformers.GetAttr("AutoTokenizer");

                        _logger.LogDebug("Loading tokenizer from pretrained model with cache directory: {CacheDir}",
                            modelDirectory);
                        var tokenizer = autoTokenizer.from_pretrained(modelName, cache_dir: modelDirectory);

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
        });
    }

    private async Task<dynamic> LoadModelAsync(string modelName, string modelDirectory)
    {
        _logger.LogDebug("Loading model asynchronously: {ModelName}", modelName);

        return await Task.Run(() =>
        {
            try
            {
                _logger.LogTrace("Acquiring Python GIL for model loading");
                using (Py.GIL())
                {
                    _logger.LogTrace("Python GIL acquired for model loading");

                    try
                    {
                        _logger.LogTrace("Importing transformers module for model loading");
                        dynamic transformers = Py.Import("transformers");

                        _logger.LogTrace("Getting AutoModelForSeq2SeqLM from transformers");
                        var autoModelForSeq2SeqLm = transformers.GetAttr("AutoModelForSeq2SeqLM");

                        _logger.LogDebug("Loading model from pretrained with cache directory: {CacheDir}",
                            modelDirectory);
                        var model = autoModelForSeq2SeqLm.from_pretrained(modelName,
                            cache_dir: modelDirectory,
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
        });
    }
}