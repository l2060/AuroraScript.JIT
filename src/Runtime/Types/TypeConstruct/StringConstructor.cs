using System;

namespace AuroraScript.Runtime.Types.TypeConstruct
{
    /// <summary>
    /// Represents the native 'String' constructor function in AuroraScript.
    /// Provides methods for character conversion and string comparison.
    /// </summary>
    internal class StringConstructor : ScriptType
    {
        /// <summary> The global singleton instance of the String constructor. </summary>
        internal readonly static StringConstructor INSTANCE = new StringConstructor();

        internal StringConstructor() : base("String", true)
        {
            Define("fromCharCode", ScriptDatum.FromBonding(FROMCHARCODE), writeable: false, enumerable: false);
            Define("valueOf", ScriptDatum.FromBonding(CONSTRUCTOR), writeable: false, enumerable: false);
            Define("compare", ScriptDatum.FromBonding(COMPARE), writeable: false, enumerable: false);
            Frozen();
        }

        public override void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetString(0, out var str))
            {
                ScriptDatum.WriteAsString(ref result, str);
            }
            else
            {
                ScriptDatum.WriteAsString(ref result, StringValue.Empty);
            }
        }


        /// <summary> Native implementation for String.fromCharCode(). </summary>
        internal static void FROMCHARCODE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetInteger(0, out var codePoint))
            {
                ScriptDatum.WriteAsString(ref result, StringValue.FromChar((char)codePoint));
            }
            else
            {
                ScriptDatum.WriteAsString(ref result, StringValue.Empty);
            }
        }

        /// <summary> Native implementation for the String constructor (String()). </summary>
        internal static void CONSTRUCTOR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetString(0, out var str))
            {
                ScriptDatum.WriteAsString(ref result, str);
            }
            else
            {
                ScriptDatum.WriteAsString(ref result, StringValue.Empty);
            }
        }

        /// <summary> Native implementation for comparing two strings. </summary>
        internal static void COMPARE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetString(0, out var a) && args.TryGetString(1, out var b) && a.Length > 0 && b.Length > 0)
            {
                var charA = a[0];
                var charB = b[0];
                ScriptDatum.WriteAsNumber(ref result, charA.CompareTo(charB));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, 1);
            }
        }
    }
}
