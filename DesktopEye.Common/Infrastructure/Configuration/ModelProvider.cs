﻿using System;
using System.Collections.Generic;
using System.Linq;
using Bugsnag;
using DesktopEye.Common.Infrastructure.Models;
using DesktopEye.Common.Infrastructure.Services.TrainedModel;
using Microsoft.Extensions.Logging;
using TesseractOCR.Enums;

namespace DesktopEye.Common.Infrastructure.Configuration;

public class ModelProvider : IModelProvider
{
    private readonly IModelStorageService _modelStorageService;
    private readonly IModelDownloadService _modelDownloadService;
    private readonly ModelRegistry _modelRegistry = new();
    private readonly ILogger<ModelProvider> _logger;
    private readonly Bugsnag.IClient _bugsnagClient;

    public ModelProvider(IModelStorageService modelStorageService, IModelDownloadService modelDownloadService,
        ILogger<ModelProvider> logger, IClient bugsnagClient)
    {
        _modelStorageService = modelStorageService;
        _modelDownloadService = modelDownloadService;
        _logger = logger;
        _bugsnagClient = bugsnagClient;
    }
    
    /// <summary>
    /// Processes the model provider by loading model definitions, checking availability, and downloading models if necessary.
    /// </summary>
    /// <param name="userCustomModels">List of user-defined custom models</param>
    /// <param name="userSelectedOcrLanguages">List of user-selected OCR languages</param>
    /// <returns>List of models including both default and user-defined models</returns>
    public List<Model> Process(List<Model> userCustomModels, List<Language> userSelectedOcrLanguages)
    {
        // Load model definitions from the registry and combine with user-defined custom models
        var models = LoadModelDefinitions(userCustomModels, userSelectedOcrLanguages);
        
        try
        {
            // Check which models are available and which are not
            var unavailableModels = GetUnavailableModels(models);

            // Download models that are not available
            DownloadModels(unavailableModels);
        }
        catch (Exception e)
        {
            _bugsnagClient.Notify(e);
            _logger.LogError(e, "Error processing model provider");
        }
        return models;
    }
    

    /// <summary>
    /// Loads model definitions from the registry and combines them with user-defined custom models.
    /// </summary>
    /// <param name="userCustomModels">List of user-defined custom models</param>
    /// <param name="userSelectedOcrLanguages"></param>
    /// <returns>Combined list of models including both default and user-defined models</returns>
    private List<Model> LoadModelDefinitions(List<Model> userCustomModels, List<Language> userSelectedOcrLanguages)
    {
        var baseModels = _modelRegistry.DefaultModels;
        var combinedLanguages = new List<Language>(_modelRegistry.DefaultTesseractLanguages);
        foreach (var language in userSelectedOcrLanguages.Where(language => !combinedLanguages.Contains(language)))
        {
            combinedLanguages.Add(language);
        }

        var tesseractModels = _modelRegistry.GenerateTesseractRegistry(combinedLanguages);

        var models = new List<Model>();
        models.AddRange(baseModels);
        models.AddRange(userCustomModels);
        models.AddRange(tesseractModels);

        return models;
    }

    /// <summary>
    /// returns two lists of models: available on the system and not available
    /// </summary>
    /// <param name="models">List of models to check availability</param>
    /// <returns>Tuple containing two lists: available models and unavailable models</returns>
    private List<Model> GetUnavailableModels(List<Model> models)
    {
        return models.Where(model => !_modelStorageService.IsModelAvailable(model)).ToList();
    }

    /// <summary>
    /// Downloads models that are not available on the system.
    /// </summary>
    /// <param name="models">List of models to download</param>
    private async void DownloadModels(List<Model> models)
    {
        try
        {
            foreach (var model in models.Where(model => !_modelStorageService.IsModelAvailable(model)))
            {
                await _modelDownloadService.DownloadModelAsync(model);
            }
        }
        catch (Exception e)
        {
            _bugsnagClient.Notify(e);
            _logger.LogError(e, "Error downloading models");
        }
    }
}