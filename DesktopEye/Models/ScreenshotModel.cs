using System;
using System.IO;

namespace DesktopEye.Models;

public class ScreenshotModel
{
    // Propriétés de base
    public string FilePath { get; set; }
    public DateTime CaptureTime { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    
    // Métadonnées supplémentaires
    public string Name { get; set; }
    public string Format { get; set; } = "PNG";
    public bool HasAnnotations { get; set; }
    
    // Coordonnées de capture pour les sélections de région
    public int X { get; set; }
    public int Y { get; set; }
    
    // Constructeur par défaut
    public ScreenshotModel()
    {
        CaptureTime = DateTime.Now;
        Name = $"Capture_{CaptureTime:yyyyMMdd_HHmmss}";
        FilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "Screenshots",
            $"{Name}.png");
    }
    
    // Constructeur avec chemin personnalisé
    public ScreenshotModel(string filePath)
    {
        FilePath = filePath;
        CaptureTime = DateTime.Now;
        Name = Path.GetFileNameWithoutExtension(filePath);
        Format = Path.GetExtension(filePath).TrimStart('.').ToUpperInvariant();
    }
    
    // Méthode pour générer un chemin de fichier basé sur un timestamp
    public static string GenerateFilePath(string format = "png")
    {
        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string fileName = $"DesktopEye_Capture_{timestamp}.{format}";
        
        string picturesFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
        string screenshotsFolder = Path.Combine(picturesFolder, "Screenshots");
        
        // Créer le dossier s'il n'existe pas
        if (!Directory.Exists(screenshotsFolder))
        {
            Directory.CreateDirectory(screenshotsFolder);
        }
        
        return Path.Combine(screenshotsFolder, fileName);
    }
}