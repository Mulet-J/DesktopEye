using Avalonia.Media.Imaging;
using Bugsnag;
using Bugsnag.Payload;
using DesktopEye.Common.Domain.Features.OpticalCharacterRecognition.Interfaces;
using DesktopEye.Common.Domain.Features.TextClassification.Interfaces;
using DesktopEye.Common.Domain.Features.TextTranslation.Interfaces;
using DesktopEye.Common.Domain.Models;
using DesktopEye.Common.Domain.Models.OpticalCharacterRecognition;
using DesktopEye.Common.Infrastructure.Models;
using DesktopEye.Common.Infrastructure.Services.Conda;
using DesktopEye.Common.Infrastructure.Services.Download;
using DesktopEye.Common.Infrastructure.Services.Python;
using DesktopEye.Common.Infrastructure.Services.TrainedModel;
using Exception = System.Exception;

namespace DesktopEye.Common.Tests.Integration;

#region OCR Mock Services

/// <summary>
/// Service OCR Tesseract mocké avec comportement réaliste pour les tests d'intégration
/// </summary>
public class MockTesseractOcrService : IOcrService
{
    private readonly Dictionary<string, (string text, float confidence)> _textPatterns;
    private readonly Random _random = new(42); // Seed fixe pour la reproductibilité
    private bool _isModelLoaded = false;

    public MockTesseractOcrService()
    {
        _textPatterns = new Dictionary<string, (string, float)>
        {
            // Patterns basés sur des mots-clés pour simulation réaliste
            { "hello", ("Hello World", 0.95f) },
            { "bonjour", ("Bonjour le monde", 0.92f) },
            { "hallo", ("Hallo Welt", 0.94f) },
            { "hola", ("Hola mundo", 0.93f) },
            { "longer", ("This is a longer text with multiple words that should test the OCR capabilities more thoroughly.", 0.88f) },
            { "mixed", ("Hello Bonjour Hola", 0.85f) },
            { "empty", ("", 0.0f) }
        };
    }

    public async Task<OcrResult> GetTextFromBitmapAsync(Bitmap bitmap, CancellationToken cancellation, bool preprocess)
    {
        return await GetTextFromBitmapAsync(bitmap, new List<Language>(), cancellation, preprocess);
    }

    public async Task<OcrResult> GetTextFromBitmapAsync(Bitmap bitmap, List<Language> languages, CancellationToken cancellation, bool preprocess)
    {
        if (!_isModelLoaded)
            throw new InvalidOperationException("OCR model not loaded. Call LoadRequiredAsync first.");

        // Simuler le temps de traitement OCR réaliste
        var processingTime = _random.Next(100, 500);
        await Task.Delay(processingTime, cancellation);
        
        var (text, baseConfidence) = DetermineTextFromBitmap(bitmap);
        
        // Ajuster la confiance selon les paramètres
        var confidence = CalculateConfidence(baseConfidence, languages, preprocess);
        
        var words = CreateMockWords(text, confidence);
        
        return new OcrResult(words, text, confidence);
    }

    public async Task<bool> LoadRequiredAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        // Simuler le chargement du modèle Tesseract
        await Task.Delay(_random.Next(50, 200), cancellationToken);
        _isModelLoaded = true;
        return true;
    }

    private (string text, float confidence) DetermineTextFromBitmap(Bitmap bitmap)
    {
        // Heuristique basée sur la taille de l'image pour simuler la reconnaissance
        var imageSize = bitmap.PixelSize.Width * bitmap.PixelSize.Height;
        var aspectRatio = (double)bitmap.PixelSize.Width / bitmap.PixelSize.Height;

        // Simulation de reconnaissance basée sur les caractéristiques de l'image
        return imageSize switch
        {
            < 30000 => _textPatterns["empty"], // Très petite image
            < 60000 => _textPatterns["hello"], // Petite image
            < 120000 => aspectRatio > 3 ? _textPatterns["longer"] : _textPatterns["bonjour"], // Image moyenne
            < 200000 => _textPatterns["hallo"], // Grande image
            _ => _textPatterns["mixed"] // Très grande image
        };
    }

    private float CalculateConfidence(float baseConfidence, List<Language> languages, bool preprocess)
    {
        var confidence = baseConfidence;
        
        // Améliorer la confiance si des langues spécifiques sont spécifiées
        if (languages.Count > 0)
            confidence += 0.05f;
        
        // Améliorer la confiance si le preprocessing est activé
        if (preprocess)
            confidence += 0.03f;
        
        // Ajouter une petite variation aléatoire pour simuler la réalité
        confidence += (_random.NextSingle() - 0.5f) * 0.1f;
        
        return Math.Clamp(confidence, 0.0f, 1.0f);
    }

    private List<OcrWord> CreateMockWords(string text, float averageConfidence)
    {
        if (string.IsNullOrEmpty(text))
            return new List<OcrWord>();

        var words = new List<OcrWord>();
        var parts = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        var currentX = 10;
        const int y = 20;
        const int height = 18;
        
        for (int i = 0; i < parts.Length; i++)
        {
            var word = parts[i];
            var width = word.Length * 12; // Approximation de largeur
            
            // Variation de confiance par mot
            var wordConfidence = averageConfidence + (_random.NextSingle() - 0.5f) * 0.2f;
            wordConfidence = Math.Clamp(wordConfidence, 0.1f, 1.0f);
            
            words.Add(new OcrWord(currentX, y, width, height, wordConfidence, word));
            currentX += width + 8; // Espacement entre les mots
        }
        
        return words;
    }

    public void Dispose()
    {
        _isModelLoaded = false;
    }
}

#endregion

#region Text Classification Mock Services

/// <summary>
/// Service de classification NTextCat mocké
/// </summary>
public class MockNTextCatClassifierService : ITextClassifierService
{
    private readonly Dictionary<string, Language> _languagePatterns;
    private readonly Random _random = new(42);
    private bool _isModelLoaded = false;

    public MockNTextCatClassifierService()
    {
        _languagePatterns = new Dictionary<string, Language>
        {
            { "hello", Language.English },
            { "world", Language.English },
            { "bonjour", Language.French },
            { "monde", Language.French },
            { "hallo", Language.German },
            { "welt", Language.German },
            { "hola", Language.Spanish },
            { "mundo", Language.Spanish },
            { "this", Language.English },
            { "longer", Language.English },
            { "text", Language.English }
        };
    }

    public Language ClassifyText(string text)
    {
        if (!_isModelLoaded)
            throw new InvalidOperationException("Classifier model not loaded. Call LoadRequiredAsync first.");

        if (string.IsNullOrWhiteSpace(text))
            return Language.English; // Fallback

        var textLower = text.ToLowerInvariant();
        
        // Compter les occurrences de mots pour chaque langue
        var languageScores = new Dictionary<Language, int>();
        
        foreach (var (keyword, language) in _languagePatterns)
        {
            if (textLower.Contains(keyword))
            {
                languageScores[language] = languageScores.GetValueOrDefault(language, 0) + 1;
            }
        }
        
        // Retourner la langue avec le score le plus élevé
        if (languageScores.Count > 0)
        {
            return languageScores.OrderByDescending(kvp => kvp.Value).First().Key;
        }
        
        return Language.English; // Fallback
    }

    public List<(Language language, double confidence)> ClassifyTextWithProbabilities(string text)
    {
        var detectedLanguage = ClassifyText(text);
        var confidence = 0.85 + _random.NextDouble() * 0.1; // Entre 0.85 et 0.95
        
        var results = new List<(Language, double)> { (detectedLanguage, confidence) };
        
        // Ajouter d'autres langues avec des confidences plus faibles
        var otherLanguages = Enum.GetValues<Language>()
            .Where(lang => lang != detectedLanguage)
            .Take(2)
            .ToList();
        
        foreach (var lang in otherLanguages)
        {
            var lowerConfidence = _random.NextDouble() * (confidence - 0.1);
            results.Add((lang, lowerConfidence));
        }
        
        return results.OrderByDescending(r => r.Item2).ToList();
    }

    public async Task<Language> ClassifyTextAsync(string text, CancellationToken cancellationToken = default)
    {
        // Simuler le temps de classification
        await Task.Delay(_random.Next(50, 150), cancellationToken);
        return ClassifyText(text);
    }

    public async Task<bool> LoadRequiredAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        await Task.Delay(_random.Next(20, 100), cancellationToken);
        _isModelLoaded = true;
        return true;
    }

    public void Dispose()
    {
        _isModelLoaded = false;
    }
}

/// <summary>
/// Service de classification FastText mocké
/// </summary>
public class MockFastTextClassifierService : ITextClassifierService
{
    private readonly MockNTextCatClassifierService _baseClassifier = new();
    private readonly Random _random = new(43); // Seed différent pour varier

    public Language ClassifyText(string text) => _baseClassifier.ClassifyText(text);

    public List<(Language language, double confidence)> ClassifyTextWithProbabilities(string text)
    {
        // FastText généralement plus précis, donc confidences légèrement plus élevées
        var results = _baseClassifier.ClassifyTextWithProbabilities(text);
        
        return results.Select(r => (r.language, Math.Min(r.confidence + 0.05, 0.99))).ToList();
    }

    public async Task<Language> ClassifyTextAsync(string text, CancellationToken cancellationToken = default)
    {
        // FastText généralement plus rapide
        await Task.Delay(_random.Next(30, 100), cancellationToken);
        return ClassifyText(text);
    }

    public async Task<bool> LoadRequiredAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        await _baseClassifier.LoadRequiredAsync(modelName, cancellationToken);
        return true;
    }

    public void Dispose() => _baseClassifier.Dispose();
}

#endregion

#region Translation Mock Services

/// <summary>
/// Service de traduction NLLB mocké avec traductions réalistes
/// </summary>
public class MockNllbTranslationService : ITranslationService
{
    private readonly Dictionary<(Language source, Language target, string sourceText), string> _translations;
    private readonly Dictionary<Language, string[]> _defaultWords;
    private readonly Random _random = new(44);
    private bool _isModelLoaded = false;

    public MockNllbTranslationService()
    {
        _translations = new Dictionary<(Language, Language, string), string>
        {
            // Traductions exactes pour les textes de test
            { (Language.French, Language.English, "bonjour le monde"), "hello world" },
            { (Language.English, Language.French, "hello world"), "bonjour le monde" },
            { (Language.German, Language.English, "hallo welt"), "hello world" },
            { (Language.English, Language.German, "hello world"), "hallo welt" },
            { (Language.Spanish, Language.English, "hola mundo"), "hello world" },
            { (Language.English, Language.Spanish, "hello world"), "hola mundo" },
            
            // Traductions pour textes plus longs
            { (Language.English, Language.French, "this is a longer text"), "ceci est un texte plus long" },
            { (Language.French, Language.English, "ceci est un texte plus long"), "this is a longer text" }
        };

        _defaultWords = new Dictionary<Language, string[]>
        {
            { Language.English, new[] { "hello", "world", "text", "the", "and", "is", "this" } },
            { Language.French, new[] { "bonjour", "monde", "texte", "le", "et", "est", "ceci" } },
            { Language.German, new[] { "hallo", "welt", "text", "der", "und", "ist", "dies" } },
            { Language.Spanish, new[] { "hola", "mundo", "texto", "el", "y", "es", "esto" } }
        };
    }

    public string Translate(string text, Language sourceLanguage, Language targetLanguage)
    {
        if (!_isModelLoaded)
            throw new InvalidOperationException("Translation model not loaded. Call LoadRequiredAsync first.");

        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        if (sourceLanguage == targetLanguage)
            return text;

        var textLower = text.ToLowerInvariant().Trim();
        
        // Rechercher une traduction exacte
        var exactMatch = _translations.FirstOrDefault(t => 
            t.Key.source == sourceLanguage && 
            t.Key.target == targetLanguage && 
            textLower.Contains(t.Key.sourceText.ToLowerInvariant()));
        
        if (!exactMatch.Equals(default(KeyValuePair<(Language, Language, string), string>)))
        {
            return exactMatch.Value;
        }

        // Traduction mot par mot pour les textes non reconnus
        return TranslateWordByWord(text, sourceLanguage, targetLanguage);
    }

    public async Task<string> TranslateAsync(string text, Language sourceLanguage, Language targetLanguage, CancellationToken cancellationToken = default)
    {
        // Simuler le temps de traduction (plus long que la classification)
        var delay = text.Length * 2 + _random.Next(100, 300);
        await Task.Delay(delay, cancellationToken);
        
        return Translate(text, sourceLanguage, targetLanguage);
    }

    public async Task<bool> LoadRequiredAsync(string? modelName = null, CancellationToken cancellationToken = default)
    {
        // Simuler le chargement du modèle de traduction (plus long)
        await Task.Delay(_random.Next(200, 500), cancellationToken);
        _isModelLoaded = true;
        return true;
    }

    public bool LoadRequired(string? modelName = null)
    {
        _isModelLoaded = true;
        return true;
    }

    private string TranslateWordByWord(string text, Language sourceLanguage, Language targetLanguage)
    {
        if (!_defaultWords.ContainsKey(targetLanguage))
            return $"[{targetLanguage}] {text}"; // Fallback

        var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var translatedWords = new List<string>();
        var targetWords = _defaultWords[targetLanguage];

        foreach (var word in words)
        {
            // Traduction simple basée sur la position dans la liste
            var hash = Math.Abs(word.ToLowerInvariant().GetHashCode()) % targetWords.Length;
            translatedWords.Add(targetWords[hash]);
        }

        return string.Join(" ", translatedWords);
    }

    public void Dispose()
    {
        _isModelLoaded = false;
    }
}

#endregion