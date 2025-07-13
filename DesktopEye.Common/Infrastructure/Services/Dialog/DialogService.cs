using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Microsoft.Extensions.Logging;

namespace DesktopEye.Common.Infrastructure.Services.Dialog;

/// <summary>
/// Service pour afficher des boîtes de dialogue à l'utilisateur.
/// </summary>
public class DialogService : IDialogService
{
    private readonly ILogger<DialogService> _logger;
    private readonly Bugsnag.IClient _bugsnag;

    /// <summary>
    /// Initialise une nouvelle instance du service de dialogue.
    /// </summary>
    /// <param name="logger">Le logger pour enregistrer les erreurs.</param>
    /// <param name="bugsnag">Le client Bugsnag pour rapporter les exceptions.</param>
    public DialogService(ILogger<DialogService> logger, Bugsnag.IClient bugsnag)
    {
        _logger = logger;
        _bugsnag = bugsnag;
    }

    /// <summary>
    /// Affiche une boîte de dialogue stylisée avec le titre et le contenu spécifiés.
    /// </summary>
    /// <param name="title">Le titre de la boîte de dialogue.</param>
    /// <param name="message">Le contenu de la boîte de dialogue.</param>
    /// <returns>Une tâche représentant l'opération asynchrone.</returns>
    public async Task ShowMessageBoxAsync(string title, string message)
    {
        try
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                // Obtenir la fenêtre principale
                var mainWindow = Avalonia.Application.Current?.ApplicationLifetime is
                    Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime lifetime
                    ? lifetime.MainWindow
                    : null;

                // Créer une DockPanel pour organiser les contrôles
                var dockPanel = new DockPanel
                {
                    LastChildFill = false
                };

                // Ajouter le bouton Fermer en bas
                var closeButton = new Button
                {
                    Content = "Fermer",
                    HorizontalAlignment = HorizontalAlignment.Center,
                    HorizontalContentAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(10),
                    Width = 100,
                    Height = 30,
                    Background = new SolidColorBrush(Colors.Gray),
                    Foreground = new SolidColorBrush(Colors.Black)
                };

                DockPanel.SetDock(closeButton, Dock.Bottom);
                dockPanel.Children.Add(closeButton);

                // Ajouter le ScrollViewer avec le texte
                var scrollViewer = new ScrollViewer
                {
                    Margin = new Thickness(10),
                    VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                    Content = new TextBlock
                    {
                        Text = message,
                        TextWrapping = TextWrapping.Wrap,
                        Margin = new Thickness(10),
                        Foreground = new SolidColorBrush(Colors.White)
                    }
                };

                DockPanel.SetDock(scrollViewer, Dock.Top);
                dockPanel.Children.Add(scrollViewer);

                // Créer la fenêtre
                var dialog = new Window
                {
                    Title = title,
                    MinWidth = 400,
                    MinHeight = 200,
                    MaxWidth = 800,
                    MaxHeight = 600,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    ShowInTaskbar = false,
                    CanResize = true,
                    Background = new SolidColorBrush(Colors.Black),
                    Content = dockPanel,
                    SystemDecorations = SystemDecorations.Full
                };

                // Ajouter un gestionnaire pour le bouton fermer
                closeButton.Click += (s, e) => dialog.Close();

                // Afficher la fenêtre
                if (mainWindow != null)
                {
                    dialog.Show(mainWindow);
                }
                else
                {
                    dialog.Show();
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'affichage de la boîte de dialogue: {Message}", ex.Message);
            _bugsnag.Notify(ex);

            // Afficher une boîte de dialogue de secours plus simple
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                var simpleDialog = new Window
                {
                    Title = "Erreur",
                    Width = 400,
                    Height = 200,
                    Content = new TextBlock
                    {
                        Text = $"Une erreur s'est produite lors de l'affichage du message.\n{ex.Message}",
                        Margin = new Thickness(20),
                        TextWrapping = TextWrapping.Wrap
                    },
                    WindowStartupLocation = WindowStartupLocation.CenterScreen
                };
                simpleDialog.Show();
            });
        }
    }
}