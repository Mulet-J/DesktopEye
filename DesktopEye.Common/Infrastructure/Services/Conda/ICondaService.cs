using System.Collections.Generic;
using System.Threading.Tasks;

namespace DesktopEye.Common.Infrastructure.Services.Conda;

public interface ICondaService
{
    bool IsInstalled { get; }
    string CondaExecutablePath { get; }
    public string PythonDllPath { get; }
    Task<bool> InstallMinicondaAsync();
    Task<bool> InstallPackageUsingCondaAsync(CondaInstallInstruction instruction, string? environmentName = null);
    Task<bool> InstallPackageUsingCondaAsync(List<CondaInstallInstruction> instruction, string? environmentName = null);
    Task<bool> InstallPackageUsingPipAsync(List<string> packages, string? environmentName = null);
    Task<string> ExecuteCondaCommandAsync(string command, string? environmentName = null);
}