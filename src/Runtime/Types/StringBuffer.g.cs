using System;
using System.Collections.Generic;
using System.Text;
using AuroraScript.Hosting;

namespace AuroraScript.Runtime.Types
{
    public sealed partial class StringBuffer
    {
        internal static void CREATE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsObject(ref result, args.TryGetString(0, out var initialValue)
                ? new StringBuffer(initialValue) : new StringBuffer());
        }

        /// <summary>Appends one string with script null conversion.</summary>
        [Export("append", DynamicAdapter = nameof(APPEND))]
        public void AppendCore(string value) => GetBuilder().Append(value ?? "null");

        /// <summary>Appends one string and a platform newline.</summary>
        [Export("appendLine", DynamicAdapter = nameof(APPEND_LINE))]
        public void AppendLineCore(string value) => GetBuilder().Append(value ?? "null").AppendLine();

        /// <summary>Inserts text using the existing index coercion.</summary>
        [Export("insert", DynamicAdapter = nameof(INSERT))]
        public void InsertCore(ScriptDatum index, ScriptDatum value)
        {
            DatumBuffer2 args = default;
            args[0] = index;
            args[1] = value;
            var result = default(ScriptDatum);
            INSERT(null, this, args, ref result);
        }

        /// <summary>Clears text and dynamic members.</summary>
        [Export("clear", DynamicAdapter = nameof(CLEAR))]
        public void ClearCore() => Reset();

        /// <summary>Returns the text and releases the pooled builder.</summary>
        [Export("stringAndRelease", DynamicAdapter = nameof(STRINGANDRELEASE))]
        public string StringAndReleaseCore()
        {
            var text = ToString();
            Release();
            return text;
        }

        internal static void TO_STRING(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                ScriptDatum.WriteAsString(ref result, builder.ToString());
            }
        }

        internal static void APPEND(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    builder.GetBuilder().Append(ScriptDatum.ToString(args[i]));
                }
            }
        }
        internal static void INSERT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                if (args.TryGetInteger(0, out var index) && args.TryGetString(1, out var str))
                {
                    builder.GetBuilder().Insert((int)index, str);
                }
            }
        }
        internal static void APPEND_LINE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                for (int i = 0; i < args.Length; i++)
                {
                    builder.GetBuilder().Append(ScriptDatum.ToString(args[i]));
                }
                builder.GetBuilder().AppendLine();
            }
        }
        internal static void CLEAR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                builder.Reset();
            }
        }

        internal static void RELEASE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                builder.Release();
            }
        }

        internal static void STRINGANDRELEASE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringBuffer builder)
            {
                ScriptDatum.WriteAsString(ref result, builder.ToString());
                builder.Release();
            }
        }
    }
}
