using AuroraScript.Core;
using System;
using AuroraScript.Hosting;
using System.Globalization;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Partial implementation of <see cref="NumberValue"/> providing constants and native method implementations.
    /// This fragment handles specialized numeric operations and string conversions.
    /// </summary>
    [AuroraNativeType("Number", NativeReceiverType = typeof(double), NativeConstructor = nameof(CreateCore))]
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

        private static readonly NumberValue NaNValue = new NumberValue(double.NaN);

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

            if (args.Length == 1 && args[0].Kind == ValueKind.Number)
            {
                ScriptDatum.WriteAsString(ref result, FormatString(thisNumber._value, (int)args[0].Number));
                return;
            }
            ScriptDatum.WriteAsString(ref result, FormatString(thisNumber._value));
        }

        /// <summary>Formats a Number using the same rules as its script toString method.</summary>
        [AuroraExport("toString", DynamicAdapter = nameof(TOSTRING), Target = AuroraExportTarget.Instance)]
        public static string FormatString(double value) => (value == 0 ? 0d : value).ToString();

        /// <summary>Only radix 16 selects hexadecimal, as in the dynamic method.</summary>
        [AuroraExport("toString", DynamicAdapter = nameof(TOSTRING), Target = AuroraExportTarget.Instance)]
        public static string FormatString(double value, int radix)
            => radix == 16 ? ((int)value).ToString("X") : FormatString(value);

        /// <summary>Formats an Int32 Number without a floating-point round trip.</summary>
        [AuroraExport("toString", DynamicAdapter = nameof(TOSTRING), Target = AuroraExportTarget.Instance)]
        public static string FormatString(int value) => value.ToString();

        /// <summary>Formats an Int32 Number with an integer radix.</summary>
        [AuroraExport("toString", DynamicAdapter = nameof(TOSTRING), Target = AuroraExportTarget.Instance)]
        public static string FormatString(int value, int radix) => radix == 16 ? value.ToString("X") : value.ToString();

        /// <summary>Formats a UInt32 Number without losing its unsigned value.</summary>
        [AuroraExport("toString", DynamicAdapter = nameof(TOSTRING), Target = AuroraExportTarget.Instance)]
        public static string FormatString(uint value) => value.ToString();

        /// <summary>Preserves Number's historical Int32 hexadecimal conversion for large UInt32 values.</summary>
        [AuroraExport("toString", DynamicAdapter = nameof(TOSTRING), Target = AuroraExportTarget.Instance)]
        public static string FormatString(uint value, int radix) => radix == 16
            ? value <= int.MaxValue ? ((int)value).ToString("X") : FormatString((double)value, radix)
            : value.ToString();

        /// <summary>Formats all 64 signed bits without conversion to Number.</summary>
        public static string FormatString(long value) => value.ToString(CultureInfo.InvariantCulture);

        /// <summary>Formats Int64 as full-width hexadecimal for radix 16, otherwise decimal.</summary>
        public static string FormatString(long value, int radix) => value.ToString(radix == 16 ? "X" : "D", CultureInfo.InvariantCulture);

        /// <summary>Formats all 64 unsigned bits without conversion to Number.</summary>
        public static string FormatString(ulong value) => value.ToString(CultureInfo.InvariantCulture);

        /// <summary>Formats UInt64 as full-width hexadecimal for radix 16, otherwise decimal.</summary>
        public static string FormatString(ulong value, int radix) => value.ToString(radix == 16 ? "X" : "D", CultureInfo.InvariantCulture);
    }
}
