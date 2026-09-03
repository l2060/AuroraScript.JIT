using System;
using System.Reflection;

namespace AuroraScript.Runtime.Debugging
{
    /// <summary>
    /// Helper entry points used by the Visual Studio debugger expression evaluator.
    /// These methods are only called from debugger-generated inspection queries.
    /// </summary>
    public static class ScriptDebuggerExpressionEvaluator
    {
        /// <summary>Gets a module property for a debugger expression.</summary>
        public static ScriptDatum GetModuleProperty(ScriptContext context, string name)
        {
            if (context == null || context.Module == null || string.IsNullOrEmpty(name))
            {
                return ScriptDatum.Null;
            }

            return context.Module.GetPropertyDatum(context, name);
        }

        /// <summary>Gets a global property for a debugger expression.</summary>
        public static ScriptDatum GetGlobalProperty(ScriptContext context, string name)
        {
            if (context == null || context.Global == null || string.IsNullOrEmpty(name))
            {
                return ScriptDatum.Null;
            }

            return context.Global.GetPropertyDatum(context, name);
        }

        /// <summary>Gets a debugger pseudo variable such as global.</summary>
        public static ScriptDatum GetSpecial(ScriptContext context, string name)
        {
            if (string.Equals(name, "global", StringComparison.Ordinal))
            {
                return context?.Global != null ? ScriptDatum.FromObject(context.Global) : ScriptDatum.Null;
            }

            return ScriptDatum.Null;
        }

        /// <summary>Gets an inherited closure upvalue by index.</summary>
        public static ScriptDatum GetUpvalue(ScriptContext context, int index)
        {
            return TryGetUpvalueArray(context, out var upvalues) &&
                (uint)index < (uint)upvalues.Length
                ? GetUpvalueValue(upvalues.GetValue(index))
                : ScriptDatum.Null;
        }

        /// <summary>Gets a captured local value by index from a captured-upvalue array.</summary>
        public static ScriptDatum GetCapturedLocal(object capturedUpvalues, int index)
        {
            return TryGetUpvalueArray(capturedUpvalues, out var upvalues) &&
                (uint)index < (uint)upvalues.Length
                ? GetUpvalueValue(upvalues.GetValue(index))
                : ScriptDatum.Null;
        }

        private static bool TryGetUpvalueArray(ScriptContext context, out Array upvalues)
        {
            upvalues = null;
            if (context == null)
            {
                return false;
            }

            upvalues = context.Target != null
                ? GetPrivateField<Array>(context.Target, "Upvalues")
                : GetPrivateField<Array>(context, "Upvalues");
            return upvalues != null;
        }

        private static bool TryGetUpvalueArray(object value, out Array upvalues)
        {
            upvalues = value as Array;
            return upvalues != null;
        }

        private static ScriptDatum GetUpvalueValue(object upvalue)
        {
            if (upvalue == null)
            {
                return ScriptDatum.Null;
            }

            var field = upvalue.GetType().GetField("Value", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return field != null && field.GetValue(upvalue) is ScriptDatum value ? value : ScriptDatum.Null;
        }

        private static T GetPrivateField<T>(object instance, string name)
            where T : class
        {
            return instance?.GetType().GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(instance) as T;
        }
    }
}
