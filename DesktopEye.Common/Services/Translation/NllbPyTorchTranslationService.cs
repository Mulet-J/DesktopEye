using System;
using System.Threading.Tasks;
using DesktopEye.Common.Classes;
using DesktopEye.Common.Enums;
using DesktopEye.Common.Services.ApplicationPath;
using DesktopEye.Common.Services.Conda;
using DesktopEye.Common.Services.Python;
using Python.Runtime;
using PythonException = DesktopEye.Common.Exceptions.PythonException;

namespace DesktopEye.Common.Services.Translation;

public class NllbPyTorchTranslationService : ITranslationService, IDisposable
{
    private const string BaseModel = "facebook/nllb-200-distilled-600M";

    private static readonly CondaInstallInstruction
        PythonDependencies = new("conda-forge", ["transformers", "pytorch"]);

    private readonly ICondaService _condaService;
    private readonly string _modelDirectory;
    private readonly IPythonRuntimeManager _runtimeManager;
    private dynamic? _model;
    private dynamic? _tokenizer;

    public NllbPyTorchTranslationService(ICondaService condaService, IPathService pathService,
        IPythonRuntimeManager runtimeManager)
    {
        _condaService = condaService;
        _modelDirectory = pathService.ModelsDirectory;
        _runtimeManager = runtimeManager;
        _runtimeManager.StartRuntime(this);
    }

    public void Dispose()
    {
        // ObjectDisposedException.ThrowIf(_isDisposed, nameof(NllbPyTorchTranslationService));

        try
        {
            _runtimeManager.StopRuntime(this);
        }
        catch (Exception ex)
        {
            return;
        }

        GC.SuppressFinalize(this);
    }

    public string Translate(string text, Language sourceLanguage, Language targetLanguage)
    {
        var convertedSourceLanguage = EnumLanguageToLibLanguage(sourceLanguage);
        var convertedTargetLanguage = EnumLanguageToLibLanguage(targetLanguage);

        if (_tokenizer == null)
            throw new Exception();

        if (_model == null)
            throw new Exception();

        using (Py.GIL())
        {
            _tokenizer.src_lang = convertedSourceLanguage;

            var encoded = _tokenizer(text, return_tensors: "pt");
            var forcedBosTokenId = _tokenizer.convert_tokens_to_ids(convertedTargetLanguage);

            var generatedTokens = _model.generate(
                input_ids: encoded["input_ids"],
                attention_mask: encoded["attention_mask"],
                forced_bos_token_id: forcedBosTokenId
            );

            var decoded = _tokenizer.batch_decode(generatedTokens, skip_special_tokens: true);
            return decoded[0].ToString();
        }
    }

    public async Task<string> TranslateAsync(string text, Language sourceLanguage, Language targetLanguage)
    {
        return await Task.Run(() => Translate(text, sourceLanguage, targetLanguage));
    }

    public async Task<bool> LoadRequiredAsync()
    {
        return await LoadRequiredAsync(BaseModel);
    }

    public async Task<bool> LoadRequiredAsync(string modelName)
    {
        try
        {
            await Task.Run(() => { _tokenizer = LoadTokenizer(modelName); });

            await Task.Run(() => { _model = LoadModel(modelName); });
            return true;
        }
        catch
        {
            return false;
        }
    }

    public bool LoadRequired(string modelName = BaseModel)
    {
        try
        {
            _tokenizer = LoadTokenizer(modelName);
            _model = LoadModel(modelName);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private dynamic LoadTokenizer(string modelName)
    {
        using (Py.GIL())
        {
            dynamic transformers = Py.Import("transformers");
            var autoTokenizer = transformers.GetAttr("AutoTokenizer");
            return autoTokenizer.from_pretrained(modelName, cache_dir: _modelDirectory);
        }
    }

    private dynamic LoadModel(string modelName)
    {
        using (Py.GIL())
        {
            dynamic transformers = Py.Import("transformers");
            var autoModelForSeq2SeqLm = transformers.GetAttr("AutoModelForSeq2SeqLM");
            return autoModelForSeq2SeqLm.from_pretrained(modelName, cache_dir: _modelDirectory,
                torch_dtype: "auto");
        }
    }

    private async Task<bool> CondaInstallDependenciesAsync()
    {
        var res = await _condaService.InstallPackageUsingCondaAsync(PythonDependencies);
        if (!res) throw new PythonException("Could not install dependencies");

        return true;
    }

    private string EnumLanguageToLibLanguage(Language language)
    {
        return language switch
        {
            Language.English => "eng_Latn",
            Language.French => "fra_Latn",
            _ => "eng_Latn"
        };
    }
}