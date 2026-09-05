using AuroraScript.Core;
using AuroraScript.Runtime.Pool;
using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using Microsoft.VisualBasic;
using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Partial implementation of <see cref="StringValue"/> providing constants and native method implementations.
    /// This fragment exposes common string operations to the AuroraScript runtime.
    /// </summary>
    [AuroraNativeType("String", ConstructorFactory = nameof(CreateCore))]
    [AuroraNativeReceiver(typeof(string))]
    public partial class StringValue
    {
        /// <summary> An empty string value. </summary>
        public readonly static StringValue Empty = new StringValue("");


        /// <summary>Native read-only string length without a wrapper object.</summary>
        [AuroraExport("length", IsGetter = true)]
        [AuroraNativeReceiver]
        public static int LengthCore(string value) => ValueOps.GetStringLength(value);

        /// <summary>Lowercases using the current culture, matching the script API.</summary>
        [AuroraExport("toLowerCase")]
        [AuroraNativeReceiver]
        public static string ToLowerCaseCore(string value) => value.ToLower(CultureInfo.CurrentCulture);

        /// <summary>Uppercases using the current culture, matching the script API.</summary>
        [AuroraExport("toUpperCase")]
        [AuroraNativeReceiver]
        public static string ToUpperCaseCore(string value) => value.ToUpper(CultureInfo.CurrentCulture);

        /// <summary>Trims whitespace from both ends.</summary>
        [AuroraExport("trim")]
        [AuroraNativeReceiver]
        public static string TrimCore(string value) => value.Trim();

        /// <summary>Trims leading whitespace.</summary>
        [AuroraExport("trimLeft")]
        [AuroraNativeReceiver]
        public static string TrimLeftCore(string value) => value.TrimStart();

        /// <summary>Trims trailing whitespace.</summary>
        [AuroraExport("trimRight")]
        [AuroraNativeReceiver]
        public static string TrimRightCore(string value) => value.TrimEnd();

        /// <summary>Returns the raw string without materializing a wrapper.</summary>
        [AuroraExport("toString")]
        [AuroraNativeReceiver]
        public static string ToStringCore(string value) => value;

        /// <summary>Tests ordinal substring containment.</summary>
        [AuroraExport("contains", DynamicAdapter = nameof(CONTANINS))]
        [AuroraNativeReceiver]
        public static bool ContainsCore(string value, string search) => value.Contains(search);

        /// <summary>Returns the first ordinal match index or -1.</summary>
        [AuroraExport("indexOf", DynamicAdapter = nameof(INDEXOF))]
        [AuroraNativeReceiver]
        public static int IndexOfCore(string value, string search) => value.IndexOf(search, StringComparison.Ordinal);

        /// <summary>Returns the last ordinal match index or -1.</summary>
        [AuroraExport("lastIndexOf", DynamicAdapter = nameof(LASTINDEXOF))]
        [AuroraNativeReceiver]
        public static int LastIndexOfCore(string value, string search) => value.LastIndexOf(search, StringComparison.Ordinal);

        /// <summary>Tests an ordinal prefix.</summary>
        [AuroraExport("startsWith", DynamicAdapter = nameof(STARTSWITH))]
        [AuroraNativeReceiver]
        public static bool StartsWithCore(string value, string search) => value.StartsWith(search, StringComparison.Ordinal);

        /// <summary>Tests an ordinal suffix.</summary>
        [AuroraExport("endsWith", DynamicAdapter = nameof(ENDSWITH))]
        [AuroraNativeReceiver]
        public static bool EndsWithCore(string value, string search) => value.EndsWith(search, StringComparison.Ordinal);

        /// <summary>Native character access retaining NaN for invalid indices.</summary>
        [AuroraExport("charCodeAt", DynamicAdapter = nameof(CHARCODEAT))]
        [AuroraNativeReceiver]
        public static double CharCodeAtCore(string value, int index) => ValueOps.GetStringCharCodeAt(value, index);

        /// <summary>Native character access when the compiler has proven the index in bounds.</summary>
        [AuroraExport("charCodeAt", DynamicAdapter = nameof(CHARCODEAT), RequiresIndexProof = true)]
        [AuroraNativeReceiver]
        public static int CharCodeAtInt32Core(string value, int index) => ValueOps.GetStringCharCodeAtInt32(value, index);

        /// <summary>
        /// Native implementation for String.contains().
        /// Returns true if the search string is found within this string.
        /// </summary>
        internal static void CONTANINS(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var str = thisObject as StringValue;
            if (str != null && args.TryGetString(0, out var search))
            {
                ScriptDatum.WriteAsBoolean(ref result, ContainsCore(str.Value, search));
            }
            else
            {
                ScriptDatum.MarkAsNull(ref result);
            }
        }

        /// <summary>
        /// Native implementation for String.indexOf().
        /// Returns the index of the first occurrence of the specified search string, or -1 if not found.
        /// </summary>
        internal static void INDEXOF(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var str = thisObject as StringValue;

            if (str != null && args.TryGetString(0, out var search))
            {
                ScriptDatum.WriteAsNumber(ref result, IndexOfCore(str.Value, search));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, -1);
            }
        }

        /// <summary>
        /// Native implementation for String.lastIndexOf().
        /// Returns the index of the last occurrence of the specified search string, or -1 if not found.
        /// </summary>
        internal static void LASTINDEXOF(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var str = thisObject as StringValue;
            if (str != null && args.TryGetString(0, out var search))
            {
                ScriptDatum.WriteAsNumber(ref result, LastIndexOfCore(str.Value, search));
            }
            else
            {
                ScriptDatum.WriteAsNumber(ref result, -1);
            }
        }

        /// <summary>
        /// Native implementation for String.startsWith().
        /// Returns true if this string starts with the specified prefix.
        /// </summary>
        internal static void STARTSWITH(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var str = thisObject as StringValue;
            if (str != null && args.TryGetString(0, out var search))
            {
                ScriptDatum.WriteAsBoolean(ref result, StartsWithCore(str.Value, search));
            }
            else
            {
                ScriptDatum.WriteAsBoolean(ref result, false);
            }
        }

        /// <summary>
        /// Native implementation for String.endsWith().
        /// Returns true if this string ends with the specified suffix.
        /// </summary>
        internal static void ENDSWITH(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var str = thisObject as StringValue;
            if (str != null && args.TryGetString(0, out var search))
            {
                ScriptDatum.WriteAsBoolean(ref result, EndsWithCore(str.Value, search));
            }
            else
            {
                ScriptDatum.WriteAsBoolean(ref result, false);
            }
        }

        /// <summary>
        /// Native implementation for String.substring().
        /// Returns the part of the string between the start and end indexes, or to the end of the string.
        /// </summary>
        /// <param name="ctx">The current script context.</param>
        /// <param name="thisObject">The source string.</param>
        /// <param name="args">Arguments: [start, end (optional)].</param>
        /// <param name="result">The resulting substring.</param>
        internal static void SUBSTRING(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not StringValue str)
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }
            if (!args.TryGetInteger(0, out var start))
            {
                ScriptDatum.WriteAsString(ref result, str);
                return;
            }

            if (args.TryGetInteger(1, out var end))
            {
                ScriptDatum.WriteAsString(ref result, Substring(str.Value, start, end));
                return;
            }
            ScriptDatum.WriteAsString(ref result, Substring(str.Value, start));
        }

        /// <summary>Shared substring implementation; the second index is an end, not a length.</summary>
        public static string Substring(string value, long start, long end)
        {
            if (start > end) (start, end) = (end, start);
            var length = (int)Math.Clamp(end - start, 0, Math.Max(0, value.Length - start));
            return value.Substring(Math.Clamp((int)start, 0, value.Length), length);
        }

        /// <summary>Shared one-index substring implementation.</summary>
        public static string Substring(string value, long start)
            => value.Substring((int)Math.Clamp(start, 0, Math.Max(0, value.Length)));

        /// <summary>Native Int32 indices, preserving the dynamic range rules.</summary>
        [AuroraExport("substring", DynamicAdapter = nameof(SUBSTRING))]
        [AuroraNativeReceiver]
        public static string Substring(string value, int start, int end)
        {
            if (start > end) (start, end) = (end, start);
            // The common in-range path needs only Int32 arithmetic. Keep wide
            // arithmetic for negative/extreme indices to preserve legacy behavior.
            if ((uint)start <= (uint)value.Length && (uint)end <= (uint)value.Length) return value.Substring(start, end - start);
            return Substring(value, (long)start, (long)end);
        }

        /// <summary>Native Int32 start index without a Number/Int64 round trip.</summary>
        [AuroraExport("substring", DynamicAdapter = nameof(SUBSTRING))]
        [AuroraNativeReceiver]
        public static string Substring(string value, int start)
            => value.Substring(Math.Clamp(start, 0, value.Length));

        /// <summary>Preserves the Number-to-integer conversion used by TryGetInteger.</summary>
        public static string Substring(string value, double start, double end)
            => Substring(value, (long)start, (long)end);

        /// <summary>Preserves the Number-to-integer conversion used by TryGetInteger.</summary>
        public static string Substring(string value, double start)
            => Substring(value, (long)start);

        /// <summary>Preserves slice's historical alias to substring.</summary>
        [AuroraExport("slice", DynamicAdapter = nameof(SUBSTRING))]
        [AuroraNativeReceiver]
        public static string SliceCore(string value, int start, int end) => Substring(value, start, end);

        /// <summary>Preserves the one-index slice alias.</summary>
        [AuroraExport("slice", DynamicAdapter = nameof(SUBSTRING))]
        [AuroraNativeReceiver]
        public static string SliceCore(string value, int start) => Substring(value, start);

        /// <summary>
        /// Native implementation for String.split().
        /// Splits the string into an array of substrings based on a specified separator.
        /// </summary>
        internal static void SPLIT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not StringValue str)
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }
            if (args.TryGetString(0, out var separator))
            {
                ScriptDatum.WriteAsArray(ref result, SplitCore(str.Value, separator));
            }
            else
            {
                ScriptDatum.WriteAsArray(ref result, SplitCore(str.Value));
            }
        }

        /// <summary>Splits a raw string without materializing a StringValue receiver.</summary>
        [AuroraExport("split", DynamicAdapter = nameof(SPLIT))]
        [AuroraNativeReceiver]
        public static ScriptArray SplitCore(string value, string separator)
        {
            var segments = value.Split(separator, StringSplitOptions.None);
            var array = ScriptArray.CreateWithCapacity(segments.Length);
            for (var i = 0; i < segments.Length; i++) array.SetElement(i, ScriptDatum.FromString(segments[i]));
            return array;
        }

        /// <summary>Returns a one-element array when no separator is supplied.</summary>
        [AuroraExport("split", DynamicAdapter = nameof(SPLIT))]
        [AuroraNativeReceiver]
        public static ScriptArray SplitCore(string value)
        {
            var array = ScriptArray.CreateWithCapacity(1);
            array.SetElement(0, ScriptDatum.FromString(value));
            return array;
        }

        /// <summary>
        /// Native implementation for String.match().
        /// Searches the string for a match against a regular expression.
        /// </summary>
        internal static void MATCH(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not StringValue str)
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }

            result = MatchRegex(str.Value, ResolveRegexArgument(args, requireGlobal: false));
        }

        /// <summary>Matches a string pattern, retaining the historical result datum kind.</summary>
        [AuroraExport("match", DynamicAdapter = nameof(MATCH))]
        [AuroraNativeReceiver]
        public static ScriptDatum MatchCore(string value, string pattern) => MatchRegex(value, RegexManager.Resolve(pattern, ""));

        /// <summary>Matches a regex or weakly converted pattern without asserting an argument or result type.</summary>
        [AuroraExport("match", DynamicAdapter = nameof(MATCH))]
        [AuroraNativeReceiver]
        public static ScriptDatum MatchCore(string value, ScriptDatum pattern)
        {
            DatumBuffer1 args = default;
            args[0] = pattern;
            return MatchRegex(value, ResolveRegexArgument(args, requireGlobal: false));
        }

        /// <summary>Preserves the missing-pattern behavior.</summary>
        [AuroraExport("match", DynamicAdapter = nameof(MATCH))]
        [AuroraNativeReceiver]
        public static ScriptDatum MatchCore(string value) => MatchRegex(value, RegexManager.Resolve("undefined", ""));

        private static ScriptDatum MatchRegex(string value, ScriptRegex regex)
        {
            ScriptDatum result = default;
            // MATCH historically exposes Object even when the payload is an array
            // or NullValue. Do not strengthen this to an unconditional Array result.
            ScriptDatum.WriteAsObject(ref result, regex.HasFlag("g") ? regex.MatchOfGlobalText(value) : regex.MatchText(value));
            return result;
        }

        /// <summary>
        /// Native implementation for String.matchAll().
        /// Returns an array of all results matching a string against a regular expression,
        /// including capturing groups. Requires a global regex.
        /// </summary>
        internal static void MATCHALL(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not StringValue str)
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }
            result = MatchAllRegex(str.Value, ResolveRegexArgument(args, requireGlobal: true));
        }

        /// <summary>Matches all occurrences of a string pattern; no match remains Null.</summary>
        [AuroraExport("matchAll", DynamicAdapter = nameof(MATCHALL))]
        [AuroraNativeReceiver]
        public static ScriptDatum MatchAllCore(string value, string pattern) => MatchAllRegex(value, RegexManager.Resolve(pattern, "g"));

        /// <summary>Accepts a global regex or a weakly converted pattern, retaining Datum results.</summary>
        [AuroraExport("matchAll", DynamicAdapter = nameof(MATCHALL))]
        [AuroraNativeReceiver]
        public static ScriptDatum MatchAllCore(string value, ScriptDatum pattern)
        {
            DatumBuffer1 args = default;
            args[0] = pattern;
            return MatchAllRegex(value, ResolveRegexArgument(args, requireGlobal: true));
        }

        /// <summary>Preserves the missing-pattern behavior of matchAll.</summary>
        [AuroraExport("matchAll", DynamicAdapter = nameof(MATCHALL))]
        [AuroraNativeReceiver]
        public static ScriptDatum MatchAllCore(string value) => MatchAllRegex(value, RegexManager.Resolve("undefined", "g"));

        private static ScriptDatum MatchAllRegex(string value, ScriptRegex regex)
        {
            var array = regex.MatchAllText(value);
            return array == null ? ScriptDatum.Null : ScriptDatum.FromArray(array);
        }

        /// <summary>
        /// Native implementation for String.replace().
        /// Supports string or regex search and string or callback replacement.
        /// Returns a new string with some or all matches of a pattern replaced by a replacement.
        /// </summary>
        internal static void REPLACE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not StringValue str || args == null || args.Length < 2)
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }
            ScriptDatum.WriteAsString(ref result, ReplaceCore(ctx, str.Value, args));
        }

        /// <summary>Replaces literal strings without a dynamic receiver, argument buffer or callback closure.</summary>
        [AuroraExport("replace", DynamicAdapter = nameof(REPLACE))]
        [AuroraNativeReceiver]
        public static string ReplaceCore(string value, string search, string replacement) => value.Replace(search, replacement);

        /// <summary>Preserves regex, callback and weak-conversion semantics for uncertain argument types.</summary>
        [AuroraExport("replace", DynamicAdapter = nameof(REPLACE))]
        [AuroraNativeReceiver]
        public static string ReplaceCore(ScriptContext ctx, string value, ScriptDatum search, ScriptDatum replacement)
        {
            DatumBuffer2 args = default;
            args[0] = search;
            args[1] = replacement;
            return ReplaceCore(ctx, value, args);
        }

        private static string ReplaceCore(ScriptContext ctx, string value, Span<ScriptDatum> args)
        {
            if (args.TryGetString(0, out var search))
            {
                return args.TryGetString(1, out var replacement) ? ReplaceCore(value, search, replacement) : value;
            }
            if (args.TryGetRegex(0, out var regex))
            {
                var input = value ?? string.Empty;
                var replaceAll = regex.HasFlag("g");
                if (args.TryGetString(1, out var replacement))
                    return regex.Replace(input, replacement, replaceAll);
                if (args.TryGetFunction(1, out var callback)) return ReplaceCallback(ctx, input, regex, callback, replaceAll);
            }
            return value;
        }

        // Keep closure creation exclusively on the callback path.
        private static string ReplaceCallback(ScriptContext ctx, string input, ScriptRegex regex, ClosureFunction callback, bool replaceAll)
        {
            return regex.Replace(input, match =>
            {
                var argumentCount = match.Groups.Count + 2;
                switch (argumentCount)
                {
                    case 3:
                        {
                            DatumBuffer3 callbackArgs = default;
                            return InvokeReplaceCallback(ctx, callback, input, match, callbackArgs);
                        }
                    case 4:
                        {
                            DatumBuffer4 callbackArgs = default;
                            return InvokeReplaceCallback(ctx, callback, input, match, callbackArgs);
                        }
                    case 5:
                        {
                            DatumBuffer5 callbackArgs = default;
                            return InvokeReplaceCallback(ctx, callback, input, match, callbackArgs);
                        }
                    case 6:
                        {
                            DatumBuffer6 callbackArgs = default;
                            return InvokeReplaceCallback(ctx, callback, input, match, callbackArgs);
                        }
                    case 7:
                        {
                            DatumBuffer7 callbackArgs = default;
                            return InvokeReplaceCallback(ctx, callback, input, match, callbackArgs);
                        }
                    case 8:
                        {
                            DatumBuffer8 callbackArgs = default;
                            return InvokeReplaceCallback(ctx, callback, input, match, callbackArgs);
                        }
                    default:
                        var rentedArgs = CallOps.RentArguments(argumentCount);
                        try { return InvokeReplaceCallback(ctx, callback, input, match, rentedArgs.AsSpan(0, argumentCount)); }
                        finally { CallOps.ReturnArguments(rentedArgs, argumentCount); }
                }
            }, replaceAll);
        }

        /// <summary>Pads with the first UTF-16 code unit, preserving the existing width and empty-pad behavior.</summary>
        [AuroraExport("padLeft", DynamicAdapter = nameof(PADLEFT))]
        [AuroraNativeReceiver]
        public static string PadLeftCore(string value, int width, string padding) => value.PadLeft(width, padding[0]);

        /// <summary>Pads on the right using the existing first-code-unit rule.</summary>
        [AuroraExport("padRight", DynamicAdapter = nameof(PADRIGHT))]
        [AuroraNativeReceiver]
        public static string PadRightCore(string value, int width, string padding) => value.PadRight(width, padding[0]);

        /// <summary>
        /// Native implementation for String.padLeft().
        /// Pads the current string with another string (repeated, if needed) so that the 
        /// resulting string reaches a given length. The padding is applied from the start of the current string.
        /// </summary>
        internal static void PADLEFT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not StringValue str || args == null || args.Length < 2)
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }
            if (args.TryGetInteger(0, out var len) && args.TryGetString(1, out var pad))
            {
                ScriptDatum.WriteAsString(ref result, PadLeftCore(str.Value, (int)len, pad));
            }
            else
            {
                ScriptDatum.WriteAsString(ref result, str);
            }
        }

        /// <summary>
        /// Native implementation for String.padRight().
        /// Pads the current string with a given string (repeated, if needed) so that the 
        /// resulting string reaches a given length. The padding is applied from the end of the current string.
        /// </summary>
        internal static void PADRIGHT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not StringValue str || args == null || args.Length < 2)
            {
                ScriptDatum.WriteObject(ref result, thisObject);
                return;
            }
            if (args.TryGetInteger(0, out var len) && args.TryGetString(1, out var pad))
            {
                ScriptDatum.WriteAsString(ref result, PadRightCore(str.Value, (int)len, pad));
            }
            else
            {
                ScriptDatum.WriteAsString(ref result, str);
            }
        }

        /// <summary>
        /// Native implementation for String.charCodeAt().
        /// Returns an integer between 0 and 65535 representing the UTF-16 code unit at the given index.
        /// </summary>
        internal static void CHARCODEAT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not StringValue str)
            {
                ScriptDatum.WriteAsNumber(ref result, double.NaN);
                return;
            }

            if (!args.TryGetInteger(0, out var index))
            {
                ScriptDatum.WriteAsNumber(ref result, -1);
                return;
            }
            if (index < 0 || index >= str.Value.Length)
            {
                ScriptDatum.WriteAsNumber(ref result, double.NaN);
                return;
            }
            ScriptDatum.WriteAsNumber(ref result, str.Value[(int)index]);
        }

        private static string InvokeReplaceCallback(ScriptContext ctx, ClosureFunction callback, string originalValue, Match match, Span<ScriptDatum> parameters)
        {
            var groupCount = match.Groups.Count;
            ScriptDatum.WriteAsString(ref parameters[0], match.Value);
            for (var i = 1; i < groupCount; i++)
            {
                if (match.Groups[i].Success)
                {
                    ScriptDatum.WriteAsString(ref parameters[i], match.Groups[i].Value);
                }
                else ScriptDatum.MarkAsNull(ref parameters[i]);
            }
            ScriptDatum.WriteAsNumber(ref parameters[groupCount], match.Index);
            ScriptDatum.WriteAsString(ref parameters[groupCount + 1], originalValue);

            return ScriptDatum.ToString(callback.Invoke(ctx, parameters));
        }

        private static ScriptRegex ResolveRegexArgument(Span<ScriptDatum> args, bool requireGlobal)
        {
            ScriptRegex regex = null;
            if (args != null && args.Length > 0)
            {
                var candidate = args[0];
                if (candidate.Kind == ValueKind.Regex && candidate.Object is ScriptRegex scriptRegex)
                {
                    if (requireGlobal && !scriptRegex.HasFlag("g"))
                    {
                        throw new AuroraRuntimeException("String.matchAll requires a global regular expression");
                    }
                    regex = scriptRegex;
                }
                else
                {
                    var pattern = CoercePatternFromDatum(candidate);
                    var flags = requireGlobal ? "g" : "";
                    regex = RegexManager.Resolve(pattern, flags);
                }
            }
            else
            {
                regex = RegexManager.Resolve("undefined", requireGlobal ? "g" : "");
            }

            return regex;
        }

        private static string CoercePatternFromDatum(ScriptDatum datum)
        {
            switch (datum.Kind)
            {
                case ValueKind.String:
                    return datum.StringText;
                case ValueKind.Number:
                    return datum.Number.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case ValueKind.Int64:
                    return datum.Int64.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case ValueKind.UInt64:
                    return datum.UInt64.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case ValueKind.Boolean:
                    return datum.Boolean ? "true" : "false";
                default:
                    return string.Empty;
            }
        }
    }
}
