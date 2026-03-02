using System;

namespace AuroraScript.Runtime.Types.TypeConstruct
{
    /// <summary>
    /// Represents the native 'Number' constructor function in AuroraScript.
    /// Provides access to numeric constants and parsing methods.
    /// </summary>
    internal class NumberConstructor : ScriptType
    {
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

        /// <summary> The global singleton instance of the Number constructor. </summary>
        internal readonly static NumberConstructor INSTANCE = new NumberConstructor();

        internal NumberConstructor() : base("Number", true)
        {
            Define("MAX_VALUE", MAX_VALUE, writeable: false, enumerable: false);
            Define("MIN_VALUE", MIN_VALUE, writeable: false, enumerable: false);

            Define("MAX_SAFE_INTEGER", MAX_SAFE_INTEGER, writeable: false, enumerable: false);
            Define("MIN_SAFE_INTEGER", MIN_SAFE_INTEGER, writeable: false, enumerable: false);

            Define("NaN", NaN, writeable: false, enumerable: false);

            Define("POSITIVE_INFINITY", POSITIVE_INFINITY, writeable: false, enumerable: false);
            Define("NEGATIVE_INFINITY", NEGATIVE_INFINITY, writeable: false, enumerable: false);

            Define("isNaN", new BondingFunction(IS_NAN), writeable: false, enumerable: false);
            Define("isInteger", new BondingFunction(IS_INTEGER), writeable: false, enumerable: false);
            Define("isInfinity", new BondingFunction(IS_INFINITY), writeable: false, enumerable: false);

            Define("parseFloat", new BondingFunction(PARSE_FLOAT), writeable: false, enumerable: false);
            Define("parseInt", new BondingFunction(PARSE_INTEGER), writeable: false, enumerable: false);

            Frozen();
        }

        public override void Construct(ScriptContext ctx, ScriptDatum[] args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var number))
            {
                ScriptDatum.WriteAsNumber(ref result, number);
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, double.NaN);
            }
        }



        /// <summary> Parses an argument as a floating-point number. </summary>
        internal static void PARSE_FLOAT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetNumber(0, out var number))
            {
                ScriptDatum.WriteAsNumber(ref result, number);
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, double.NaN);
            }
        }

        /// <summary> Parses an argument as an integer. </summary>
        internal static void PARSE_INTEGER(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetInteger(0, out var number))
            {
                ScriptDatum.WriteAsNumber(ref result, number);
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, double.NaN);
            }
        }

        /// <summary> Returns true if the provided value is NaN. </summary>
        internal static void IS_NAN(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, args.TryGetStrictNumber(0, out var num) && double.IsNaN(num));
        }

        /// <summary> Returns true if the provided value is an integer. </summary>
        internal static void IS_INTEGER(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, args.TryGetStrictNumber(0, out var num) && double.IsInteger(num));
        }

        /// <summary> Returns true if the provided value is Infinity. </summary>
        internal static void IS_INFINITY(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(ref result, args.TryGetStrictNumber(0, out var num) && double.IsInfinity(num));
        }
    }
}
