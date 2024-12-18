﻿using System;
using System.Collections.Generic;
using System.Reflection;

#if NET6_0_OR_GREATER
using System.Runtime.Loader;
#else
using System.Runtime.CompilerServices;
#endif

#pragma warning disable CS8632

namespace MelonLoader.Resolver
{
    internal class AssemblyManager
    {
        internal static Dictionary<string, AssemblyResolveInfo> InfoDict = new Dictionary<string, AssemblyResolveInfo>();

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
            if (InfoDict.TryGetValue(name, out AssemblyResolveInfo resolveInfo))
                return resolveInfo;
            lock (InfoDict)
                InfoDict[name] = new AssemblyResolveInfo();
            return InfoDict[name];
        }

        private static Assembly Resolve(string requested_name, Version requested_version, bool is_preload)
        {
            // Get Resolve Information Object
            AssemblyResolveInfo resolveInfo = GetInfo(requested_name);

            // Resolve the Information Object
            Assembly assembly = resolveInfo.Resolve(requested_version);

            // Run Passthrough Events
            if (assembly == null)
                assembly = MelonAssemblyResolver.SafeInvoke_OnAssemblyResolve(requested_name, requested_version);

            // Search Directories
            if (is_preload && (assembly == null))
                assembly = SearchDirectoryManager.Scan(requested_name);

            // Load if Valid Assembly
            if (assembly != null)
                LoadInfo(assembly);

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

#if NET6_0_OR_GREATER

        private static Assembly? Resolve(AssemblyLoadContext alc, AssemblyName name)
            => Resolve(name.Name, name.Version, true);

        private static void InstallHooks()
        {
            AssemblyLoadContext.Default.Resolving += Resolve;
        }

#else

        private static Assembly Resolve(string requested_name, ushort major, ushort minor, ushort build, ushort revision, bool is_preload)
        {
            Version requested_version = new Version(major, minor, build, revision);
            return Resolve(requested_name, requested_version, is_preload);
        }

        [MethodImpl(MethodImplOptions.InternalCall)]
        private extern static void InstallHooks();

#endif
    }
}
