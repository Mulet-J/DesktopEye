using System;
using System.Collections.Generic;
using System.IO;

namespace DesktopEye.Common.Infrastructure.Configuration.Classes;

public class AppConfig
{
    private string? _localAppDataDirectory;

    private bool? _setupFinished;

    public string LocalAppDataDirectory
    {
        get => string.IsNullOrEmpty(_localAppDataDirectory)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DesktopEye")
            : _localAppDataDirectory;
        set => _localAppDataDirectory = value;
    }

    public bool SetupFinished
    {
        get => _setupFinished ?? false;
        set => _setupFinished = value;
    }

    public Dictionary<string, object> CustomSettings { get; set; } = new();
}