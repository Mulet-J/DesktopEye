using System;

namespace DesktopEye.Common.Infrastructure.Exceptions;

public class CondaException : Exception
{
    public CondaException()
    {
    }

    public CondaException(string message) : base(message)
    {
    }
}