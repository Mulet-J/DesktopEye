namespace DesktopEye.Common.Services.ApplicationPath;

public interface IPathService
{
    string AppDataDirectory { get; }
    string LocalAppDataDirectory { get; }
    string CondaDirectory { get; }
    string ModelsDirectory { get; }
    string CacheDirectory { get; }
    string DownloadsDirectory { get; }
}