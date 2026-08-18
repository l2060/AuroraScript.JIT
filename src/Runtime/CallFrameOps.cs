using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using AuroraScript.Runtime.Types;

namespace AuroraScript.Runtime
{
    /// <summary>Small helpers used by compiled entry adapters.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class CallFrameOps
    {
        /// <summary>Enters a statically bound script function without allocating a child context.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int EnterDirect(ScriptContext context, string functionName)
        {
            return context.EnterDirect(functionName);
        }

        /// <summary>Temporarily switches the active module without allocating a child context.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int EnterModule(ScriptContext context, ScriptModule module)
        {
            return context.EnterModule(module);
        }

        /// <summary>Restores the context state that preceded a lightweight call frame.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Leave(ScriptContext context, int restoreDepth)
        {
            context.LeaveFrame(restoreDepth);
        }

        /// <summary>Snapshots the script stack only on an exceptional path.</summary>
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void CaptureException(ScriptContext context)
        {
            context.CaptureExceptionStack();
        }

        /// <summary>Gets an argument or the script null value when it is absent.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetArgument(Span<ScriptDatum> arguments, int index)
        {
            return (uint)index < (uint)arguments.Length ? arguments[index] : default;
        }

        /// <summary>Gets an argument or a supplied default value when it is absent.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetArgumentOrDefault(
            Span<ScriptDatum> arguments,
            int index,
            ScriptDatum defaultValue)
        {
            return (uint)index < (uint)arguments.Length ? arguments[index] : defaultValue;
        }
    }
}
