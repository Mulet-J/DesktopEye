using System;

namespace DesktopEye.Common.Exceptions;

public class LanguageException : Exception
{
    public LanguageException()
    {
    }

    public LanguageException(string message) : base(message)
    {
    }
}