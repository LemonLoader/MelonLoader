using MelonLoader.Bootstrap.Utils;
using System.Runtime.InteropServices;

namespace MelonLoader.Bootstrap.RuntimeHandlers.Il2Cpp;

internal class Il2CppLib(Il2CppLib.MethodGetNameFn methodGetName)
{
    public required nint Handle { get; init; }

    public required InitFn Init { get; init; }
    public required RuntimeInvokeFn RuntimeInvoke { get; init; }

    public static Il2CppLib? TryLoad(nint hRuntime)
    {
        if (!NativeFunc.GetExport<InitFn>(hRuntime, "il2cpp_init", out var init)
            || !NativeFunc.GetExport<RuntimeInvokeFn>(hRuntime, "il2cpp_runtime_invoke", out var runtimeInvoke)
            || !NativeFunc.GetExport<MethodGetNameFn>(hRuntime, "il2cpp_method_get_name", out var methodGetName))
            return null;

        return new(methodGetName)
        {
            Handle = hRuntime,
            Init = init,
            RuntimeInvoke = runtimeInvoke
        };
    }

    public string? GetMethodName(nint method)
    {
        return method == 0 ? null : Marshal.PtrToStringAnsi(methodGetName(method));
    }

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate nint InitFn(nint a);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate nint RuntimeInvokeFn(nint method, nint obj, nint args, nint exc);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    public delegate nint MethodGetNameFn(nint method);
}
