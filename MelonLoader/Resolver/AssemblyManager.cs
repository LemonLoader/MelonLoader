using System;
using System.Collections.Generic;
using System.Reflection;

#if NET6_0_OR_GREATER
using System.Runtime.Loader;
#endif

#pragma warning disable CS8632

namespace MelonLoader.Resolver;

internal static class AssemblyManager
{
    private static readonly Dictionary<string, AssemblyResolveInfo> InfoDict = new();

    internal static bool Setup()
    {
        InstallHooks();

        // Setup all Loaded Assemblies
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            LoadInfo(assembly);

        return true;
    }

    internal static AssemblyResolveInfo GetInfo(string name)
    {
        lock (InfoDict)
        {
            if (InfoDict.TryGetValue(name, out AssemblyResolveInfo resolveInfo))
                return resolveInfo;
            InfoDict[name] = new AssemblyResolveInfo();
            return InfoDict[name];
        }
    }

    private static Assembly SearchAssembly(string requestedName, Version requestedVersion)
    {
        // Get Resolve Information Object
        AssemblyResolveInfo resolveInfo = GetInfo(requestedName);

        // Resolve the Information Object
        Assembly assembly = resolveInfo.Resolve(requestedVersion);

        // Run Passthrough Events
        if (assembly == null)
            assembly = MelonAssemblyResolver.SafeInvoke_OnAssemblyResolve(requestedName, requestedVersion);

#if NET6_0_OR_GREATER
        // Search Directories
        if (assembly == null)
            assembly = SearchDirectoryManager.Scan(requestedName);
#endif

        // Return
        return assembly;
    }

    internal static void LoadInfo(Assembly assembly)
    {
        // Get AssemblyName
        AssemblyName assemblyName = assembly.GetName();

        // Get Resolve Information Object
        AssemblyResolveInfo resolveInfo = GetInfo(assemblyName.Name);

        // Set Version of Assembly
        resolveInfo.SetVersionSpecific(assemblyName.Version, assembly);

        // Run Passthrough Events
        MelonAssemblyResolver.SafeInvoke_OnAssemblyLoad(assembly);
    }

    private static void InstallHooks()
    {
        AppDomain.CurrentDomain.AssemblyLoad += OnAppDomainAssemblyLoad;
#if NET6_0_OR_GREATER
        AssemblyLoadContext.Default.Resolving += Resolve;
#else
            AppDomain.CurrentDomain.AssemblyResolve += OnAppDomainAssemblyResolve;
            InternalUtils.BootstrapInterop.Library.MonoInstallHooks();
#endif
    }

#if NET6_0_OR_GREATER
    private static Assembly? Resolve(AssemblyLoadContext alc, AssemblyName name)
        => SearchAssembly(name.Name, name.Version);
#else
        private static Assembly SearchAssembly(string requestedName, ushort major, ushort minor, ushort build, ushort revision)
        {
            Version requestedVersion = new Version(major, minor, build, revision);
            return SearchAssembly(requestedName, requestedVersion);
        }

        private static Assembly OnAppDomainAssemblyResolve(object sender, ResolveEventArgs args)
        {
            AssemblyName name = new AssemblyName(args.Name);
            return SearchDirectoryManager.Scan(name.Name);
        }
#endif

    private static void OnAppDomainAssemblyLoad(object _, AssemblyLoadEventArgs args)
    {
        LoadInfo(args.LoadedAssembly);
    }
}