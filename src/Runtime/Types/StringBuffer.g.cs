using System;
using System.Collections.Generic;
using System.Text;

namespace AuroraScript.Runtime.Types
{
    public sealed partial class StringBuffer
    {
        internal static void TO_STRING(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                ScriptDatum.WriteAsString(ref result, builder._builder.ToString());
            }
        }

        internal static void APPEND(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    builder._builder.Append(ScriptDatum.ToString(args[i]));
                }
            }
        }
        internal static void INSERT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                if (args.TryGetInteger(0, out var index) && args.TryGetString(1, out var str))
                {
                    builder._builder.Insert((int)index, str);
                }
            }
        }
        internal static void APPEND_LINE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    builder._builder.Append(ScriptDatum.ToString(args[i]));
                }
                builder._builder.AppendLine();
            }
        }
        internal static void CLEAR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                builder._builder.Clear();
            }
        }


    }
}
