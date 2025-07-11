using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using DesktopEye.Common.Domain.Features.Dictionary.Helpers;
using DesktopEye.Common.Infrastructure.Services.Dictionary;
using MsBox.Avalonia;
using System.Timers;
using Avalonia.Threading;
using DesktopEye.Common.Application.ViewModels.ScreenCapture;

using DesktopEye.Common.Application.ViewModels.ScreenCapture;

namespace DesktopEye.Common.Application.Views.ScreenCapture;

public partial class ScreenCaptureActionsView : UserControl
{
    private readonly Timer _selectionTimer;
    private string? _lastSelectedText;

    public ScreenCaptureActionsView()
    {
        InitializeComponent();
        TranslatedTextBox.PropertyChanged += TranslatedTextBox_PropertyChanged;
        ExtractedTextBox.PropertyChanged += TranslatedTextBox_PropertyChanged;
        _selectionTimer = new Timer(1000); // 1,2 sec
        _selectionTimer.AutoReset = false;
        _selectionTimer.Elapsed += OnSelectionTimerElapsed;
        
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

    private void TranslatedTextBox_PropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == TextBox.SelectionStartProperty || e.Property == TextBox.SelectionEndProperty)
        {
            var textBox = sender as TextBox;
            if (textBox != null && textBox.SelectionStart != textBox.SelectionEnd && textBox.Text != null)
            {
                int start = Math.Max(0, Math.Min(textBox.SelectionStart, textBox.SelectionEnd));
                int end = Math.Min(textBox.Text.Length, Math.Max(textBox.SelectionStart, textBox.SelectionEnd));
                int length = Math.Max(0, end - start);

                if (length > 0)
                {
                    _lastSelectedText = textBox.Text.Substring(start, length);
                    _selectionTimer.Stop();
                    _selectionTimer.Start();
                }
            }
            else
            {
                _selectionTimer.Stop();
            }
        }
    }

    private async void OnSelectionTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(_lastSelectedText))
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_lastSelectedText))
                    return;
            
                // Utiliser le Dispatcher pour accéder au ViewModel depuis le thread du timer
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    // Vérifier que le DataContext est bien notre ViewModel
                    if (DataContext is ScreenCaptureActionsViewModel viewModel)
                    {
                        await viewModel.HandleDefinitionLookupAsync(_lastSelectedText);
                    }
                });
                
            }
            catch
            {
                await Dispatcher.UIThread.InvokeAsync(async () =>
                {
                    await MessageBoxManager.GetMessageBoxStandard("Error",
                        "Impossible de récupérer les définitions pour le terme sélectionné. Un").ShowAsPopupAsync(this);
                });
            }
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