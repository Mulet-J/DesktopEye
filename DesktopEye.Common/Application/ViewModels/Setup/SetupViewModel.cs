using System;
using System.Collections.Generic;
using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.Application.ViewModels.Base;
using DesktopEye.Common.Application.Views.Setup;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DesktopEye.Common.Application.ViewModels.Setup;

public partial class SetupViewModel : ViewModelBase
{
    private readonly IAppConfigService _appConfigService;
    private readonly IServiceProvider _serviceProvider;
    [ObservableProperty] private bool _canGoBack;

    [ObservableProperty] private UserControl? _currentChildView;
    [ObservableProperty] private int _currentStep;
    [ObservableProperty] private string _nextButtonText = "Next";
    [ObservableProperty] private List<(Type view, Type viewModel)> _steps = new();

    public SetupViewModel(IServiceProvider serviceProvider, IAppConfigService appConfigService)
    {
        _serviceProvider = serviceProvider;
        _appConfigService = appConfigService;
        InitializeSteps();
        UpdateCurrentStep();
    }

    [RelayCommand]
    private void GoNext()
    {
        if (CurrentChildView?.DataContext is IValidatableViewModel validatableViewModel)
        {
            validatableViewModel.ValidateValues();

            if (validatableViewModel.ValidationErrors.Count > 0) return;
        }

        if (CurrentStep < Steps.Count - 1)
        {
            CurrentStep++;
            UpdateCurrentStep();
        }
        else
        {
            _appConfigService.SetValueAsync("SetupFinished", true);
            SetupCompleted?.Invoke(this, EventArgs.Empty);
        }
    }

    [RelayCommand]
    private void GoBack()
    {
        if (CurrentStep > 0)
        {
            CurrentStep--;
            UpdateCurrentStep();
        }
    }

    private void UpdateCurrentStep()
    {
        SetChildContext(Steps[CurrentStep].view, Steps[CurrentStep].viewModel);
        CanGoBack = CurrentStep > 0;
        NextButtonText = CurrentStep == Steps.Count - 1 ? "Finish" : "Next";
    }

    private void InitializeSteps()
    {
        Steps.Add((typeof(SetupPathView), typeof(SetupPathViewModel)));
        Steps.Add((typeof(SetupPythonView), typeof(SetupPythonViewModel)));
        Steps.Add((typeof(SetupModelsView), typeof(SetupModelsViewModel)));
        // Add more steps as needed
    }

    public void SetStep(int stepId)
    {
        if (stepId >= 0 && stepId < Steps.Count)
        {
            CurrentStep = stepId;
            UpdateCurrentStep();
        }
    }

    private void SetChildContext(Type view, Type viewModel)
    {
        // Create the view instance using the service provider
        var viewInstance = (UserControl)Activator.CreateInstance(view)!;

        // Create the viewmodel instance using the service provider
        var viewModelInstance = (ViewModelBase)_serviceProvider.GetRequiredService(viewModel);

        // Set the view's DataContext to the viewmodel
        viewInstance.DataContext = viewModelInstance;

        // Update the observable properties
        CurrentChildView = viewInstance;
    }

    // Event to notify when setup is finished
    public event EventHandler? SetupCompleted;
}