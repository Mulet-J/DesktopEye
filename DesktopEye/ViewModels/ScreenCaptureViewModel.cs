using System;
using System.IO;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Helpers;
using DesktopEye.Services.ScreenCaptureService;
using SkiaSharp;

namespace DesktopEye.ViewModels;

public partial class ScreenCaptureViewModel : ViewModelBase
{
    private readonly IScreenCaptureService _screenCaptureService;
    [ObservableProperty] private Bitmap? _bitmap;
    
    // Propriétés pour la sélection
    [ObservableProperty] private int _selectionX;
    [ObservableProperty] private int _selectionY;
    [ObservableProperty] private int _selectionWidth;
    [ObservableProperty] private int _selectionHeight;
    
    // État de l'application
    [ObservableProperty] private bool _isCapturing;
    [ObservableProperty] private string _statusMessage = "";

    // Stockage de l'image originale pour le traitement
    private SKBitmap? _originalSKBitmap;

    public ScreenCaptureViewModel(IScreenCaptureService screenCaptureService)
    {
        _screenCaptureService = screenCaptureService;
    }
    
    public void Initialize()
    {
        CaptureFullScreen();
    }
    
    [RelayCommand]
    private void CaptureFullScreen()
    {
        try
        {
            IsCapturing = true;
            StatusMessage = "Capturing screen...";
            
            _originalSKBitmap = _screenCaptureService.CaptureScreen();
            Bitmap = _originalSKBitmap.ToAvaloniaBitmap();
            
            StatusMessage = "Screen captured successfully";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error capturing screen: {ex.Message}";
        }
        finally
        {
            IsCapturing = false;
        }
    }
    
    [RelayCommand]
    private void CaptureSelection()
    {
        if (_originalSKBitmap == null || SelectionWidth <= 0 || SelectionHeight <= 0)
        {
            StatusMessage = "No valid selection to capture";
            return;
        }
        
        try
        {
            IsCapturing = true;
            StatusMessage = "Capturing selection...";
            
            // Créer un rectangle SKRect pour la sélection
            var rect = new SKRectI(
                SelectionX, 
                SelectionY, 
                SelectionX + SelectionWidth, 
                SelectionY + SelectionHeight
            );
            
            // Extraire la région sélectionnée
            using (var croppedBitmap = new SKBitmap(SelectionWidth, SelectionHeight))
            {
                // Extraire la région sélectionnée
                if (_originalSKBitmap.ExtractSubset(croppedBitmap, rect))
                {
                    // Sauvegarder l'image
                    SaveCapturedImage(croppedBitmap);
                    StatusMessage = "Selection captured successfully";
                }
                else
                {
                    StatusMessage = "Failed to extract selection";
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error capturing selection: {ex.Message}";
        }
        finally
        {
            IsCapturing = false;
        }
    }
    
    private void SaveCapturedImage(SKBitmap bitmap)
    {
        try
        {
            // Créer un nom de fichier avec un timestamp
            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            string fileName = $"DesktopEye_Capture_{timestamp}.png";
            
            // Chemin pour sauvegarder (dossier Images/Screenshots)
            string picturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            string screenshotsFolder = Path.Combine(picturesFolder, "Screenshots");
            
            // Créer le dossier s'il n'existe pas
            if (!Directory.Exists(screenshotsFolder))
            {
                Directory.CreateDirectory(screenshotsFolder);
            }
            
            string filePath = Path.Combine(screenshotsFolder, fileName);
            
            // Encoder et sauvegarder
            using (var image = SKImage.FromBitmap(bitmap))
            using (var data = image.Encode(SKEncodedImageFormat.Png, 100))
            using (var stream = File.OpenWrite(filePath))
            {
                data.SaveTo(stream);
            }
            
            StatusMessage = $"Image saved to {filePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error saving image: {ex.Message}";
        }
    }
    
    // Commandes pour les outils d'annotation
    [RelayCommand]
    private void AddText()
    {
        // Implémenter l'ajout de texte à la capture
        StatusMessage = "Text tool selected";
    }
    
    [RelayCommand]
    private void AddPoint()
    {
        // Implémenter l'ajout d'un point à la capture
        StatusMessage = "Point tool selected";
    }
    
    [RelayCommand]
    private void AddArrow()
    {
        // Implémenter l'ajout d'une flèche à la capture
        StatusMessage = "Arrow tool selected";
    }
    
    [RelayCommand]
    private void Zoom()
    {
        // Implémenter le zoom sur la capture
        StatusMessage = "Zoom tool selected";
    }
    
    [RelayCommand]
    private void AddShape()
    {
        // Implémenter l'ajout d'une forme à la capture
        StatusMessage = "Shape tool selected";
    }
    
    [RelayCommand]
    private void Cancel()
    {
        // Annuler la capture et fermer la fenêtre
        // La fermeture elle-même sera gérée par le code-behind
        StatusMessage = "Capture cancelled";
    }
}