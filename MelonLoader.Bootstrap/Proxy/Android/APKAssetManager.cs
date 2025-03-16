#if ANDROID
using MelonLoader.Bootstrap.Java;
using MelonLoader.Bootstrap.Logging;

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
            using Stream? assetStream = GetAssetStream(itemPath);
            if (assetStream == null)
            {
                MelonLogger.LogError("Failed to get asset stream: " + itemPath, "APKAssetManager");
                return;
            }

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
        using JString pathString = JNI.NewString(path);
        using JClass assetManagerClass = JNI.GetObjectClass(assetManager);
        using JObject asset = JNI.CallObjectMethod<JObject>(assetManager, JNI.GetMethodID(assetManagerClass, "open", "(Ljava/lang/String;)Ljava/io/InputStream;"), new JValue(pathString));
        if (asset == null || !asset.Valid())
            return [];

        using MemoryStream outputStream = new();

        using JArray<sbyte> buffer = JNI.NewArray<sbyte>(1024);
        JMethodID readMethodID = JNI.GetMethodID(JNI.GetObjectClass(asset), "read", "([B)I");

        int bytesRead = 0;
        while ((bytesRead = JNI.CallMethod<int>(asset, readMethodID, new JValue(buffer))) > 0)
        {
            byte[] managedBuffer = (byte[])(Array)buffer.GetElements();
            outputStream.Write(managedBuffer, 0, bytesRead);
        }

        using JClass assetClass = JNI.GetObjectClass(asset);
        JMethodID closeMethodID = JNI.GetMethodID(assetClass, "close", "()V");
        JNI.CallVoidMethod(asset, closeMethodID);

        HandleException();

        return outputStream.ToArray();
    }

    public static Stream? GetAssetStream(string path)
    {
        using JString pathString = JNI.NewString(path);
        using JClass assetManagerClass = JNI.GetObjectClass(assetManager);
        JObject asset = JNI.CallObjectMethod<JObject>(assetManager, JNI.GetMethodID(assetManagerClass, "open", "(Ljava/lang/String;)Ljava/io/InputStream;"), new JValue(pathString));
        if (asset == null || !asset.Valid())
            return null;

        HandleException();

        return new APKAssetStream(asset);
    }

    public static string[] GetDirectoryContents(string directory)
    {
        using JString pathString = JNI.NewString(directory);
        using JClass assetManagerClass = JNI.GetObjectClass(assetManager);
        using JObjectArray<JString> assets = JNI.CallObjectMethod<JObjectArray<JString>>(assetManager, JNI.GetMethodID(assetManagerClass, "list", "(Ljava/lang/String;)[Ljava/lang/String;"), new JValue(pathString));

        string[] cleanAssets = [.. assets.Select(a => a.GetString())];
        HandleException();

        return cleanAssets;
    }

    public static bool DoesAssetExist(string path)
    {
        // using `list` isn't as fast as just calling open, but this allows the function to not crash on debuggable builds of apps
        string containingDir = path[..path.LastIndexOf('/')];
        using JString pathString = JNI.NewString(containingDir);
        using JClass assetManagerClass = JNI.GetObjectClass(assetManager);
        using JObjectArray<JString> assets = JNI.CallObjectMethod<JObjectArray<JString>>(assetManager, JNI.GetMethodID(assetManagerClass, "list", "(Ljava/lang/String;)[Ljava/lang/String;"), new JValue(pathString));

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

        using JClass unityClass = JNI.FindClass("com/unity3d/player/UnityPlayer");
        JFieldID activityFieldId = JNI.GetStaticFieldID(unityClass, "currentActivity", "Landroid/app/Activity;");
        using JObject currentActivityObj = JNI.GetStaticObjectField<JObject>(unityClass, activityFieldId);
        using JClass activityClass = JNI.GetObjectClass(currentActivityObj);
        JObject assetManagerObj = JNI.CallObjectMethod<JObject>(currentActivityObj, JNI.GetMethodID(activityClass, "getAssets", "()Landroid/content/res/AssetManager;"));

        HandleException();

        assetManager = assetManagerObj;
    }

    public class APKAssetStream : Stream, IDisposable
    {
        private readonly JMethodID _availableJmid;
        private readonly JMethodID _markSupportedJmid;
        private readonly JMethodID _skipJmid;
        private readonly JMethodID _resetJmid;
        private readonly JMethodID _readJmid;
        private readonly JMethodID _closeJmid;

        private readonly JObject _streamObject;

        private long _pos = 0;
        private bool _disposed = false;

        public APKAssetStream(JObject obj)
        {
            _streamObject = obj;

            using JClass streamClass = JNI.GetObjectClass(_streamObject);

            _availableJmid = JNI.GetMethodID(streamClass, "available", "()I");
            _readJmid = JNI.GetMethodID(streamClass, "read", "([BII)I");
            _markSupportedJmid = JNI.GetMethodID(streamClass, "markSupported", "()Z");
            _skipJmid = JNI.GetMethodID(streamClass, "skip", "(J)J");
            _resetJmid = JNI.GetMethodID(streamClass, "reset", "()V");
            _closeJmid = JNI.GetMethodID(streamClass, "close", "()V");
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => false;

        public override long Length
        {
            get
            {
                int length = JNI.CallMethod<int>(_streamObject, _availableJmid);
                HandleException();
                return length;
            }
        }

        public override long Position
        {
            get => _pos;
            set
            {
                bool canMark = JNI.CallMethod<bool>(_streamObject, _markSupportedJmid);
                if (!canMark)
                    throw new NotImplementedException();

                JNI.CallVoidMethod(_streamObject, _resetJmid);
                if (value > 0)
                {
                    long val = JNI.CallMethod<long>(_streamObject, _skipJmid, new JValue(value));
                    _pos = val;
                }

                HandleException();
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            using JArray<sbyte> javaBuffer = JNI.NewArray<sbyte>(count);

            int read = JNI.CallMethod<int>(_streamObject, _readJmid, new JValue(javaBuffer), new JValue(offset), new JValue(count));
            HandleException();

            if (read == -1)
                return 0;

            for (int i = 0; i < read; i++)
            {
                buffer[i] = (byte)javaBuffer[i];
            }

            _pos += read;
            return read;
        }

        public override void Write(byte[] buffer, int offset, int count) => throw new NotImplementedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotImplementedException();
        public override void SetLength(long value) => throw new NotImplementedException();
        public override void Flush() { }

        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    JNI.CallVoidMethod(_streamObject, _closeJmid);
                    _streamObject.Dispose();
                    HandleException();
                }
                _disposed = true;
            }
            base.Dispose(disposing);
        }
    }
}
#endif