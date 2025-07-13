using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using DesktopEye.Common.Application.ViewModels.Base;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;
using DesktopEye.Common.Infrastructure.Services.PathValidation;

namespace DesktopEye.Common.Application.ViewModels.Setup;

public partial class SetupPathViewModel : ViewModelBase, IValidatableViewModel
{
    private readonly IAppConfigService _appConfigService;
    private readonly IPathValidationService _pathValidationService;

    [ObservableProperty] private string? _localAppDataFolder;
    [ObservableProperty] private List<string> _validationErrors = new();

    public SetupPathViewModel(IAppConfigService appConfigService, IPathValidationService pathValidationService)
    {
        _appConfigService = appConfigService;
        _pathValidationService = pathValidationService;

        LocalAppDataFolder = _appConfigService.Config.LocalAppDataDirectory;
    }

    public async void ValidateValues()
    {
        ValidateAppDataPath();

        if (ValidationErrors.Count != 0) return;
        if (LocalAppDataFolder != null)
            await _appConfigService.SetValueAsync("LocalAppDataDirectory", LocalAppDataFolder);
    }

    private void ValidateAppDataPath()
    {
        var result = _pathValidationService.ValidateAppDataPath(LocalAppDataFolder);
        ValidationErrors = result.Errors;
    }
}