using System.Threading.Tasks;

namespace DesktopEye.Common.Infrastructure.Services.Dialog;

public interface IDialogService
{
    Task ShowMessageBoxAsync(string title, string message);

}