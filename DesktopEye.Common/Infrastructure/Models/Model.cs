namespace DesktopEye.Common.Infrastructure.Models;

public class Model
{
    public string? ModelName { get; set; }
    public string? ModelUrl { get; set; }
    public string? ModelFolderName { get; set; }
    public ModelType Type { get; set; }
    public ModelRuntime Runtime { get; set; }
    public ModelSource Source { get; set; }
}