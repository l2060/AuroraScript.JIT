using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime
{
    /// <summary>Resolves delegates used by dynamically generated closure objects.</summary>
    internal static class ClosureOps
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate Resolve(int id) =>
            DynamicMethodRegistry.Resolve(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate0 Resolve0(int id) =>
            DynamicMethodRegistry.Resolve0(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate1 Resolve1(int id) =>
            DynamicMethodRegistry.Resolve1(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate2 Resolve2(int id) =>
            DynamicMethodRegistry.Resolve2(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate3 Resolve3(int id) =>
            DynamicMethodRegistry.Resolve3(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate4 Resolve4(int id) =>
            DynamicMethodRegistry.Resolve4(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate5 Resolve5(int id) =>
            DynamicMethodRegistry.Resolve5(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate6 Resolve6(int id) =>
            DynamicMethodRegistry.Resolve6(id);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptFunctionDelegate7 Resolve7(int id) =>
            DynamicMethodRegistry.Resolve7(id);
    }
}
