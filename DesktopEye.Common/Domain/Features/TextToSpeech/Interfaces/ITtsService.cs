using System;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Models;
using SoundFlow.Components;

namespace DesktopEye.Common.Domain.Features.TextToSpeech.Interfaces;

public interface ITtsService : IDisposable
{
    
    Task<string> TextToSpeechAsync(string text, Language language);
    
    SoundPlayer? GetSoundPlayerForFile(string audioFilePath);
}