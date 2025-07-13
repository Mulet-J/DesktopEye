using System.Collections.Generic;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.TextToSpeech;
using SoundFlow.Components;

namespace DesktopEye.Common.Domain.Features.TextToSpeech.Interfaces;

public interface ITtsOrchestrator
{
    /// <summary>
    /// Génère un fichier audio à partir d'un texte et d'une langue
    /// </summary>
    /// <param name="text">Le texte à convertir en audio</param>
    /// <param name="language">La langue du texte</param>
    /// <returns>Le chemin vers le fichier audio généré</returns>
    Task<string> GenerateAudioAsync(string text, Language language);
    
        
    /// <summary>
    /// Crée un lecteur audio pour un fichier audio
    /// </summary>
    /// <param name="audioFilePath">Le chemin vers le fichier audio</param>
    /// <returns>Un lecteur audio configuré</returns>
    SoundPlayer? CreatePlayer(string audioFilePath);
    
    
}