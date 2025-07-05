using Avalonia;

namespace DesktopEye.Common.Domain.Models.OpticalCharacterRecognition;

public class OcrWord
{
    public OcrWord(int left, int top, int width, int height, float confidence, string text)
    {
        Rectangle = new Rect(top, left, width, height);
        Confidence = confidence;
        Text = text;
    }

    public Rect Rectangle { get; set; }
    public float Confidence { get; set; }
    public string Text { get; set; }
}