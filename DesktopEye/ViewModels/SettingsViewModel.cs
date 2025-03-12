using System;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DesktopEye.ViewModels;

public partial class SettingsViewModel : ViewModelBase
{
    // Paramètres de sauvegarde
    [ObservableProperty]
    private string _saveDirectory;
    
    [ObservableProperty]
    private string _defaultFileName;
    
    [ObservableProperty]
    private string _fileFormat;
    
    // Options d'interface
    [ObservableProperty]
    private bool _showNotifications;
    
    [ObservableProperty]
    private bool _startWithSystem;
    
    [ObservableProperty]
    private bool _minimizeToTray;
    
    // Options de capture
    [ObservableProperty]
    private bool _includeCursor;
    
    [ObservableProperty]
    private bool _captureWindowUnderCursor;
    
    [ObservableProperty]
    private int _captureDelaySeconds;
    
    // Raccourcis clavier
    [ObservableProperty]
    private string _fullScreenHotkey;
    
    [ObservableProperty]
    private string _regionCaptureHotkey;
    
    public SettingsViewModel()
    {
        // Initialiser avec les valeurs par défaut
        _saveDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "Screenshots");
            
        _defaultFileName = "DesktopEye_Capture_{timestamp}";
        _fileFormat = "PNG";
        _showNotifications = true;
        _startWithSystem = false;
        _minimizeToTray = true;
        _includeCursor = false;
        _captureWindowUnderCursor = false;
        _captureDelaySeconds = 0;
        _fullScreenHotkey = "Print";
        _regionCaptureHotkey = "Ctrl+Shift+Print";
        
        // Créer le répertoire de sauvegarde s'il n'existe pas
        if (!Directory.Exists(_saveDirectory))
        {
            Directory.CreateDirectory(_saveDirectory);
        }
    }
    
    [RelayCommand]
    private void BrowseSaveDirectory()
    {
        // Cette méthode serait implémentée pour ouvrir un sélecteur de dossier
        // Mais pour l'instant, nous utilisons juste un placeholder
    }
    
    [RelayCommand]
    private void SaveSettings()
    {
        // Sauvegarder les paramètres (à implémenter)
    }
    
    [RelayCommand]
    private void ResetToDefaults()
    {
        // Réinitialiser tous les paramètres aux valeurs par défaut
        SaveDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "Screenshots");
        DefaultFileName = "DesktopEye_Capture_{timestamp}";
        FileFormat = "PNG";
        ShowNotifications = true;
        StartWithSystem = false;
        MinimizeToTray = true;
        IncludeCursor = false;
        CaptureWindowUnderCursor = false;
        CaptureDelaySeconds = 0;
        FullScreenHotkey = "Print";
        RegionCaptureHotkey = "Ctrl+Shift+Print";
    }
}
