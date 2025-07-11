using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.Domain.Features.TextToSpeech;
using DesktopEye.Common.Domain.Models;
using SoundFlow.Components;
using SoundFlow.Providers;
using SoundFlow.Backends.MiniAudio;

namespace DesktopEye.Common.Application.ViewModels
{
    public partial class AudioPlayerViewModel : ObservableObject, IDisposable
    {
        private ITtsService _ttsService;
        private SoundPlayer _soundPlayer;
        private StreamDataProvider _dataProvider;
        private bool _disposedValue;

        [ObservableProperty]
        private string _text;

        [ObservableProperty]
        private Language? _language;

        [ObservableProperty]
        private bool _isAudioReady;

        [ObservableProperty]
        private bool _isPlaying;

        [ObservableProperty]
        private bool _isGeneratingAudio;

        [ObservableProperty]
        private float _currentPlaybackSpeed = 1.0f;

        [ObservableProperty]
        private string _audioFilePath;

        public ITtsService TtsService
        {
            get => _ttsService;
            set => SetProperty(ref _ttsService, value);
        }

        [RelayCommand]
        public async Task GenerateAudio()
        {
            Console.WriteLine($"GenerateAudio appelé - Text: '{Text}', Language: {Language}, TtsManager: {TtsService != null}");

            if (string.IsNullOrWhiteSpace(Text))
            {
                Console.WriteLine("Erreur: Texte vide ou null");
                return;
            }

            if (!Language.HasValue)
            {
                Console.WriteLine("Erreur: Langue non définie");
                return;
            }

            if (TtsService == null)
            {
                Console.WriteLine("Erreur: TtsManager null");
                return;
            }

            try
            {
                Console.WriteLine("Début de génération audio...");
                IsGeneratingAudio = true;
                CleanupCurrentMedia();

                IsAudioReady = false;
                IsPlaying = false;
                AudioFilePath = null;

                var filePath = await TtsService.TextToSpeechAsync(Text, Language.Value);
                Console.WriteLine($"Fichier généré : {filePath}");

                if (!File.Exists(filePath))
                {
                    Console.WriteLine($"Le fichier audio n'existe pas : {filePath}");
                    return;
                }

                AudioFilePath = filePath;

                // Créer le SoundPlayer
                _dataProvider = new StreamDataProvider(File.OpenRead(filePath));
                _soundPlayer = new SoundPlayer(_dataProvider);

                _soundPlayer.PlaybackSpeed = CurrentPlaybackSpeed;

                Mixer.Master.AddComponent(_soundPlayer);

                IsAudioReady = true;
                Console.WriteLine("MediaPlayer créé et prêt");

                // Démarrer automatiquement la lecture
                PlayAudio();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erreur lors de la génération audio : {ex.Message}");
                Console.WriteLine($"StackTrace : {ex.StackTrace}");
            }
            finally
            {
                IsGeneratingAudio = false;
            }
        }

        [RelayCommand]
        public void PlayAudio()
        {
            if (!IsAudioReady || _soundPlayer == null)
            {
                Console.WriteLine("Audio non prêt ou player null");
                return;
            }

            Console.WriteLine("Lecture audio en cours...");
            _soundPlayer.Play();
            IsPlaying = true;
        }

        [RelayCommand]
        public void PauseAudio()
        {
            if (!IsPlaying || _soundPlayer == null)
                return;

            _soundPlayer.Pause();
            IsPlaying = false;
        }

        [RelayCommand]
        public void StopAudio()
        {
            if (_soundPlayer == null)
                return;

            _soundPlayer.Stop();
            IsPlaying = false;
        }

        partial void OnCurrentPlaybackSpeedChanged(float value)
        {
            if (_soundPlayer != null)
            {
                _soundPlayer.PlaybackSpeed = value;
            }
        }

        private void CleanupCurrentMedia()
        {
            /*if (_soundPlayer != null)
            {
                // Supprimer la ligne problématique EndReached
                // _soundPlayer.EndReached -= OnAudioEndReached;
                
                _soundPlayer.Stop();
                Mixer.Master.RemoveComponent(_soundPlayer);
                
                // Gérer la disposal correctement
                if (_soundPlayer is IDisposable disposablePlayer)
                {
                    disposablePlayer.Dispose();
                }
                _soundPlayer = null;
            }*/

            if (_dataProvider != null)
            {
                _dataProvider.Dispose();
                _dataProvider = null;
            }
        }

        public void Dispose()
        {
            if (!_disposedValue)
            {
                CleanupCurrentMedia();
                _disposedValue = true;
            }
        }
    }
    
}