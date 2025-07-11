using Avalonia.Controls;
using Avalonia.Interactivity;
using DesktopEye.Common.Application.ViewModels.ScreenCapture;

namespace DesktopEye.Common.Application.Views.ScreenCapture;

public partial class ScreenCaptureActionsView : UserControl
{
    public ScreenCaptureActionsView()
    {
        InitializeComponent();
        
        // Abonner au changement d'onglet
        TabControl tabControl = this.FindControl<TabControl>("MainTabControl");
        if (tabControl != null)
        {
            tabControl.SelectionChanged += TabControl_SelectionChanged;
        }
    }

    private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DataContext is ScreenCaptureActionsViewModel viewModel &&
            sender is TabControl tabControl &&
            tabControl.SelectedItem is TabItem selectedTab)
        {
            viewModel.ActiveTab = selectedTab.Name;
        }
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