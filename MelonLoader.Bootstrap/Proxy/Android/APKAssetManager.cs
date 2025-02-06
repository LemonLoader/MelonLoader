using JNISharp.NativeInterface;

namespace MelonLoader.Bootstrap.Proxy.Android;

public static class APKAssetManager
{
    private static JObject assetManager;

    public static void Initialize()
    {
        GetAndroidAssetManager();
    }

    public static void SaveItemToDirectory(string itemPath, string copyBase, bool includeInitial = true)
    {
        string[] contents = GetDirectoryContents(itemPath);
        if (contents.Length == 0)
        {
            string path = includeInitial ? itemPath : itemPath[(itemPath.IndexOf('/') + 1)..];
            if (string.IsNullOrEmpty(path))
                return;

            string outPath = Path.Combine(copyBase, path);
            string outDir = Path.GetDirectoryName(outPath)!;

            if (!Directory.Exists(outDir))
                Directory.CreateDirectory(outDir);

            using FileStream fileStream = File.Open(outPath, FileMode.Create);
            using Stream assetStream = GetAssetStream(itemPath);

            assetStream.CopyTo(fileStream);
            
            return;
        }

        foreach (string item in contents)
        {
            SaveItemToDirectory(Path.Combine(itemPath, item), copyBase, includeInitial);
        }
    }

    public static byte[] GetAssetBytes(string path)
    {
        JString pathString = JNI.NewString(path);
        JObject asset = JNI.CallObjectMethod<JObject>(assetManager, JNI.GetMethodID(JNI.GetObjectClass(assetManager), "open", "(Ljava/lang/String;)Ljava/io/InputStream;"), new JValue(pathString));
        if (asset == null || !asset.Valid())
            return [];

        using MemoryStream outputStream = new();

        JArray<sbyte> buffer = JNI.NewArray<sbyte>(1024);
        int bytesRead;
        JMethodID readMethodID = JNI.GetMethodID(JNI.GetObjectClass(asset), "read", "([B)I");

        while ((bytesRead = JNI.CallMethod<int>(asset, readMethodID, new JValue(buffer))) > 0)
        {
            byte[] managedBuffer = (byte[])(Array)buffer.GetElements();
            outputStream.Write(managedBuffer, 0, bytesRead);
        }

        JMethodID closeMethodID = JNI.GetMethodID(JNI.GetObjectClass(asset), "close", "()V");
        JNI.CallVoidMethod(asset, closeMethodID);

        HandleException();

        return outputStream.ToArray();
    }

    public static Stream GetAssetStream(string path)
    {
        using JString pathString = JNI.NewString(path);
        JObject asset = JNI.CallObjectMethod<JObject>(assetManager, JNI.GetMethodID(JNI.GetObjectClass(assetManager), "open", "(Ljava/lang/String;)Ljava/io/InputStream;"), new JValue(pathString));
        if (asset == null || !asset.Valid())
            return null;

        HandleException();

        return new APKAssetStream(asset);
    }

    public static string[] GetDirectoryContents(string directory)
    {
        JString pathString = JNI.NewString(directory);
        JObjectArray<JString> assets = JNI.CallObjectMethod<JObjectArray<JString>>(assetManager, JNI.GetMethodID(JNI.GetObjectClass(assetManager), "list", "(Ljava/lang/String;)[Ljava/lang/String;"), new JValue(pathString));

        string[] cleanAssets = assets.Select(a => a.GetString()).ToArray();
        HandleException();

        return cleanAssets;
    }

    public static bool DoesAssetExist(string path)
    {
        // using `list` isn't as fast as just calling open, but this allows the function to not crash on debuggable builds of apps
        string containingDir = path[..path.LastIndexOf('/')];
        JString pathString = JNI.NewString(containingDir);
        JObjectArray<JString> assets = JNI.CallObjectMethod<JObjectArray<JString>>(assetManager, JNI.GetMethodID(JNI.GetObjectClass(assetManager), "list", "(Ljava/lang/String;)[Ljava/lang/String;"), new JValue(pathString));

        bool exists = assets.Any(js =>
        {
            string asset = JNI.GetJStringString(js);
            return path.EndsWith(asset);
        });

        HandleException();

        return exists;
    }

    private static void HandleException()
    {
        if (JNI.ExceptionCheck())
            JNI.ExceptionClear();
    }

    private static void GetAndroidAssetManager()
    {
        if (assetManager?.Valid() ?? false)
            return;

        JClass unityClass = JNI.FindClass("com/unity3d/player/UnityPlayer");
        JFieldID activityFieldId = JNI.GetStaticFieldID(unityClass, "currentActivity", "Landroid/app/Activity;");
        JObject currentActivityObj = JNI.GetStaticObjectField<JObject>(unityClass, activityFieldId);
        JObject assetManagerObj = JNI.CallObjectMethod<JObject>(currentActivityObj, JNI.GetMethodID(JNI.GetObjectClass(currentActivityObj), "getAssets", "()Landroid/content/res/AssetManager;"));

        HandleException();

        assetManager = assetManagerObj;
    }

    public class APKAssetStream : Stream, IDisposable
    {
        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        private JMethodID AVAILABLE_JMID;
        public override long Length
        {
            get
            {
                int length = JNI.CallMethod<int>(_streamObject, AVAILABLE_JMID);
                HandleException();
                return length;
            }
        }

        private JMethodID MARKSUPPORTED_JMID;
        private JMethodID SKIP_JMID;
        private JMethodID RESET_JMID;
        public override long Position
        {
            get => _pos;
            set
            {
                bool canMark = JNI.CallMethod<bool>(_streamObject, MARKSUPPORTED_JMID);
                if (!canMark)
                    throw new NotImplementedException();

                JNI.CallVoidMethod(_streamObject, RESET_JMID);
                if (value > 0)
                {
                    long val = JNI.CallMethod<long>(_streamObject, SKIP_JMID, new JValue(value));
                    _pos = val;
                }

                HandleException();
            }
        }
        private long _pos = 0;

        private JObject _streamObject;

        public APKAssetStream(JObject obj)
        {
            _streamObject = obj;
            AVAILABLE_JMID = JNI.GetMethodID(JNI.GetObjectClass(_streamObject), "available", "()I");
            READ_JMID = JNI.GetMethodID(JNI.GetObjectClass(_streamObject), "read", "([BII)I");
            MARKSUPPORTED_JMID = JNI.GetMethodID(JNI.GetObjectClass(_streamObject), "markSupported", "()Z");
            SKIP_JMID = JNI.GetMethodID(JNI.GetObjectClass(_streamObject), "skip", "(J)J");
            RESET_JMID = JNI.GetMethodID(JNI.GetObjectClass(_streamObject), "reset", "()V");
        }

        public override void Flush()
        {
        }

        private JMethodID READ_JMID;
        public override int Read(byte[] buffer, int offset, int count)
        {
            using JArray<sbyte> javaBuffer = JNI.NewArray<sbyte>(buffer.Length);
            int read = JNI.CallMethod<int>(_streamObject, READ_JMID, new JValue(javaBuffer), new JValue(offset), new JValue(count));
            HandleException();

            for (int i = 0; i < count; i++)
            {
                buffer[i] = (byte)javaBuffer[i];
            }

            _pos += read;
            return read;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        void IDisposable.Dispose()
        {
            JMethodID closeMethodID = JNI.GetMethodID(JNI.GetObjectClass(_streamObject), "close", "()V");
            JNI.CallVoidMethod(_streamObject, closeMethodID);
            _streamObject.Dispose();

            HandleException();
        }
    }
}
