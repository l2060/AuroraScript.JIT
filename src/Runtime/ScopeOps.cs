using AuroraScript.Runtime.Types;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime
{
    /// <summary>Lexical boundary for module/global state used by generated code.</summary>
    internal static class ScopeOps
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetModule(ScriptContext context, string name) =>
            context.Module.GetPropertyDatum(context, name);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum SetModule(ScriptContext context, string name, ScriptDatum value)
        {
            context.Module.SetPropertyDatum(context, name, value);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetGlobal(ScriptContext context, string name) =>
            context.Global.GetPropertyDatum(context, name);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum SetGlobal(ScriptContext context, string name, ScriptDatum value)
        {
            context.Global.SetPropertyDatum(context, name, value);
            return value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetGlobalObject(ScriptContext context) =>
            ScriptDatum.FromObject(context.Global);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptDatum GetUserState(ScriptContext context) =>
            ScriptDatum.FromObject(context.UserState);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ScriptObject GetUserStateObject(ScriptContext context) =>
            context.UserState ?? ScriptObject.Null;
    }
}
