using DesktopEye.Common.Enums;

namespace DesktopEye.Common.Services.Helpers;

public static class LanguageHelper
{
    public static Language FromIso3ToLanguage(string? iso3)
    {
        return iso3 switch
        {
            "eng" => Language.English,
            "fra" => Language.French,
            "deu" => Language.German,
            "spa" => Language.Spanish,
            _ => Language.Unknown
        };
    }

    public static Language FromIso2ToLanguage(string? iso2)
    {
        return iso2 switch
        {
            "en" => Language.English,
            "fr" => Language.French,
            "de" => Language.German,
            "sp" => Language.Spanish,
            _ => Language.Unknown
        };
    }
}