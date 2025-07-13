using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using DesktopEye.Common.Infrastructure.Configuration.Classes;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;

namespace DesktopEye.Common.Infrastructure.Configuration;

public class AppConfigService : IAppConfigService
{
    private const string ApplicationName = "DesktopEye";
    private const string ConfigFileName = "appConfig.json";
    private readonly string _configFilePath;
    private readonly JsonSerializerOptions _jsonOptions;

    public AppConfigService()
    {
        // Always use the default AppData location for the config file itself
        var appDataPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ApplicationName);

        Directory.CreateDirectory(appDataPath);

        _configFilePath = Path.Combine(appDataPath, ConfigFileName);

        _jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };

        Config = new AppConfig();
        LoadConfig();
    }

    public AppConfig Config { get; private set; }


    public bool ConfigFileExists()
    {
        return File.Exists(_configFilePath);
    }

    public bool IsSetupFinished()
    {
        if (!ConfigFileExists()) return false;
        if (Config.SetupFinished == null || !Config.SetupFinished) return false;
        return true;
    }

    public async Task<T> GetValueAsync<T>(string key, T defaultValue = default!)
    {
        try
        {
            var property = typeof(AppConfig).GetProperty(key);
            if (property != null)
            {
                var value = property.GetValue(Config);
                return value is T typedValue ? typedValue : defaultValue;
            }

            // Check custom settings
            if (Config.CustomSettings.TryGetValue(key, out var customValue))
            {
                if (customValue is JsonElement jsonElement)
                    return JsonSerializer.Deserialize<T>(jsonElement.GetRawText(), _jsonOptions) ?? defaultValue;
                return customValue is T typedCustomValue ? typedCustomValue : defaultValue;
            }

            return defaultValue;
        }
        catch
        {
            return defaultValue;
        }
    }

    public async Task SetValueAsync<T>(string key, T value)
    {
        try
        {
            var property = typeof(AppConfig).GetProperty(key);
            if (property != null && property.CanWrite)
                property.SetValue(Config, value);
            else
                // Store in custom settings
                Config.CustomSettings[key] = value!;

            await SaveConfigAsync();
            NotifyConfigChanged();
        }
        catch (Exception ex)
        {
            ;
        }
    }

    public event EventHandler<AppConfig>? ConfigChanged;


    public void NotifyConfigChanged()
    {
        ConfigChanged?.Invoke(this, Config);
    }

    #region LoadConfig

    public void LoadConfig()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = File.ReadAllText(_configFilePath); // Synchronous
                var loadedConfig = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);

                if (loadedConfig != null)
                {
                    Config = loadedConfig;
                    NotifyConfigChanged();
                }
            }
            else
            {
                // Create default config file if it doesn't exist
                SaveConfig();
            }
        }
        catch (Exception ex)
        {
            Config = new AppConfig();
            SaveConfig();
        }
    }

    public async Task LoadConfigAsync()
    {
        try
        {
            if (File.Exists(_configFilePath))
            {
                var json = await File.ReadAllTextAsync(_configFilePath);
                var loadedConfig = JsonSerializer.Deserialize<AppConfig>(json, _jsonOptions);

                if (loadedConfig != null)
                {
                    Config = loadedConfig;
                    NotifyConfigChanged();
                }
            }
            else
            {
                // Create default config file if it doesn't exist
                await SaveConfigAsync();
            }
        }
        catch (Exception ex)
        {
            // Log error and use default config
            Config = new AppConfig();
            await SaveConfigAsync();
        }
    }

    #endregion

    #region SaveConfig

    private void SaveConfig()
    {
        try
        {
            var json = JsonSerializer.Serialize(Config, _jsonOptions);
            File.WriteAllText(_configFilePath, json); // Synchronous
        }
        catch (Exception ex)
        {
            ;
        }
    }

    public async Task SaveConfigAsync()
    {
        try
        {
            var json = JsonSerializer.Serialize(Config, _jsonOptions);
            await File.WriteAllTextAsync(_configFilePath, json);
        }
        catch (Exception ex)
        {
            ;
        }
    }

    #endregion
}