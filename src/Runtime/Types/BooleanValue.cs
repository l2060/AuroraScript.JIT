using System;
using System.Runtime.CompilerServices;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a boolean value in AuroraScript.
    /// This is an immutable object wrapping a CLI <see cref="bool"/>.
    /// </summary>
    public sealed class BooleanValue : ScriptImmutable
    {
        /// <summary> The singleton instance representing the 'true' value. </summary>
        public readonly static BooleanValue True = new BooleanValue(true, 1, new StringValue("true"));

        /// <summary> The singleton instance representing the 'false' value. </summary>
        public readonly static BooleanValue False = new BooleanValue(false, 0, new StringValue("false"));

        /// <summary> The integer representation of the boolean (1 for true, 0 for false). </summary>
        public readonly int IntValue;

        /// <summary> The underlying CLI boolean value. </summary>
        public readonly bool Value;

        /// <summary> The string representation of the boolean. </summary>
        public readonly StringValue StrValue;

        internal override ScriptDatum TypeOfValue => TypeNames.Boolean;

        private BooleanValue(bool val, int intVal, StringValue valueString) : base(Prototypes.BooleanValuePrototype)
        {
            Value = val;
            IntValue = intVal;
            StrValue = valueString;
        }

        /// <summary>
        /// Native implementation for the 'toString' method.
        /// </summary>
        internal new static void TOSTRING(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is BooleanValue boolean)
            {
                ScriptDatum.WriteAsString(ref result, boolean.StrValue);
            }
            else
            {
                ScriptDatum.WriteAsString(ref result, StringValue.FALSE);
            }
        }

        /// <summary>
        /// Returns the string representation of the boolean.
        /// </summary>
        /// <returns>Either "true" or "false".</returns>
        public override string ToString()
        {
            return StrValue.Value;
        }

        /// <summary>
        /// Returns the singleton instance corresponding to the given boolean value.
        /// </summary>
        /// <param name="value">The boolean value.</param>
        /// <returns>The <see cref="BooleanValue"/> singleton.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static BooleanValue Of(bool value)
        {
            return value ? True : False;
        }

        /// <summary>
        /// Checks if this boolean represents a "truthy" value.
        /// </summary>
        /// <returns>True if the value is true; otherwise, false.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool IsTrue()
        {
            return Value;
        }
    }
}
