using System.Collections.Generic;
using TesseractOCR.Enums;

namespace DesktopEye.Common.Domain.Features.OpticalCharacterRecognition.Helpers;

public static class ScriptNameHelper
{
    private static readonly Dictionary<ScriptName, List<Language>> ScriptToLanguages = new()
    {
        // Latin Script
        {
            ScriptName.Latin, [
                Language.English,
                Language.French,
                Language.Danish,
                Language.German,
                Language.Italian,
                Language.Dutch,
                Language.Norwegian,
                Language.Portuguese,
                Language.SpanishCastilian,
                Language.Swedish
            ]
        },

        // Cyrillic Script
        { ScriptName.Cyrillic, [Language.Russian] },

        // Japanese Script
        { ScriptName.Japanese, [Language.Japanese] },

        // Hangul Script
        { ScriptName.Hangul, [Language.Korean] },

        // HanS Script
        { ScriptName.HanS, [Language.ChineseSimplified] }
    };

    // Helper method to get script for a language
    public static List<Language> GetLanguageForScript(ScriptName scriptName)
    {
        return ScriptToLanguages.TryGetValue(scriptName, out var language) ? language : [Language.English];
    }
}