using System.Collections.Generic;

namespace DesktopEye.Common.Classes;

public class OcrResult
{
    public OcrResult(List<OcrWord> words, string text)
    {
        Words = words;
        Text = text;
    }

    public List<OcrWord> Words { get; set; }
    public string Text { get; set; }
}