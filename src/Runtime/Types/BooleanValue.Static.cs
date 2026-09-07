using AuroraScript.Hosting;
using System;

namespace AuroraScript.Runtime.Types
{
    public sealed partial class BooleanValue
    {
        /// <summary>The script Boolean.true constant.</summary>
        [Export("true")]
        public static readonly bool TrueConstant = true;

        /// <summary>The script Boolean.false constant.</summary>
        [Export("false")]
        public static readonly bool FalseConstant = false;

        /// <summary>Primitive construction and Boolean.valueOf for proven booleans.</summary>
        [Export("valueOf", DynamicAdapter = nameof(CREATE))]
        public static bool CreateCore(bool value = false) => value;

        /// <summary>Preserves truthiness conversion, missing arguments and surplus arguments.</summary>
        internal static void CREATE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, args.TryGetRef(0, ref result) && ScriptDatum.IsTrue(result));
        }
    }
}
