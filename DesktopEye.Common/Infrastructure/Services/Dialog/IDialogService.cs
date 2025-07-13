using System.Threading.Tasks;

namespace DesktopEye.Common.Infrastructure.Services.Dialog;

/// <summary>
/// Interface for dialog services to display messages and interact with the user.
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Displays a message box with the specified title and content.
    /// </summary>
    /// <param name="title">The title of the message box.</param>
    /// <param name="content">The content of the message box.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task ShowMessageBoxAsync(string title, string message);

}