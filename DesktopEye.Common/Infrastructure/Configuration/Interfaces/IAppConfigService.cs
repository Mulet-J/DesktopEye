using System.Threading.Tasks;
using DesktopEye.Common.Infrastructure.Configuration.Classes;

namespace DesktopEye.Common.Infrastructure.Configuration.Interfaces;

public interface IAppConfigService
{
    public AppConfig Config { get; }
    bool ConfigFileExists();
    bool IsSetupFinished();
    Task LoadConfigAsync();

    Task SaveConfigAsync();
    Task<T> GetValueAsync<T>(string key, T defaultValue = default!);
    Task SetValueAsync<T>(string key, T value);
}