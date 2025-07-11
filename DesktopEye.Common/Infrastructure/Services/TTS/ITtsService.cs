using DesktopEye.Common.Domain.Models;

namespace DesktopEye.Common.Infrastructure.Services.TTS;

using System;
using System.Threading.Tasks;
using SoundFlow.Components;

public interface ITtsService : IDisposable
{
    // Conserver uniquement la méthode de synthèse vocale
    Task<string> TextToSpeechAsync(string text, Language language);
    
    // Ajouter une méthode pour récupérer un MediaPlayer configuré
    SoundPlayer GetSoundPlayerForFile(string audioFilePath);
}