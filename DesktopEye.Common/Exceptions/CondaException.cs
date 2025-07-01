using System;

namespace DesktopEye.Common.Exceptions;

public class CondaException : Exception
{
    public CondaException()
    {
    }

    public CondaException(string message) : base(message)
    {
    }
}