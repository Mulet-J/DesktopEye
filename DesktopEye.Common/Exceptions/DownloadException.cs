using System;

namespace DesktopEye.Common.Exceptions;

public class DownloadException : Exception
{
    public DownloadException()
    {
    }

    public DownloadException(string message) : base(message)
    {
    }
}