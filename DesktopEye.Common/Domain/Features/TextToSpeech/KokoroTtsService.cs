using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Features.TextToSpeech.Interfaces;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Infrastructure.Configuration.Interfaces;
using DesktopEye.Common.Infrastructure.Models;
using DesktopEye.Common.Infrastructure.Services.ApplicationPath;
using DesktopEye.Common.Infrastructure.Services.TrainedModel;
using KokoroSharp;
using KokoroSharp.Processing;
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

    private void EnsureSynthesizerInitialized()
    {
        if (_synthesizer == null)
        {
            
            var model = KokoroTtsModel;
            string modelPath = Path.Combine(_modelDirectory, model.ModelName);
            
            _logger.LogInformation("Initialisation du synthétiseur KokoroTts avec le modèle dans {ModelPath}", modelPath);
            
            
            _synthesizer = new KokoroWavSynthesizer(modelPath);
        }
    }
    
    private void EnsureMiniAudioEngineInitialized()
    {
        if (_miniAudioEngine == null)
        {
            _logger.LogInformation("Initialisation du moteur audio MiniAudioEngine");
            _miniAudioEngine = new MiniAudioEngine(48000, Capability.Playback);
        }
    }
    
    
    private void  InitializeSoundFlow()
    {
        try
        {
            // Create output directory if it doesn't exist
            if (!Directory.Exists(Path.Combine(_modelDirectory,"Audios")))
            {
                Directory.CreateDirectory(Path.Combine(_modelDirectory,"Audios"));
            }
            
            _miniAudioEngine = new MiniAudioEngine(48000, Capability.Playback);
            _synthesizer = new KokoroWavSynthesizer(Path.Combine(_modelDirectory, KokoroTtsModel.ModelName));
            _soundFlowInitialized = true;
        }
        catch (Exception ex)
        {
            _logger.LogError($@"Erreur lors de l'initialisation de SoundFlow : {ex.Message}");
            _soundFlowInitialized = false;
        }
    }

public async Task<string> TextToSpeechAsync(string text, Language language)
{
    try 
    {
        EnsureSynthesizerInitialized();
        
        // Nettoyage et validation du texte
        text = text?.Trim() ?? string.Empty;
        if (string.IsNullOrEmpty(text))
        {
            _logger.LogWarning("Tentative de synthèse avec un texte vide");
            return string.Empty;
        }

        var voiceId = GetVoiceForLanguage(language);
        var fileName = $"TTS_{language}_{Guid.NewGuid()}.wav";
        var filePath = Path.Combine(Path.Combine(_modelDirectory, "Audios"), fileName);

        var voice = KokoroVoiceManager.GetVoice(voiceId);
        
        // Configuration plus restrictive pour éviter les dépassements
        var config = new KokoroTTSPipelineConfig(new DefaultSegmentationConfig
        {
            MaxFirstSegmentLength = 100,  // Réduire significativement la taille des segments
            MaxSecondSegmentLength = 100
        })
        {
            // Fonction de segmentation personnalisée pour découper en plus petits morceaux
            SegmentationFunc = tokens =>
            {
                var segments = new List<int[]>();
                const int maxLength = 100;
                
                for (int i = 0; i < tokens.Length; i += maxLength)
                {
                    var length = Math.Min(maxLength, tokens.Length - i);
                    var segment = new int[length];
                    Array.Copy(tokens, i, segment, 0, length);
                    segments.Add(segment);
                }
                
                return segments;
            },
            SecondsOfPauseBetweenProperSegments = new PauseAfterSegmentStrategy(0.2f)
        };

        _logger.LogInformation("Début de la synthèse vocale pour {Length} caractères", text.Length);
        var audioData = await _synthesizer!.SynthesizeAsync(text, voice, config);
        _synthesizer.SaveAudioToFile(audioData, filePath);
        _logger.LogInformation("Synthèse vocale terminée, fichier sauvegardé : {FilePath}", filePath);

        return filePath;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Erreur lors de la synthèse vocale: {Message}", ex.Message);
        throw;
    }
}

    public SoundPlayer? GetSoundPlayerForFile(string audioFilePath)
    {

        EnsureMiniAudioEngineInitialized();
        var dataProvider = new StreamDataProvider(File.OpenRead(audioFilePath));
        var player = new SoundPlayer(dataProvider);

        return player;
    }

    private string GetVoiceForLanguage(Language language) => language switch
    {
        Language.French => "ff_siwis",    // Voix française
        Language.English => "af_heart",   // Voix anglaise
        Language.Spanish => "em_alex",    // Voix espagnole
        Language.Japanese => "jf_alpha",  // Voix japonaise
        Language.Italian => "if_sara",    // Voix italienne
        Language.Portuguese => "pf_dora", // Voix portugaise
        Language.Chinese => "zf_xiaoxiao", // Voix chinoise
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
            //InitializeSoundFlow();
            if (!Directory.Exists(Path.Combine(_modelDirectory,"Audios")))
            {
                Directory.CreateDirectory(Path.Combine(_modelDirectory,"Audios"));
            }
            
            _synthesizer = new KokoroWavSynthesizer(Path.Combine(_modelDirectory, KokoroTtsModel.ModelName));
            _soundFlowInitialized = true;
            
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