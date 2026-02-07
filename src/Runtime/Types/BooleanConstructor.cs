using System;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents the native 'Boolean' constructor function in AuroraScript.
    /// Used for creating boolean values or coercing other types to boolean.
    /// </summary>
    internal class BooleanConstructor : BondingFunction
    {
        /// <summary> The global singleton instance of the Boolean constructor. </summary>
        internal readonly static BooleanConstructor INSTANCE = new BooleanConstructor();

        internal BooleanConstructor() : base(CONSTRUCTOR)
        {
            _prototype = Prototypes.BooleanConstructorPrototype;
        }

        /// <summary>
        /// Parses a value as a boolean.
        /// </summary>
        internal static void PARSE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, args.TryGetRef(0, ref result) && ScriptDatum.IsTrue(result));
        }

        /// <summary>
        /// Native implementation for calling Boolean() as a constructor or function.
        /// </summary>
        internal static void CONSTRUCTOR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, args.TryGetRef(0, ref result) && ScriptDatum.IsTrue(result));
        }
    }
}
