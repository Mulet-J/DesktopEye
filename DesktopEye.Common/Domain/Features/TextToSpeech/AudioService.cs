using System;
using System.IO;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models;
using SoundFlow.Components;

namespace DesktopEye.Common.Domain.Features.TextToSpeech;

public class AudioService
{
    private readonly ITtsService _ttsService;
    
    public AudioService(ITtsService ttsService)
    {
        _ttsService = ttsService;
    }
    
    public async Task<string> GenerateAudioForText(string text, Language language)
    {
        if (string.IsNullOrEmpty(text))
            throw new ArgumentException("Le texte ne peut pas être vide", nameof(text));
            
        return await _ttsService.TextToSpeechAsync(text, language);
    }
    
    public SoundPlayer? CreatePlayer(string audioFilePath)
    {
        if (!File.Exists(audioFilePath))
            throw new FileNotFoundException("Le fichier audio n'existe pas", audioFilePath);
            
        return _ttsService.GetSoundPlayerForFile(audioFilePath);
    }
}