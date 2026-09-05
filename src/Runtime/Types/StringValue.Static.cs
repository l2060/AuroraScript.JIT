using AuroraScript.Hosting;
using System;

namespace AuroraScript.Runtime.Types
{
    public sealed partial class StringValue
    {
        /// <summary>Primitive constructor and String.valueOf for proven string arguments.</summary>
        [AuroraExport("valueOf", DynamicAdapter = nameof(CREATE))]
        public static string CreateCore(string value = "") => value;

        /// <summary>Preserves the first UTF-16 code-unit conversion, without a StringValue wrapper.</summary>
        [AuroraExport("fromCharCode", DynamicAdapter = nameof(FROMCHARCODE))]
        public static string FromCharCodeCore(int code) => TextFromChar((char)code);

        /// <summary>Compares the first UTF-16 code units, retaining the historical empty-string result.</summary>
        [AuroraExport("compare", DynamicAdapter = nameof(COMPARE))]
        public static int CompareCore(string left, string right) => left.Length > 0 && right.Length > 0 ? left[0].CompareTo(right[0]) : 1;

        private static void CREATE(ScriptContext ctx, ScriptObject self, Span<ScriptDatum> args, ref ScriptDatum result) => ScriptDatum.WriteAsString(ref result, args.TryGetString(0, out var value) ? value : string.Empty);

        private static void FROMCHARCODE(ScriptContext ctx, ScriptObject self, Span<ScriptDatum> args, ref ScriptDatum result) => ScriptDatum.WriteAsString(ref result, args.TryGetInteger(0, out var code) ? FromCharCodeCore(unchecked((int)code)) : string.Empty);

        private static void COMPARE(ScriptContext ctx, ScriptObject self, Span<ScriptDatum> args, ref ScriptDatum result) => ScriptDatum.WriteAsNumber(ref result, args.TryGetString(0, out var left) && args.TryGetString(1, out var right) ? CompareCore(left, right) : 1);
    }
}
