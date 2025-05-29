using FastText.NetWrapper;

namespace DesktopEye.Services.TextClassifierService;

public class FastTextService : ITextClassifierService
{
    private readonly FastTextWrapper _fastText;

    public FastTextService(string modelPath)
    {
        _fastText = new FastTextWrapper();
        _fastText.LoadModel(modelPath);
    }
    
    public string InferLanguage(string inputText)
    {
        var prediction = _fastText.PredictSingle(inputText);
        float probability = prediction.Probability;

        return prediction.Label;
    }
}