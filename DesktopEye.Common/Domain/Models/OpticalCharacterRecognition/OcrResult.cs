using System.Collections.Generic;

namespace DesktopEye.Common.Domain.Models.OpticalCharacterRecognition;

public class OcrResult
{
    public OcrResult(List<OcrWord> words, string text, float meanConfidence)
    {
        Words = words;
        Text = text;
        MeanConfidence = meanConfidence;
    }

    public List<OcrWord> Words { get; set; }
    public string Text { get; set; }
    public float MeanConfidence { get; set; }
}