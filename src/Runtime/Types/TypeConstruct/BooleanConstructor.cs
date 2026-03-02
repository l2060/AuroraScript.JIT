using System;

namespace AuroraScript.Runtime.Types.TypeConstruct
{
    /// <summary>
    /// Represents the native 'Boolean' constructor function in AuroraScript.
    /// Used for creating boolean values or coercing other types to boolean.
    /// </summary>
    internal class BooleanConstructor : ScriptType
    {
        /// <summary> The global singleton instance of the Boolean constructor. </summary>
        internal readonly static BooleanConstructor INSTANCE = new BooleanConstructor();

        internal BooleanConstructor() : base("Boolean", true)
        {
            Define("true", BooleanValue.True, writeable: false, enumerable: false);
            Define("false", BooleanValue.False, writeable: false, enumerable: false);
            Define("valueOf", new BondingFunction(PARSE), writeable: false, enumerable: false);
            Frozen();
        }

        public override void Construct(ScriptContext ctx, ScriptDatum[] args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, args.TryGetRef(0, ref result) && ScriptDatum.IsTrue(result));
        }

        /// <summary>
        /// Parses a value as a boolean.
        /// </summary>
        internal static void PARSE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, args.TryGetRef(0, ref result) && ScriptDatum.IsTrue(result));
        }
    }
}
