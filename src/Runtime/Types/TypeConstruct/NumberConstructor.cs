using System;

namespace AuroraScript.Runtime.Types.TypeConstruct
{
    /// <summary>
    /// Represents the native 'Number' constructor function in AuroraScript.
    /// Provides access to numeric constants and parsing methods.
    /// </summary>
    internal class NumberConstructor : ScriptType
    {


        /// <summary> The global singleton instance of the Number constructor. </summary>
        internal readonly static NumberConstructor INSTANCE = new NumberConstructor();

        internal NumberConstructor() : base("Number", true)
        {
            Define("MAX_VALUE", NumberValue.MAX_VALUE, writeable: false, enumerable: false);
            Define("MIN_VALUE", NumberValue.MIN_VALUE, writeable: false, enumerable: false);

            Define("MAX_SAFE_INTEGER", NumberValue.MAX_SAFE_INTEGER, writeable: false, enumerable: false);
            Define("MIN_SAFE_INTEGER", NumberValue.MIN_SAFE_INTEGER, writeable: false, enumerable: false);

            Define("NaN", NumberValue.NaN, writeable: false, enumerable: false);

            Define("POSITIVE_INFINITY", NumberValue.POSITIVE_INFINITY, writeable: false, enumerable: false);
            Define("NEGATIVE_INFINITY", NumberValue.NEGATIVE_INFINITY, writeable: false, enumerable: false);

            Define("isNaN", ScriptDatum.FromBonding(IS_NAN), writeable: false, enumerable: false);
            Define("isInteger", ScriptDatum.FromBonding(IS_INTEGER), writeable: false, enumerable: false);
            Define("isInfinity", ScriptDatum.FromBonding(IS_INFINITY), writeable: false, enumerable: false);

            Define("parseFloat", ScriptDatum.FromBonding(PARSE_FLOAT), writeable: false, enumerable: false);
            Define("parseInt", ScriptDatum.FromBonding(PARSE_INTEGER), writeable: false, enumerable: false);

            Frozen();
        }

        public override void Construct(ScriptContext ctx, Span<ScriptDatum> args, ref ScriptDatum result)
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
            else if (args.TryGetString(0, out var text) && double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out number))
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
            else if (args.TryGetString(0, out var text) && double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var parsed))
            {
                ScriptDatum.WriteAsNumber(ref result, Math.Truncate(parsed));
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
