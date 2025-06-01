namespace DesktopEye.Common.Services.TextClassifierService;

public interface ITextClassifierService
{
    public string InferLanguage(string inputText);
}