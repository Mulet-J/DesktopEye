using System.Linq;
using System.Threading;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;
using DesktopEye.Common.Infrastructure.Models;
using DesktopEye.Common.Infrastructure.Services.TrainedModel;
using Microsoft.Extensions.Logging;

namespace DesktopEye.Common.Infrastructure.Services.TTS;

using System;
using System.IO;
using System.Threading.Tasks;
using KokoroSharp;
using KokoroSharp.Utilities;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;

public class KokoroTtsService : ITtsService, ILoadable
{
    private readonly string _audioFolderPath;
    private readonly KokoroWavSynthesizer _synthesizer;
    private MiniAudioEngine _miniAudioEngine;
    private ModelRegistry _modelRegistry = new ModelRegistry();
    private bool _disposedValue;
    private bool _soundFlowInitialized = false;
    private readonly ILogger<KokoroTtsService> _logger;
    
    private Model NTextCatModel =>
        _modelRegistry.DefaultModels.FirstOrDefault(model => model.ModelName == "NTextCat.xml") ??
        throw new InvalidOperationException("NTextCat model not found in registry");


    public KokoroTtsService(string audioFolderPath, string modelPath, ILogger<KokoroTtsService> logger)
    {
        _audioFolderPath = audioFolderPath;
        _logger = logger;
        _synthesizer = new KokoroWavSynthesizer(modelPath);

        Directory.CreateDirectory(_audioFolderPath);

        // Initialisation de LibVLC
        InitializeSoundFlow();
    }

    private void InitializeSoundFlow()
    {
        try
        {
            _miniAudioEngine = new MiniAudioEngine(48000, Capability.Playback);
            _soundFlowInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Erreur lors de l'initialisation de SoundFlow : {ex.Message}");
            _soundFlowInitialized = false;
        }
    }

    public async Task<string> TextToSpeechAsync(string text, Language language)
    {
        var voiceId = GetVoiceForLanguage(language);
        var fileName = $"TTS_{language}_{Guid.NewGuid()}.wav";
        var filePath = Path.Combine(_audioFolderPath, fileName);

        var voice = KokoroVoiceManager.GetVoice(voiceId);
        var audioData = await _synthesizer.SynthesizeAsync(text, voice);
        _synthesizer.SaveAudioToFile(audioData, filePath);

        return filePath;
    }

    public SoundPlayer GetSoundPlayerForFile(string audioFilePath)
    {
        if (!_soundFlowInitialized)
        {
            Console.WriteLine("SoundFlow n'est pas initialisé. Impossible de créer un Media.");
            return null;
        }
        
        var dataProvider = new StreamDataProvider(File.OpenRead(audioFilePath));
        var player = new SoundPlayer(dataProvider);

        return player;
    }

    private string GetVoiceForLanguage(Language language) => language switch
    {
        Language.French => "ff_siwis",    // Voix française
        Language.English => "af_heart",   // Voix anglaise
        Language.Spanish => "em_alex",    // Voix espagnole
        Language.German => "de_css10",    // Voix allemande
        Language.Japanese => "jf_alpha",  // Voix japonaise
        _ => "af_heart"                   // Par défaut
    };

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposedValue)
        {
            if (disposing)
            {
                _miniAudioEngine?.Dispose();
                _synthesizer?.Dispose();
            }

            _disposedValue = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }

    public async Task<bool> LoadRequiredAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        /*return await Task.Run(() => LoadRequired(modelName), cancellationToken);*/
        throw new NotImplementedException();
    }
}