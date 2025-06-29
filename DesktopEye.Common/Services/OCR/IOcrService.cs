using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using DesktopEye.Common.Classes;
using DesktopEye.Common.Enums;
using DesktopEye.Common.Interfaces;

namespace DesktopEye.Common.Services.OCR;

public interface IOcrService : ILoadable
{
    /// <summary>
    ///     Get the text while trying to infer the language
    /// </summary>
    /// <param name="bitmap"></param>
    /// <param name="preprocess"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    Task<OcrResult> GetTextFromBitmapAsync(Bitmap bitmap, CancellationToken cancellation, bool preprocess);

    /// <summary>
    ///     Get the text using the specified languages
    /// </summary>
    /// <param name="bitmap"></param>
    /// <param name="languages"></param>
    /// <param name="preprocess"></param>
    /// <param name="cancellation"></param>
    /// <returns></returns>
    Task<OcrResult> GetTextFromBitmapAsync(Bitmap bitmap, List<Language> languages, CancellationToken cancellation,
        bool preprocess);
}