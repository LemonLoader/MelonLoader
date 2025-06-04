using System.IO;
using System.Diagnostics;

namespace MelonLoader.Utils
{
    public static class MelonEnvironment
    {
        private const string OurRuntimeName =
#if !NET6_0
            "net35";
#else
            "net6";
#endif

        public static bool IsDotnetRuntime { get; } = OurRuntimeName == "net6";
        public static bool IsMonoRuntime { get; } = !IsDotnetRuntime;

        public static string MelonBaseDirectory => LoaderConfig.Current.Loader.BaseDirectory;

        public static string GameExecutablePath { get; } = Process.GetCurrentProcess().MainModule.FileName;
        public static string MelonLoaderDirectory { get; } = Path.Combine(MelonBaseDirectory, "MelonLoader");
#if !ANDROID
        public static string GameRootDirectory { get; } = Path.GetDirectoryName(GameExecutablePath);
#else
        public static string GameRootDirectory { get; } = MelonBaseDirectory;

        public static string PackageName { get; } = Path.GetFileName(MelonBaseDirectory); // MelonBaseDirectory is named after the current package by the bootstrap
#endif


        public static string DependenciesDirectory { get; } = Path.Combine(MelonLoaderDirectory, "Dependencies");
        public static string SupportModuleDirectory { get; } = Path.Combine(DependenciesDirectory, "SupportModules");
        public static string CompatibilityLayerDirectory { get; } = Path.Combine(DependenciesDirectory, "CompatibilityLayers");
        public static string Il2CppAssemblyGeneratorDirectory { get; } = Path.Combine(DependenciesDirectory, "Il2CppAssemblyGenerator");
        public static string ModsDirectory { get; } = Path.Combine(MelonBaseDirectory, "Mods");
        public static string PluginsDirectory { get; } = Path.Combine(MelonBaseDirectory, "Plugins");
        public static string UserLibsDirectory { get; } = Path.Combine(MelonBaseDirectory, "UserLibs");
        public static string UserDataDirectory { get; } = Path.Combine(MelonBaseDirectory, "UserData");
        public static string MelonLoaderLogsDirectory { get; } = Path.Combine(MelonLoaderDirectory, "Logs");
        public static string OurRuntimeDirectory { get; } = Path.Combine(MelonLoaderDirectory, OurRuntimeName);

        public static string GameExecutableName { get; } = Path.GetFileNameWithoutExtension(GameExecutablePath);
#if !ANDROID
        public static string UnityGameDataDirectory { get; } = Path.Combine(GameRootDirectory, GameExecutableName + "_Data");
#else

        public static string UnityGameDataDirectory { get; } = "bin/Data/"; // inside the APK's assets directory; we're interacting with it via the AssetManager API so this makes sense
#endif
        public static string UnityGameManagedDirectory { get; } = Path.Combine(UnityGameDataDirectory, "Managed");
        public static string Il2CppDataDirectory { get; } = Path.Combine(UnityGameDataDirectory, "il2cpp_data");
        public static string UnityPlayerPath { get; } = Path.Combine(GameRootDirectory, "UnityPlayer.dll");

        public static string MelonManagedDirectory { get; } = Path.Combine(DependenciesDirectory, "Mono");
        public static string Il2CppAssembliesDirectory { get; } = Path.Combine(MelonLoaderDirectory, "Il2CppAssemblies");

        internal static void PrintEnvironment()
        {
            //These must not be changed, lum needs them
            MelonLogger.MsgDirect($"Core::BasePath = {MelonBaseDirectory}");
            MelonLogger.MsgDirect($"Game::BasePath = {GameRootDirectory}");
            MelonLogger.MsgDirect($"Game::DataPath = {UnityGameDataDirectory}");
            MelonLogger.MsgDirect($"Game::ApplicationPath = {GameExecutablePath}");

            MelonLogger.MsgDirect($"Runtime Type: {OurRuntimeName}");
        }
    }
}