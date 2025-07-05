using System;

namespace DesktopEye.Common.Infrastructure.Exceptions;

public class DownloadException : Exception
{
    public DownloadException()
    {
    }

    public DownloadException(string message) : base(message)
    {
    }
}