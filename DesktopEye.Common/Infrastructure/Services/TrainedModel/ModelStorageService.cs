using System.IO;
using DesktopEye.Common.Infrastructure.Models;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;

namespace DesktopEye.Common.Infrastructure.Services.TrainedModel;

public class ModelStorageService : IModelStorageService
{
    private readonly IPathService _pathService;

    public ModelStorageService(IPathService pathService)
    {
        _pathService = pathService;
    }

    /// <summary>
    /// Checks if a model is available in the local storage.
    /// </summary>
    /// <param name="model">The model to check.</param>
    /// <returns>True if the model is available, otherwise false.</returns>
    public bool IsModelAvailable(Model model)
    {
        var modelPath = Path.Combine(_pathService.ModelsDirectory, model.ModelFolderName, model.ModelName);
        return File.Exists(modelPath);
    }
}