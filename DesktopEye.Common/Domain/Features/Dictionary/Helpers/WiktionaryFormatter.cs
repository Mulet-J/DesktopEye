using DesktopEye.Common.Domain.Models.Dictionary;

namespace DesktopEye.Common.Domain.Features.Dictionary.Helpers;

using System.Text;
using System.Text.RegularExpressions;


public static class WiktionaryFormatter
{
    public static string FormatDefinitions(WiktionaryResponse response, string term)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Definitions for « {term} » :\n");

        foreach (var lang in response)
        {
            sb.AppendLine($"Language : {lang.Key}");
            foreach (var entry in lang.Value)
            {
                sb.AppendLine($"  {entry.PartOfSpeech} ({entry.Language})");
                foreach (var def in entry.Definitions)
                {
                    var cleanDef = CleanHtml(def.Definition);
                    sb.AppendLine($"    • {cleanDef}");

                    if (def.ParsedExamples != null)
                    {
                        foreach (var ex in def.ParsedExamples)
                        {
                            var cleanEx = CleanHtml(ex.Example);
                            sb.AppendLine($"      Ex: {cleanEx}");
                        }
                    }
                    else if (def.Examples != null)
                    {
                        foreach (var ex in def.Examples)
                        {
                            var cleanEx = CleanHtml(ex);
                            sb.AppendLine($"      Ex: {cleanEx}");
                        }
                    }
                }
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static string CleanHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        // Suppression des balises HTML
        return Regex.Replace(html, "<.*?>", string.Empty)
            .Replace("&quot;", "\"")
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">");
    }
}