using System.Threading.Tasks;

namespace DesktopEye.Common.Services.Download;

public interface IDownloadService
{
    Task<bool> DownloadFileAsync(string url, string destinationPath);
}