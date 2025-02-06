namespace JNISharp.NativeInterface;

using System.Runtime.InteropServices;

[StructLayout(LayoutKind.Sequential)]
public unsafe struct JavaVMInitArgs
{
    public int Version;

    public int OptionsCount;

    public JavaVMOption* Options;

    public bool IgnoreUnrecognized;

    public JavaVMInitArgs(JNI.Version version, JavaVMOption[] options, bool ignoreUnrecognized)
    {
        this.Version = (int)version;
        this.OptionsCount = options.Length;
        this.Options = (JavaVMOption*)Marshal.UnsafeAddrOfPinnedArrayElement(options, 0);
        this.IgnoreUnrecognized = ignoreUnrecognized;
    }
}

