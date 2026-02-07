using System;
using System.Runtime.CompilerServices;

namespace AuroraScript
{
    internal static class ThrowHelper
    {


        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotfoundProperty(String key) => throw new AuroraRuntimeException("Cannot found property");




        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowEmptyStack() => throw new AuroraRuntimeException("Stack is empty.");


        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowSwapUnderflow() => throw new AuroraRuntimeException("Stack has fewer than two elements.");



        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotConstructor(String type) => throw new AuroraRuntimeException($"TypeError: {type} is not a constructor.");



        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowProxyConstructor() => throw new AuroraRuntimeException($"The options of Proxy must include get and set methods.");

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidHotPatchParam(String paramName) => throw new AuroraRuntimeException($"Invalid HotPatch parameter \"{paramName}\".");



        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowFrozen() => throw new AuroraRuntimeException("You cannot modify this object");


        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowDisableWritable() => throw new AuroraRuntimeException("Write operations cannot be performed on read-only properties");
    }
}
