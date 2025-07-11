
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace DesktopEye.Common.Domain.Models.Dictionary;

public class WiktionaryResponse : Dictionary<string, List<WiktionaryEntry>> { }

public class WiktionaryEntry
{
    [JsonPropertyName("partOfSpeech")]
    public string PartOfSpeech { get; set; }

    [JsonPropertyName("language")]
    public string Language { get; set; }

    [JsonPropertyName("definitions")]
    public List<WiktionaryDefinition> Definitions { get; set; }
}

public class WiktionaryDefinition
{
    [JsonPropertyName("definition")]
    public string Definition { get; set; }

    [JsonPropertyName("parsedExamples")]
    public List<ParsedExample> ParsedExamples { get; set; }

    [JsonPropertyName("examples")]
    public List<string> Examples { get; set; }
}

public class ParsedExample
{
    [JsonPropertyName("example")]
    public string Example { get; set; }
}