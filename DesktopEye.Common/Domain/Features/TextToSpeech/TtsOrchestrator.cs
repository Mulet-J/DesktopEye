using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Features.TextClassification.Interfaces;
using DesktopEye.Common.Domain.Features.TextToSpeech.Interfaces;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.TextClassification;
using DesktopEye.Common.Domain.Models.TextToSpeech;
using DesktopEye.Common.Infrastructure.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SoundFlow.Components;

namespace DesktopEye.Common.Domain.Features.TextToSpeech
{
    public class TtsOrchestrator : ServiceOrchestrator<ITtsService, TtsType>, ITtsOrchestrator
    {
        private readonly ILogger<TtsOrchestrator> _logger;
        private readonly IServiceProvider _serviceProvider;
        private ITtsService? _currentService;
        private bool _disposedValue;

        public TtsType CurrentServiceType { get; private set; }
        
        public TtsOrchestrator(ILogger<TtsOrchestrator> logger, Bugsnag.IClient bugsnag, IServiceProvider serviceProvider) : base(serviceProvider, bugsnag, logger)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            CurrentServiceType = TtsType.KokoroTts;
            
            // Initialize the default service type
            SetServiceType(CurrentServiceType);
        }

        public void SetServiceType(TtsType serviceType)
        {
            try
            {
                _logger.LogInformation("Changement de service TTS vers {ServiceType}", serviceType);
                
                // Dispose of the current service if it implements IDisposable
                if (_currentService is IDisposable disposable && _currentService != null)
                {
                    disposable.Dispose();
                }

                // Get the new service instance based on the service type
                _currentService = _serviceProvider.GetKeyedService<ITtsService>(serviceType);
                
                if (_currentService == null)
                {
                    throw new InvalidOperationException($"Service TTS non trouvé pour le type {serviceType}");
                }
                
                CurrentServiceType = serviceType;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement de service TTS");
                throw;
            }
        }

        public  async Task<string> GenerateAudioAsync(string text, Language language)
        {
            EnsureServiceInitialized();
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Le texte ne peut pas être vide", nameof(text));
            
            return await _currentService.TextToSpeechAsync(text, language);        }

        public async Task<string> TextToSpeechAsync(string text, Language language)
        {
            EnsureServiceInitialized();
            return await _currentService!.TextToSpeechAsync(text, language);
        }

        public SoundPlayer? GetSoundPlayerForFile(string audioFilePath)
        {
            EnsureServiceInitialized();
            return _currentService!.GetSoundPlayerForFile(audioFilePath);
        }
        
        public async Task<string> GenerateAudioForText(string text, Language language)
        {
            EnsureServiceInitialized();
            if (string.IsNullOrEmpty(text))
                throw new ArgumentException("Le texte ne peut pas être vide", nameof(text));
            
            return await _currentService.TextToSpeechAsync(text, language);
        }
    
        
        public SoundPlayer? CreatePlayer(string audioFilePath)
        {
            EnsureServiceInitialized();
            if (!File.Exists(audioFilePath))
                throw new FileNotFoundException("Le fichier audio n'existe pas", audioFilePath);
            
            return _currentService.GetSoundPlayerForFile(audioFilePath);
        }

        private void EnsureServiceInitialized()
        {
            if (_currentService == null)
            {
                SetServiceType(CurrentServiceType);
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposedValue)
            {
                if (disposing && _currentService is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                _currentService = null;
                _disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(disposing: true);
            GC.SuppressFinalize(this);
        }

        protected override TtsType GetDefaultServiceType()
        {
            return TtsType.KokoroTts;
        }
    }
}