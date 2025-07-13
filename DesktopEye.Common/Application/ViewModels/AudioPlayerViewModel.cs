using System;
using System.IO;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DesktopEye.Common.Domain.Features.TextToSpeech;
using DesktopEye.Common.Domain.Features.TextToSpeech.Interfaces;
using DesktopEye.Common.Domain.Models;
using SoundFlow.Components;
using SoundFlow.Providers;
using SoundFlow.Backends.MiniAudio;

namespace DesktopEye.Common.Application.ViewModels
{
       public partial class AudioPlayerViewModel : ObservableObject, IDisposable
    {
        private readonly ITtsOrchestrator _ttsOrchestrator;
        private SoundPlayer? _soundPlayer;
        private StreamDataProvider? _dataProvider;
        private bool _disposedValue;

        [ObservableProperty]
        private string _text = string.Empty;

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
        private string _audioFilePath = string.Empty;

        public AudioPlayerViewModel(ITtsOrchestrator ttsOrchestrator)
        {
            _ttsOrchestrator = ttsOrchestrator ?? throw new ArgumentNullException(nameof(ttsOrchestrator));
        }

        [RelayCommand]
        public async Task GenerateAudio()
        {

            if (string.IsNullOrWhiteSpace(Text) || Language == null)
            {
                return;
            }

            if (IsPlaying)
            {
                _soundPlayer.Stop();
                IsPlaying = false;
            }

            IsGeneratingAudio = true;
            try
            {
                // Générer le fichier audio
                AudioFilePath = await _ttsOrchestrator.GenerateAudioAsync(Text, Language.Value);
                
                // Créer le lecteur
                _soundPlayer = _ttsOrchestrator.CreatePlayer(AudioFilePath);
                if (_soundPlayer != null)
                {
                    _soundPlayer.PlaybackSpeed = CurrentPlaybackSpeed;
                    IsAudioReady = true;
                    _soundPlayer.PlaybackEnded += OnPlaybackEnded;
                    PlayAudio();
                }
            }
            catch (Exception ex)
            {
                ;
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
                return;
            }

            Mixer.Master.AddComponent(_soundPlayer);
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
        
        private void OnPlaybackEnded(object? sender, EventArgs e)
        {
            IsPlaying = false;
            _soundPlayer.Stop();
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