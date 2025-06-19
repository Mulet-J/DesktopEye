using FastText.NetWrapper;

namespace DesktopEye.Common.Services.TextClassifierService;

public class FastTextService : ITextClassifierService
{
    private readonly FastTextWrapper _fastText;

    public FastTextService(string modelPath)
    {
        _fastText = new FastTextWrapper();
        _fastText.LoadModel(modelPath);
    }

    //TODO remonter probabilité
    public string InferLanguage(string inputText)
    {
        var prediction = _fastText.PredictSingle(inputText);

        return prediction.Label;
    }
}