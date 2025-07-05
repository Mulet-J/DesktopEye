using System;

namespace DesktopEye.Common.Infrastructure.Exceptions;

public class PythonException : Exception
{
    public PythonException()
    {
    }

    public PythonException(string message) : base(message)
    {
    }
}