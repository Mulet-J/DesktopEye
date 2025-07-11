using KokoroSharp;
using KokoroSharp.Utilities;

namespace DesktopEye.Common.Tests.Services.TTS;

public class KokoroTtsServiceTest
{
    [Fact]
    public async Task GetAudioFromTextAsync_ShouldReturnAudio_WhenValidTextIsProvided()
    {
        var _synthesizer = new KokoroWavSynthesizer("/Users/remi.marques/Downloads/kokoro-v1.0.onnx");
        var res = await _synthesizer.SynthesizeAsync(
            "Salut ! Je suis un test de synthèse vocale avec KokoroSharp. ",
            KokoroVoiceManager.GetVoice("ff_siwis"));
        _synthesizer.SaveAudioToFile(res, "/Users/remi.marques/res.wav");
    }
}