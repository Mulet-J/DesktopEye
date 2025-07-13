using System;
using System.IO;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;

namespace DesktopEye.Common.Infrastructure.Services.ApplicationPath;

public class PathService : IPathService
{
    private const string ApplicationName = "DesktopEye";
    private readonly IAppConfigService _appConfigService;


    public PathService(IAppConfigService appConfigService)
    {
        // Initialize directories based on the application name
        AppDataDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData).Replace(" ", "_"),
            ApplicationName);

        _appConfigService = appConfigService;
        appConfigService.LoadConfigAsync().ConfigureAwait(false);

        EnsureDirectoriesExist();
    }

    public string AppDataDirectory { get; }
    public string LocalAppDataDirectory => _appConfigService.Config.LocalAppDataDirectory;
    public string CacheDirectory => Path.Combine(LocalAppDataDirectory, "cache");
    public string CondaDirectory => Path.Combine(LocalAppDataDirectory, "miniconda");
    public string DownloadsDirectory => Path.Combine(LocalAppDataDirectory, "downloads");
    public string ModelsDirectory => Path.Combine(LocalAppDataDirectory, "models");

    private void EnsureDirectoriesExist()
    {
        try
        {
            // We don't create conda's folder because the script create the folder by itself
            Directory.CreateDirectory(AppDataDirectory);
            Directory.CreateDirectory(LocalAppDataDirectory);
            Directory.CreateDirectory(CacheDirectory);
            Directory.CreateDirectory(ModelsDirectory);
            Directory.CreateDirectory(DownloadsDirectory);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to initialize application directories", ex);
        }
    }
}