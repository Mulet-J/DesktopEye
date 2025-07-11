using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;
using DesktopEye.Common.Infrastructure.Models;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using DesktopEye.Common.Infrastructure.Services.TrainedModel;
using KokoroSharp;
using KokoroSharp.Utilities;
using Microsoft.Extensions.Logging;
using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;

namespace DesktopEye.Common.Domain.Features.TextToSpeech;

public class KokoroTtsService : ITtsService, ILoadable
{
    private readonly ILogger<KokoroTtsService> _logger;
    private readonly IPathService _pathService;
    
    private KokoroWavSynthesizer? _synthesizer;
    private MiniAudioEngine? _miniAudioEngine;
    private readonly ModelRegistry _modelRegistry = new();
    
    private bool _disposedValue;
    private readonly string _modelDirectory;
    private bool _soundFlowInitialized = false;

    private Model KokoroTtsModel =>
        _modelRegistry.DefaultModels.FirstOrDefault(model => model.ModelName == "kokoro-v1.0.onnx") ??
        throw new InvalidOperationException("kokoro model not found in registry");
    
    public KokoroTtsService(ILogger<KokoroTtsService> logger, IPathService pathService)
    {
        _logger = logger;
        _pathService = pathService;
        _modelDirectory = Path.Combine(_pathService.ModelsDirectory, KokoroTtsModel.ModelFolderName);
    }
    
    private void InitializeSoundFlow()
    {
        try
        {
            _synthesizer = new KokoroWavSynthesizer(Path.Combine(_modelDirectory, KokoroTtsModel.ModelName));
            Directory.CreateDirectory(Path.Combine(_modelDirectory,"Audios"));
            _miniAudioEngine = new MiniAudioEngine(48000, Capability.Playback);
            _soundFlowInitialized = true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($@"Erreur lors de l'initialisation de SoundFlow : {ex.Message}");
            _soundFlowInitialized = false;
        }
    }

    public async Task<string> TextToSpeechAsync(string text, Language language)
    {
        var voiceId = GetVoiceForLanguage(language);
        var fileName = $"TTS_{language}_{Guid.NewGuid()}.wav";
        var filePath = Path.Combine(Path.Combine(_modelDirectory,"Audios"), fileName);

        var voice = KokoroVoiceManager.GetVoice(voiceId);
        var audioData = await _synthesizer!.SynthesizeAsync(text, voice);
        _synthesizer.SaveAudioToFile(audioData, filePath);

        return filePath;
    }

    public SoundPlayer? GetSoundPlayerForFile(string audioFilePath)
    {
        if (!_soundFlowInitialized)
        {
            Console.WriteLine(@"SoundFlow n'est pas initialisé. Impossible de créer un Media.");
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

    public Task<bool> LoadRequiredAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        if (_soundFlowInitialized) return Task.FromResult(true);

        _logger.LogInformation("Loading Kokoro TTS model from: {ModelPath}", _modelDirectory);
        
        try
        {
            InitializeSoundFlow();
            if (!_soundFlowInitialized)
            {
                _logger.LogError("Failed to initialize SoundFlow for Kokoro TTS.");
                return Task.FromResult(false);
            }
            
            _logger.LogInformation("Kokoro TTS model loaded successfully.");
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load Kokoro TTS model.");
            return Task.FromResult(false);
        }
    }
}