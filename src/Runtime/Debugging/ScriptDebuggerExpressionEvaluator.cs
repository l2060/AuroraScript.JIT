using AuroraScript.Runtime.Types;
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

        /// <summary>Gets a debugger pseudo variable such as global, $state, or $args.</summary>
        public static ScriptDatum GetSpecial(ScriptContext context, string name, ScriptDatum[] arguments)
        {
            if (string.Equals(name, "global", StringComparison.Ordinal))
            {
                return context?.Global != null ? ScriptDatum.FromObject(context.Global) : ScriptDatum.Null;
            }

            if (string.Equals(name, "$state", StringComparison.Ordinal))
            {
                return context?.UserState != null ? ScriptDatum.FromObject(context.UserState) : ScriptDatum.Null;
            }

            if (string.Equals(name, "$args", StringComparison.Ordinal))
            {
                return arguments == null ? ScriptDatum.FromObject(new ScriptArray(0)) : ScriptDatum.FromObject(new ScriptArray(arguments));
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

        /// <summary>Packs zero fast-call arguments for the debugger.</summary>
        public static ScriptDatum[] PackArguments()
        {
            return Array.Empty<ScriptDatum>();
        }

        /// <summary>Packs span-call arguments for the debugger.</summary>
        public static ScriptDatum[] PackSpanArguments(Span<ScriptDatum> args)
        {
            return args.ToArray();
        }

        /// <summary>Packs one fast-call argument for the debugger.</summary>
        public static ScriptDatum[] PackArguments(ScriptDatum arg0)
        {
            return new[] { arg0 };
        }

        /// <summary>Packs two fast-call arguments for the debugger.</summary>
        public static ScriptDatum[] PackArguments(ScriptDatum arg0, ScriptDatum arg1)
        {
            return new[] { arg0, arg1 };
        }

        /// <summary>Packs three fast-call arguments for the debugger.</summary>
        public static ScriptDatum[] PackArguments(ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2)
        {
            return new[] { arg0, arg1, arg2 };
        }

        /// <summary>Packs four fast-call arguments for the debugger.</summary>
        public static ScriptDatum[] PackArguments(ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3)
        {
            return new[] { arg0, arg1, arg2, arg3 };
        }

        /// <summary>Packs five fast-call arguments for the debugger.</summary>
        public static ScriptDatum[] PackArguments(ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4)
        {
            return new[] { arg0, arg1, arg2, arg3, arg4 };
        }

        /// <summary>Packs six fast-call arguments for the debugger.</summary>
        public static ScriptDatum[] PackArguments(ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5)
        {
            return new[] { arg0, arg1, arg2, arg3, arg4, arg5 };
        }

        /// <summary>Packs seven fast-call arguments for the debugger.</summary>
        public static ScriptDatum[] PackArguments(ScriptDatum arg0, ScriptDatum arg1, ScriptDatum arg2, ScriptDatum arg3, ScriptDatum arg4, ScriptDatum arg5, ScriptDatum arg6)
        {
            return new[] { arg0, arg1, arg2, arg3, arg4, arg5, arg6 };
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
