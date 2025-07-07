using DesktopEye.Common.Infrastructure.Models;

namespace DesktopEye.Common.Infrastructure.Services.TrainedModel;

public interface IModelStorageService
{
    public bool IsModelAvailable(Model model);
}