using System;
using System.Collections.Generic;
using System.Linq;
using DesktopEye.Common.Domain.Models;

namespace DesktopEye.Common.Domain.Features.TextClassification.Helpers;

public static class LanguageHelper
{
    public static Language From639_3ToLanguage(string? iso639)
    {
        return iso639 switch
        {
            "dan" => Language.Danish,
            "deu" => Language.German,
            "eng" => Language.English,
            "fra" => Language.French,
            "ita" => Language.Italian,
            "jpn" => Language.Japanese,
            "kor" => Language.Korean,
            "nld" => Language.Dutch,
            "nor" => Language.Norwegian,
            "por" => Language.Portuguese,
            "rus" => Language.Russian,
            "spa" => Language.Spanish,
            "swe" => Language.Swedish,
            "zho" => Language.Chinese,
            _ => throw new Exception()
        };
    }

    public static Language From639_1ToLanguage(string? iso639)
    {
        return iso639 switch
        {
            "da" => Language.Danish,
            "de" => Language.German,
            "en" => Language.English,
            "fr" => Language.French,
            "it" => Language.Italian,
            "ja" => Language.Japanese,
            "ko" => Language.Korean,
            "nl" => Language.Dutch,
            "no" => Language.Norwegian,
            "pt" => Language.Portuguese,
            "ru" => Language.Russian,
            "es" => Language.Spanish,
            "sv" => Language.Swedish,
            "zh" => Language.Chinese,
            _ => throw new Exception()
        };
    }

    public static List<Language> GetAllLanguages()
    {
        return Enum.GetValues<Language>().ToList();
    }
}