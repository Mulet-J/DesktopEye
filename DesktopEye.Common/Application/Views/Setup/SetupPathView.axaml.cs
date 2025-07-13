using System;
using System.IO;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using DesktopEye.Common.Application.ViewModels.Setup;

namespace DesktopEye.Common.Application.Views.Setup;

public partial class SetupPathView : UserControl
{
    public SetupPathView()
    {
        InitializeComponent();
    }

    private async void SelectFolderButton_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            // Get the top-level window
            var topLevel = TopLevel.GetTopLevel(this); // 'this' should be your control/window

            if (topLevel != null)
            {
                // Create and configure the folder picker options
                var folderOptions = new FolderPickerOpenOptions
                {
                    Title = "Select a folder",
                    AllowMultiple = false
                };

                // Show the folder picker dialog
                var result = await topLevel.StorageProvider.OpenFolderPickerAsync(folderOptions);

                var folderPath = "";
                if (result.Count > 0)
                {
                    // User selected a folder
                    var selectedFolder = result[0];
                    folderPath = Path.Combine(selectedFolder.Path.LocalPath, "DesktopEye/");
                }

                if (DataContext is SetupPathViewModel viewModel && !string.IsNullOrWhiteSpace(folderPath))
                    viewModel.LocalAppDataFolder = folderPath;
            }
        }
        catch (Exception ex)
        {
        }
    }
}