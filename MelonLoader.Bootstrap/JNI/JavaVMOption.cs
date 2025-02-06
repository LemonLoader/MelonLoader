namespace JNISharp.NativeInterface;

using System;
using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct JavaVMOption
{
    public IntPtr OptionString;

    public IntPtr ExtraInfo;

    public JavaVMOption(string optionString)
    {
        this.OptionString = Marshal.StringToHGlobalAnsi(optionString);
        this.ExtraInfo = IntPtr.Zero;
    }
}

