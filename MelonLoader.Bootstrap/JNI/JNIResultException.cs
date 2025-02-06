using System;

namespace JNISharp.NativeInterface;

public class JNIResultException : Exception
{
    public JNI.Result Result { get; set;}

    public JNIResultException(JNI.Result result) : base($"JNI error occurred: {result}")
    {
        this.Result = result;
    }
}
