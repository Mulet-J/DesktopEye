using System;
using System.IO;

namespace DesktopEye.Common.Infrastructure.Services.ApplicationPath;

public class PathService : IPathService
{
    private const string ApplicationName = "DesktopEye";

    public PathService()
    {
        // Initialize directories based on the application name
        AppDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ApplicationName);
        LocalAppDataDirectory =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                ApplicationName);
        CacheDirectory = Path.Combine(LocalAppDataDirectory, "cache");
        ModelsDirectory = Path.Combine(LocalAppDataDirectory, "models");
        CondaDirectory = Path.Combine(LocalAppDataDirectory, "miniconda");
        DownloadsDirectory = Path.Combine(LocalAppDataDirectory, "downloads");
    }

    public string AppDataDirectory { get; }
    public string CacheDirectory { get; }
    public string CondaDirectory { get; }
    public string DownloadsDirectory { get; }
    public string LocalAppDataDirectory { get; }
    public string ModelsDirectory { get; }
}