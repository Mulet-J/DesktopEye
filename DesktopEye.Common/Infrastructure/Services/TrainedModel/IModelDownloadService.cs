using System.Threading.Tasks;
using DesktopEye.Common.Infrastructure.Models;

namespace DesktopEye.Common.Infrastructure.Services.TrainedModel;

public interface IModelDownloadService
{
    public Task<bool> DownloadModelAsync(Model model);
}