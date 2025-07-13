using System.Text;
using System.Text.RegularExpressions;
using DesktopEye.Common.Domain.Models.Dictionary;

namespace DesktopEye.Common.Domain.Features.Dictionary.Helpers;

/// <summary>
/// Helper class for formatting Wiktionary dictionary responses into readable text.
/// </summary>
public static class WiktionaryFormatter
{
    /// <summary>
    /// Formats dictionary definitions from a Wiktionary response into a human-readable string.
    /// </summary>
    /// <param name="response">The Wiktionary response containing definitions grouped by language</param>
    /// <param name="term">The term that was looked up</param>
    /// <returns>A formatted string with all definitions and examples</returns>
    public static string FormatDefinitions(WiktionaryResponse response, string term)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Définitions pour « {term} » :\n");

        bool isFirstLanguage = true;
        foreach (var lang in response)
        {
            // Add separator between languages (except for the first one)
            if (!isFirstLanguage)
            {
                sb.AppendLine();
                sb.AppendLine("───────────────────────────────────────────────");
                sb.AppendLine();
            }
            isFirstLanguage = false;
            
            sb.AppendLine($"Langue : {lang.Key}");
            sb.AppendLine();
            
            foreach (var entry in lang.Value)
            {
                sb.AppendLine($"  {entry.PartOfSpeech} ({entry.Language})");
                sb.AppendLine();
                
                foreach (var def in entry.Definitions)
                {
                    var cleanDef = CleanHtml(def.Definition);
                    sb.AppendLine($"    • {cleanDef}");
                    
                    if (def.ParsedExamples?.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("      Exemples :");
                        foreach (var ex in def.ParsedExamples)
                        {
                            var cleanEx = CleanHtml(ex.Example);
                            sb.AppendLine($"        ◦ {cleanEx}");
                        }
                    }
                    else if (def.Examples?.Count > 0)
                    {
                        sb.AppendLine();
                        sb.AppendLine("      Exemples :");
                        foreach (var ex in def.Examples)
                        {
                            var cleanEx = CleanHtml(ex);
                            sb.AppendLine($"        ◦ {cleanEx}");
                        }
                    }
                    sb.AppendLine();
                }
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Removes HTML tags and converts HTML entities to their corresponding characters.
    /// </summary>
    /// <param name="html">The HTML string to clean</param>
    /// <returns>A plain text version of the input HTML</returns>
    private static string CleanHtml(string html)
    {
        if (string.IsNullOrEmpty(html)) return string.Empty;
        // Remove HTML tags
        return Regex.Replace(html, "<.*?>", string.Empty)
            .Replace("&quot;", "\"")
            .Replace("&amp;", "&")
            .Replace("&lt;", "<")
            .Replace("&gt;", ">");
    }
}