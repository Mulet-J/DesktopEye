using KokoroSharp;
using KokoroSharp.Core;
using KokoroSharp.Utilities;

namespace DesktopEye.Common.Tests.Services;

public class KokoroTtsServiceTest
{
    [Fact]
    public async Task GetAudioFromTextAsync_ShouldReturnAudio_WhenValidTextIsProvided()
    {
        var _synthesizer = new KokoroWavSynthesizer("C:\\Users\\mtraore\\Downloads\\kokoro.onnx");
        var res = await _synthesizer.SynthesizeAsync(
            "Salut ! Je suis un test de synthèse vocale avec KokoroSharp. ",
            KokoroVoiceManager.GetVoice("ff_siwis"));
        _synthesizer.SaveAudioToFile(res, "C:\\Users\\mtraore\\Downloads\\res.wav");
    }
}