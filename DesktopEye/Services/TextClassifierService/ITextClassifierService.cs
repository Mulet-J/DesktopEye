namespace DesktopEye.Services.TextClassifierService;

public interface ITextClassifierService
{
    public string InferLanguage(string inputText);
}