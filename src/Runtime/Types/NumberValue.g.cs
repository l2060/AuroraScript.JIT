using AuroraScript.Core;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Partial implementation of <see cref="NumberValue"/> providing constants and native method implementations.
    /// This fragment handles specialized numeric operations and string conversions.
    /// </summary>
    public partial class NumberValue
    {

        /// <summary> A numeric value representing -1. </summary>
        public static readonly NumberValue Negative1 = new NumberValue(-1);

        /// <summary> A numeric value representing 0. </summary>
        public static readonly NumberValue Zero = new NumberValue(0);

        /// <summary> A numeric value representing 1. </summary>
        public static readonly NumberValue Num1 = new NumberValue(1);

        /// <summary> A numeric value representing 2. </summary>
        public static readonly NumberValue Num2 = new NumberValue(2);

        /// <summary> A numeric value representing 3. </summary>
        public static readonly NumberValue Num3 = new NumberValue(3);

        /// <summary> A numeric value representing 4. </summary>
        public static readonly NumberValue Num4 = new NumberValue(4);

        /// <summary> A numeric value representing 5. </summary>
        public static readonly NumberValue Num5 = new NumberValue(5);

        /// <summary> A numeric value representing 6. </summary>
        public static readonly NumberValue Num6 = new NumberValue(6);

        /// <summary> A numeric value representing 7. </summary>
        public static readonly NumberValue Num7 = new NumberValue(7);

        /// <summary> A numeric value representing 8. </summary>
        public static readonly NumberValue Num8 = new NumberValue(8);

        /// <summary> A numeric value representing 9. </summary>
        public static readonly NumberValue Num9 = new NumberValue(9);

        /// <summary> Represents the value of positive infinity. </summary>
        internal readonly static NumberValue POSITIVE_INFINITY = new NumberValue(double.PositiveInfinity);
        /// <summary> Represents the value of negative infinity. </summary>
        internal readonly static NumberValue NEGATIVE_INFINITY = new NumberValue(double.NegativeInfinity);
        /// <summary> Represents the 'Not-a-Number' (NaN) value. </summary>
        internal readonly static NumberValue NaN = new NumberValue(double.NaN);
        /// <summary> Represents the largest representable numeric value. </summary>
        internal readonly static NumberValue MAX_VALUE = new NumberValue(double.MaxValue);
        /// <summary> Represents the smallest representable numeric value. </summary>
        internal readonly static NumberValue MIN_VALUE = new NumberValue(double.MinValue);
        /// <summary> Represents the maximum safe integer in AuroraScript (+9,007,199,254,740,991). </summary>
        internal static readonly NumberValue MAX_SAFE_INTEGER = new NumberValue(+9_007_199_254_740_991);
        /// <summary> Represents the minimum safe integer in AuroraScript (-9,007,199,254,740,991). </summary>
        internal static readonly NumberValue MIN_SAFE_INTEGER = new NumberValue(-9_007_199_254_740_991);

        /// <summary>
        /// Native implementation for Number.toString().
        /// Supports an optional radix argument (optimized for radix 16/hex).
        /// </summary>
        /// <param name="ctx">The current script context.</param>
        /// <param name="thisObject">The numeric operand.</param>
        /// <param name="args">Optional radix (base).</param>
        /// <param name="result">The resulting string value.</param>
        internal new static void TOSTRING(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not NumberValue thisNumber)
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }

            if (args != null && args.Length == 1)
            {
                var arg = args[0];
                if (arg.Kind == ValueKind.Number)
                {
                    if ((int)arg.Number == 16)
                    {
                        ScriptDatum.WriteAsString(ref result, thisNumber.Int32Value.ToString("X"));
                        return;
                    }
                }
            }
            ScriptDatum.WriteAsString(ref result, thisNumber._value.ToString());
        }
    }
}
