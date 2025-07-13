using System.Collections.Generic;
using System.Threading.Tasks;
using DesktopEye.Common.Domain.Features.TextToSpeech.Interfaces;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.TextToSpeech;
using SoundFlow.Components;

namespace DesktopEye.Common.Application.ViewModels.ScreenCapture.Design;

public class DesignTtsOrchestrator : ITtsOrchestrator
{
    public Task<string> GenerateAudioAsync(string text, Language language)
    {
        return Task.FromResult("dummy.wav");
    }

    public SoundPlayer? CreatePlayer(string audioFilePath)
    {
        return null;
    }

    public TtsType CurrentServiceType => TtsType.KokoroTts;

    public bool SetCurrentService(TtsType serviceType)
    {
        return true;
    }

    public IEnumerable<string> GetAvailableVoices(Language language)
    {
        return new[] { "Voice 1", "Voice 2" };
    }
}