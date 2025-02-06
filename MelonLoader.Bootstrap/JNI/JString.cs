namespace JNISharp.NativeInterface;

public class JString : JObject
{
    public JString() : base() { }

    public string GetString() => JNI.GetJStringString(this);
}
