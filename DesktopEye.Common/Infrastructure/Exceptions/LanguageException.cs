using System;

namespace DesktopEye.Common.Infrastructure.Exceptions;

public class LanguageException : Exception
{
    public LanguageException()
    {
    }

    public LanguageException(string message) : base(message)
    {
    }
}