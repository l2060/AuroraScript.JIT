using AuroraScript.Hosting;
using System;
using System.Globalization;

namespace AuroraScript.Runtime.Types
{
    public sealed partial class NumberValue
    {
        /// <summary>The largest finite Number value.</summary>
        [Export("MAX_VALUE")]
        public static readonly double MaxValue = double.MaxValue;

        /// <summary>The historical minimum Number constant.</summary>
        [Export("MIN_VALUE")]
        public static readonly double MinValue = double.MinValue;

        /// <summary>The largest integer represented exactly by Number.</summary>
        [Export("MAX_SAFE_INTEGER")]
        public static readonly double MaxSafeInteger = 9_007_199_254_740_991d;

        /// <summary>The smallest integer represented exactly by Number.</summary>
        [Export("MIN_SAFE_INTEGER")]
        public static readonly double MinSafeInteger = -9_007_199_254_740_991d;

        /// <summary>The Number NaN constant.</summary>
        [Export("NaN")]
        public static readonly double NotANumber = double.NaN;

        /// <summary>The positive infinity Number constant.</summary>
        [Export("POSITIVE_INFINITY")]
        public static readonly double PositiveInfinity = double.PositiveInfinity;

        /// <summary>The negative infinity Number constant.</summary>
        [Export("NEGATIVE_INFINITY")]
        public static readonly double NegativeInfinity = double.NegativeInfinity;

        /// <summary>Primitive constructor and Number.valueOf for proven numeric arguments.</summary>
        [Export("valueOf", DynamicAdapter = nameof(CREATE))]
        public static double CreateCore(double value = double.NaN) => value;

        /// <summary>Parses a Number using invariant floating-point syntax.</summary>
        [Export("parseFloat", DynamicAdapter = nameof(PARSE_FLOAT))]
        public static double ParseFloatCore(string value) =>
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? number
                : double.NaN;

        /// <summary>Parses and truncates a Number using invariant floating-point syntax.</summary>
        [Export("parseInt", DynamicAdapter = nameof(PARSE_INTEGER))]
        public static double ParseIntegerCore(string value) =>
            double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var number)
                ? Math.Truncate(number)
                : double.NaN;

        /// <summary>Returns whether a Number is NaN.</summary>
        [Export("isNaN", DynamicAdapter = nameof(IS_NAN))]
        public static bool IsNaNCore(double value) => double.IsNaN(value);

        /// <summary>Returns whether a Number is integral.</summary>
        [Export("isInteger", DynamicAdapter = nameof(IS_INTEGER))]
        public static bool IsIntegerCore(double value) => double.IsInteger(value);

        /// <summary>Returns whether a Number is positive or negative infinity.</summary>
        [Export("isInfinity", DynamicAdapter = nameof(IS_INFINITY))]
        public static bool IsInfinityCore(double value) => double.IsInfinity(value);

        private static void CREATE(
            ScriptContext context,
            ScriptObject thisObject,
            Span<ScriptDatum> arguments,
            ref ScriptDatum result)
        {
            ScriptDatum.WriteAsNumber(
                ref result,
                arguments.TryGetNumber(0, out var number) ? number : double.NaN);
        }

        private static void PARSE_FLOAT(
            ScriptContext context,
            ScriptObject thisObject,
            Span<ScriptDatum> arguments,
            ref ScriptDatum result)
        {
            if (arguments.TryGetNumber(0, out var number))
            {
                ScriptDatum.WriteAsNumber(ref result, number);
            }
            else if (arguments.TryGetString(0, out var text))
            {
                ScriptDatum.WriteAsNumber(ref result, ParseFloatCore(text));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, double.NaN);
            }
        }

        private static void PARSE_INTEGER(
            ScriptContext context,
            ScriptObject thisObject,
            Span<ScriptDatum> arguments,
            ref ScriptDatum result)
        {
            if (arguments.TryGetInteger(0, out var number))
            {
                ScriptDatum.WriteAsNumber(ref result, number);
            }
            else if (arguments.TryGetString(0, out var text))
            {
                ScriptDatum.WriteAsNumber(ref result, ParseIntegerCore(text));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, double.NaN);
            }
        }

        private static void IS_NAN(
            ScriptContext context,
            ScriptObject thisObject,
            Span<ScriptDatum> arguments,
            ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(
                ref result,
                arguments.TryGetStrictNumber(0, out var number) && IsNaNCore(number));
        }

        private static void IS_INTEGER(
            ScriptContext context,
            ScriptObject thisObject,
            Span<ScriptDatum> arguments,
            ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(
                ref result,
                arguments.TryGetStrictNumber(0, out var number) && IsIntegerCore(number));
        }

        private static void IS_INFINITY(
            ScriptContext context,
            ScriptObject thisObject,
            Span<ScriptDatum> arguments,
            ref ScriptDatum result)
        {
            ScriptDatum.WriteAsBoolean(
                ref result,
                arguments.TryGetStrictNumber(0, out var number) && IsInfinityCore(number));
        }
    }
}
