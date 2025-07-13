using System.Collections.Generic;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.Application.ViewModels.Base;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;
using DesktopEye.Common.Infrastructure.Services.Conda;

namespace DesktopEye.Common.Application.ViewModels.Setup;

public partial class SetupPythonViewModel : ViewModelBase, IValidatableViewModel
{
    private readonly ICondaService _condaService;

    [ObservableProperty] private bool _installSuccess;
    [ObservableProperty] private bool _isCondaInstalled;
    [ObservableProperty] private List<string> _validationErrors = new();

    public SetupPythonViewModel(ICondaService condaService)
    {
        _condaService = condaService;
        _isCondaInstalled = _condaService.IsInstalled;
    }

    public async void ValidateValues()
    {
        ValidationErrors = [];
        if (!IsCondaInstalled) ValidationErrors.Add("The conda environment is not currently installed.");
    }

    [RelayCommand]
    private async Task SetupUpConda()
    {
        var a = await InstallConda();
        if (IsCondaInstalled) InstallSuccess = await InstallCondaLibraries();
    }

    private async Task<bool> InstallConda()
    {
        var status = false;
        if (!IsCondaInstalled) status = await _condaService.InstallMinicondaAsync();

        IsCondaInstalled = _condaService.IsInstalled;
        return status;
    }

    private async Task<bool> InstallCondaLibraries()
    {
        return await _condaService.InstallPackageUsingPipAsync(["transformers", "torch", "accelerate"]);
    }
}