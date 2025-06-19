using Avalonia.Controls;
using Avalonia.Interactivity;
using DesktopEye.Common.ViewModels.ScreenCapture;

namespace DesktopEye.Common.Views.ScreenCapture;

public partial class ScreenCaptureActionsView : UserControl
{
    public ScreenCaptureActionsView()
    {
        InitializeComponent();
    }

    private async void OnExtractTextClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ScreenCaptureActionsViewModel viewModel)
        {
            await viewModel.ExtractText();
        }
    }

    private async void OnInferLanguageClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ScreenCaptureActionsViewModel viewModel)
        {
            await viewModel.InferLanguage();
        }
    }

    private async void OnTranslateClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is ScreenCaptureActionsViewModel viewModel)
        {
            await viewModel.Translate();
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        // Fermer la fenêtre parente
        var window = TopLevel.GetTopLevel(this) as Window;
        window?.Close();
    }
}