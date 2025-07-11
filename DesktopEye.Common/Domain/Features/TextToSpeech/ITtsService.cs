using System;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models;
using SoundFlow.Components;

namespace DesktopEye.Common.Domain.Features.TextToSpeech;

public interface ITtsService : IDisposable
{
    // Conserver uniquement la méthode de synthèse vocale
    Task<string> TextToSpeechAsync(string text, Language language);
    
    // Ajouter une méthode pour récupérer un MediaPlayer configuré
    SoundPlayer? GetSoundPlayerForFile(string audioFilePath);
}