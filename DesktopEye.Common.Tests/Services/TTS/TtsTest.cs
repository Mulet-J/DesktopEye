using SoundFlow.Backends.MiniAudio;
using SoundFlow.Components;
using SoundFlow.Enums;
using SoundFlow.Providers;

namespace DesktopEye.Common.Tests.Services.TTS;

public class TtsTest
{

    [Fact]
    public void TestSoundflowInit()
    {
        using var miniAudioEngine = new MiniAudioEngine(48000, Capability.Playback);
        
        using var dataProvider = new StreamDataProvider(File.OpenRead("/Users/remi.marques/Library/Application_Support/DesktopEye/Audio/test.wav"));
        var player = new SoundPlayer(dataProvider);

        Mixer.Master.AddComponent(player);
        
        player.Play();
        
        // Attendre un peu pour s'assurer que le son est joué
        System.Threading.Thread.Sleep(10000);
        
        player.Stop();
    }
    
    [Fact]
    public void TestSoundflowInitWithAudioFile()
    {
        /*using var miniAudioEngine = new MiniAudioEngine(48000, Capability.Playback);
        
        var audioFilePath = "/Users/remi.marques/Library/Application_Support/DesktopEye/Audio/test.wav";
        using var dataProvider = new StreamDataProvider(File.OpenRead(audioFilePath));
        var player = new SoundPlayer(dataProvider);

        Mixer.Master.AddComponent(player);
        
        player.Play();
        
        // Attendre un peu pour s'assurer que le son est joué
        System.Threading.Thread.Sleep(10000);
        
        player.Stop();*/
        
        // Dans une méthode de test
        using var miniAudioEngine = new MiniAudioEngine(48000, Capability.Playback);

        using var dataProvider = new StreamDataProvider(File.OpenRead("/Users/remi.marques/Library/Application_Support/DesktopEye/Audio/test.wav"));
        var player = new SoundPlayer(dataProvider);
        Mixer.Master.AddComponent(player);
        player.Play();
        System.Threading.Thread.Sleep(5000);
        
        player.Stop();
    }
}