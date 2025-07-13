// In SetupWindow.axaml.cs

using System;
using Avalonia.Controls;
using DesktopEye.Common.Application.ViewModels.Setup;

namespace DesktopEye.Common.Application.Views.Setup;

public partial class SetupWindow : Window
{
    private SetupViewModel? _currentViewModel;

    public SetupWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        // Unsubscribe from previous ViewModel if any
        if (_currentViewModel != null) _currentViewModel.SetupCompleted -= OnSetupCompleted;

        // Subscribe to new ViewModel
        if (DataContext is SetupViewModel setupViewModel)
        {
            _currentViewModel = setupViewModel;
            setupViewModel.SetupCompleted += OnSetupCompleted;
        }
    }

    private void OnSetupCompleted(object? sender, EventArgs e)
    {
        // The setup completion is now handled by the App class
        // This method can be used for any window-specific cleanup if needed

        // Optional: You can still do window-specific cleanup here
        // but don't close the window - let the App class handle the transition
    }

    // Clean up event subscriptions when window is closed
    protected override void OnClosed(EventArgs e)
    {
        if (_currentViewModel != null) _currentViewModel.SetupCompleted -= OnSetupCompleted;

        base.OnClosed(e);
    }
}