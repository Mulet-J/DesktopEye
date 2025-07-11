using System.Collections.Generic;
using System.IO;
using DesktopEye.Common.Infrastructure.Models;
using TesseractOCR.Enums;

namespace DesktopEye.Common.Infrastructure.Services.TrainedModel;

public class ModelRegistry
{
    public readonly List<Model> DefaultModels =
    [
        new()
        {
            ModelName = "NTextCat.xml",
            ModelUrl = "https://raw.githubusercontent.com/ivanakcheurov/ntextcat/refs/heads/master/src/LanguageModels/Core14.profile.xml",
            ModelFolderName = "ntextcat",
            Runtime = ModelRuntime.NTextCat,
            Source = ModelSource.DirectDownload,
            Type = ModelType.TextClassifier
        },
        new()
        {
            ModelName = "FastTextLight.bin",
            ModelUrl = "https://dl.fbaipublicfiles.com/fasttext/supervised-models/lid.176.bin",
            ModelFolderName = "fasttext",
            Runtime = ModelRuntime.FastText,
            Source = ModelSource.DirectDownload,
            Type = ModelType.TextClassifier
        },
        new()
        {
            ModelName = "FastText.bin",
            ModelUrl = "https://huggingface.co/facebook/fasttext-language-identification/resolve/main/model.bin",
            ModelFolderName = "fasttext",
            Runtime = ModelRuntime.FastText,
            Source = ModelSource.DirectDownload,
            Type = ModelType.TextClassifier
        },
        new()
        {
            ModelName = "facebook/nllb-200-distilled-600M",
            ModelUrl = "https://huggingface.co/facebook/nllb-200-distilled-600M",
            ModelFolderName = "nllb-pytorch",
            Runtime = ModelRuntime.NllbPyTorch,
            Source = ModelSource.HuggingFace,
            Type = ModelType.TextTranslator
        },
        new()
        {
            ModelName = "kokoro-v1.0.onnx",
            ModelUrl = "https://github.com/thewh1teagle/kokoro-onnx/releases/download/model-files-v1.0/kokoro-v1.0.onnx",
            ModelFolderName = "kokoro-tts",
            Runtime = ModelRuntime.KokoroTts,
            Source = ModelSource.DirectDownload,
            Type = ModelType.TextToSpeech
        }
        
    ];
    
    public readonly List<Language> DefaultTesseractLanguages =
    [
        Language.English,
        Language.SpanishCastilian,
        Language.Portuguese,
        Language.French,
        Language.German,
        Language.Italian,
        Language.Japanese,
        Language.JapaneseVertical,
        Language.ChineseSimplified,
        Language.ChineseTraditional,
        Language.Osd
    ];

    public List<Model> GenerateTesseractRegistry(List<TesseractOCR.Enums.Language> languages)
    {
        var models = new List<Model>();
        foreach (var language in languages)
        {
            var modelName = LanguageHelper.EnumToString(language) + ".traineddata";
            var model = new Model
            {
                ModelName = modelName,
                ModelUrl = $"https://raw.githubusercontent.com/tesseract-ocr/tessdata/refs/heads/main/{modelName.ToLowerInvariant()}",
                ModelFolderName = "tesseract" ,
                Runtime = ModelRuntime.Tesseract,
                Source = ModelSource.DirectDownload,
                Type = ModelType.OpticalCharacterRecognition
            };
            models.Add(model);
        }
        return models;
    }
}