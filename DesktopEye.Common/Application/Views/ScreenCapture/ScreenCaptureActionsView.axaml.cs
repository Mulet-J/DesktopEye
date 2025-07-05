using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DesktopEye.Common.Application.Views.ScreenCapture;

public partial class ScreenCaptureActionsView : UserControl
{
    public ScreenCaptureActionsView()
    {
        InitializeComponent();
    }

    private void OnTextTabClick(object? sender, RoutedEventArgs e)
    {
        // Activer l'onglet Texte
        var textTab = this.FindControl<Button>("TextTabButton");
        var translationTab = this.FindControl<Button>("TranslationTabButton");
        var textContent = this.FindControl<Grid>("TextTabContent");
        var translationContent = this.FindControl<Grid>("TranslationTabContent");

        if (textTab != null && translationTab != null && textContent != null && translationContent != null)
        {
            textTab.Classes.Add("active");
            translationTab.Classes.Remove("active");
            textContent.IsVisible = true;
            translationContent.IsVisible = false;
        }
    }

    private void OnTranslationTabClick(object? sender, RoutedEventArgs e)
    {
        // Activer l'onglet Traduction
        var textTab = this.FindControl<Button>("TextTabButton");
        var translationTab = this.FindControl<Button>("TranslationTabButton");
        var textContent = this.FindControl<Grid>("TextTabContent");
        var translationContent = this.FindControl<Grid>("TranslationTabContent");

        if (textTab != null && translationTab != null && textContent != null && translationContent != null)
        {
            textTab.Classes.Remove("active");
            translationTab.Classes.Add("active");
            textContent.IsVisible = false;
            translationContent.IsVisible = true;
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window;
        window?.Close();
    }
}