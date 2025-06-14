using System;

namespace DesktopEye.Common.Exceptions;

public class PythonException : Exception
{
    public PythonException()
    {
    }

    public PythonException(string message) : base(message)
    {
    }
}