using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.Application.ViewModels.Base;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Infrastructure.Configuration;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;
using DesktopEye.Common.Infrastructure.Models;

namespace DesktopEye.Common.Application.ViewModels.Setup;

public partial class SetupModelsViewModel : ViewModelBase, IValidatableViewModel
{
    private readonly IModelProvider _modelProvider;

    [ObservableProperty] private bool _modelsDownloaded;

    [ObservableProperty] private List<string> _validationErrors = new();

    public SetupModelsViewModel(IModelProvider modelProvider)
    {
        _modelProvider = modelProvider;
    }

    public void ValidateValues()
    {
        if (!ModelsDownloaded) DownloadRequiredModels();
    }

    [RelayCommand]
    private void DownloadRequiredModels()
    {
        var userCustomModels = new List<Model>();
        var userCustomLanguages = new List<Language>();
        _ = _modelProvider.Process(userCustomModels, userCustomLanguages);
        ModelsDownloaded = true;
    }
}