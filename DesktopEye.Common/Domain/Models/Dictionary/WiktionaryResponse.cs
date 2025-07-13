using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DesktopEye.Common.Domain.Models.Dictionary;

/// <summary>
/// Represents a response from the Wiktionary API, mapping language codes to lists of entries.
/// </summary>
public class WiktionaryResponse : Dictionary<string, List<WiktionaryEntry>> { }

/// <summary>
/// Represents a single dictionary entry from Wiktionary.
/// </summary>
public class WiktionaryEntry
{
    /// <summary>
    /// Gets or sets the part of speech of the entry (noun, verb, adjective, etc.).
    /// </summary>
    [JsonPropertyName("partOfSpeech")]
    public string PartOfSpeech { get; set; }

    /// <summary>
    /// Gets or sets the language of the entry.
    /// </summary>
    [JsonPropertyName("language")]
    public string Language { get; set; }

    /// <summary>
    /// Gets or sets the list of definitions for this entry.
    /// </summary>
    [JsonPropertyName("definitions")]
    public List<WiktionaryDefinition> Definitions { get; set; }
}

/// <summary>
/// Represents a definition within a Wiktionary entry.
/// </summary>
public class WiktionaryDefinition
{
    /// <summary>
    /// Gets or sets the text of the definition.
    /// </summary>
    [JsonPropertyName("definition")]
    public string Definition { get; set; }

    /// <summary>
    /// Gets or sets the parsed examples associated with this definition.
    /// </summary>
    [JsonPropertyName("parsedExamples")]
    public List<ParsedExample> ParsedExamples { get; set; }

    /// <summary>
    /// Gets or sets the simple examples associated with this definition.
    /// </summary>
    [JsonPropertyName("examples")]
    public List<string> Examples { get; set; }
}

/// <summary>
/// Represents a parsed example of word usage from Wiktionary.
/// </summary>
public class ParsedExample
{
    /// <summary>
    /// Gets or sets the example text.
    /// </summary>
    [JsonPropertyName("example")]
    public string Example { get; set; }
}