using AuroraScript.Core;
using AuroraScript.Runtime.Pool;
using AuroraScript.Runtime.Types;
using Microsoft.VisualBasic;
using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Partial implementation of <see cref="StringValue"/> providing constants and native method implementations.
    /// This fragment exposes common string operations to the AuroraScript runtime.
    /// </summary>
    public partial class StringValue
    {
        /// <summary> An empty string value. </summary>
        public readonly static StringValue Empty = new StringValue("");
        /// <summary> A string value representing "null". </summary>
        public readonly static StringValue NULL = new StringValue("null");
        /// <summary> A string value representing "true". </summary>
        public readonly static StringValue TRUE = new StringValue("true");
        /// <summary> A string value representing "false". </summary>
        public readonly static StringValue FALSE = new StringValue("false");
        /// <summary> A string value used as a generic object label. </summary>
        public readonly static StringValue OBJECT = new StringValue("[object]");

        /// <summary>
        /// Native implementation for String.toLowerCase().
        /// Returns a new string with all characters converted to lowercase.
        /// </summary>
        internal static void TOLOWERCASE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringValue strValue)
            {
                ScriptDatum.WriteAsString(ref result, strValue.Value.ToLower(CultureInfo.CurrentCulture));
            }
            else
            {
                ScriptDatum.MarkAsNull(ref result);
            }
        }

        /// <summary>
        /// Native implementation for String.toUpperCase().
        /// Returns a new string with all characters converted to uppercase.
        /// </summary>
        internal static void TOUPPERCASE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringValue strValue)
            {
                ScriptDatum.WriteAsString(ref result, strValue.Value.ToUpper(CultureInfo.CurrentCulture));
            }
            else
            {
                ScriptDatum.MarkAsNull(ref result);
            }
        }

        /// <summary>
        /// Native implementation for String.toString().
        /// Returns the string itself.
        /// </summary>
        internal new static void TOSTRING(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringValue strValue)
            {
                ScriptDatum.WriteAsString(ref result, strValue);
            }
            else
            {
                ScriptDatum.WriteAsString(ref result, thisObject.ToString());
            }
        }

        /// <summary>
        /// Native implementation for reading String.length.
        /// Returns the number of characters in the string.
        /// </summary>
        internal new static void LENGTH(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is StringValue strValue)
            {
                ScriptDatum.WriteAsNumber(ref result, strValue.Value.Length);
            }
            else
            {
                ScriptDatum.MarkAsNull(ref result);
            }
        }

        /// <summary>
        /// Native implementation for String.contains().
        /// Returns true if the search string is found within this string.
        /// </summary>
        internal static void CONTANINS(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            var str = thisObject as StringValue;
            if (str != null && args.TryGetString(0, out var search))
            {
                ScriptDatum.WriteAsBoolean(ref result, str.Value.Contains(search));
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
                ScriptDatum.WriteAsNumber(ref result, str.Value.IndexOf(search, StringComparison.Ordinal));
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
                ScriptDatum.WriteAsNumber(ref result, str.Value.LastIndexOf(search, StringComparison.Ordinal));
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
                ScriptDatum.WriteAsBoolean(ref result, str.Value.StartsWith(search, StringComparison.Ordinal));
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
                ScriptDatum.WriteAsBoolean(ref result, str.Value.EndsWith(search, StringComparison.Ordinal));
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
                if (start > end)
                {
                    (start, end) = (end, start);
                }
                var length = (int)Math.Clamp(end - start, 0, Math.Max(0, str.Value.Length - start));
                ScriptDatum.WriteAsString(ref result, str.Value.Substring(Math.Clamp((int)start, 0, str.Value.Length), length));
                return;
            }
            var safeStart = (int)Math.Clamp(start, 0, Math.Max(0, str.Value.Length));
            ScriptDatum.WriteAsString(ref result, str.Value.Substring(safeStart));
        }

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
                var segments = str.Value.Split(new[] { separator }, StringSplitOptions.None).Select(StringValue.Of).Cast<ScriptObject>().ToArray();
                ScriptDatum.WriteAsArray(ref result, new ScriptArray(segments));
            }
            else
            {
                ScriptDatum.WriteAsArray(ref result, new ScriptArray(new ScriptObject[] { thisObject }));
            }
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

            var regex = ResolveRegexArgument(args, requireGlobal: false);
            if (regex == null)
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }
            ScriptDatum.WriteAsObject(ref result, regex.HasFlag("g") ? regex.MatchOfGlobal(str) : regex.Match(str));
        }

        /// <summary>
        /// Native implementation for String.matchAll().
        /// Returns an iterator of all results matching a string against a regular expression, 
        /// including capturing groups. Requires a global regex.
        /// </summary>
        internal static void MATCHALL(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is not StringValue str)
            {
                ScriptDatum.MarkAsNull(ref result);
                return;
            }
            var regex = ResolveRegexArgument(args, requireGlobal: true);

            var array = regex.MatchAll(str);
            if (array != null)
            {
                ScriptDatum.WriteAsArray(ref result, array);
            }
            else
            {
                ScriptDatum.MarkAsNull(ref result);
            }
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
            var target = str.Value;
            if (args.TryGetString(0, out var search))
            {
                if (!args.TryGetString(1, out var replace))
                {
                    ScriptDatum.WriteAsString(ref result, str);
                    return;
                }
                target = target.Replace(search, replace);
            }
            else if (args.TryGetRegex(0, out var regex))
            {
                var input = target ?? string.Empty;
                var replaceAll = regex.HasFlag("g");
                if (args.TryGetString(1, out var replacement))
                {
                    target = regex.Replace(input, replacement, replaceAll);
                }
                else if (args.TryGetFunction(1, out var callback))
                {
                    var originalValue = StringValue.Of(input);

                    target = regex.Replace(input, match =>
                    {
                        var groupCount = match.Groups.Count;
                        var callbackArgs = new ScriptDatum[groupCount + 2];
                        ScriptDatum.WriteAsString(ref callbackArgs[0], match.Value);
                        for (var i = 1; i < groupCount; i++)
                        {
                            if (match.Groups[i].Success)
                            {
                                ScriptDatum.WriteAsString(ref callbackArgs[i], match.Groups[i].Value);
                            }
                        }
                        ScriptDatum.WriteAsNumber(ref callbackArgs[groupCount], match.Index);
                        ScriptDatum.WriteAsString(ref callbackArgs[groupCount + 1], originalValue);

                        return InvokeReplaceCallback(ctx, callback, callbackArgs);
                    }, replaceAll);
                }
                else
                {
                    ScriptDatum.WriteAsString(ref result, str);
                    return;
                }
            }
            else
            {
                ScriptDatum.WriteAsString(ref result, str);
                return;
            }
            ScriptDatum.WriteAsString(ref result, target);
        }

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
                ScriptDatum.WriteAsString(ref result, str.Value.PadLeft((int)len, pad[0]));
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
                ScriptDatum.WriteAsString(ref result, str.Value.PadRight((int)len, pad[0]));
            }
            else
            {
                ScriptDatum.WriteAsString(ref result, str);
            }
        }

        /// <summary>
        /// Native implementation for String.trim().
        /// Removes whitespace from both ends of the string.
        /// </summary>
        internal static void TRIM(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringValue str)
            {
                ScriptDatum.WriteAsString(ref result, str.Value.Trim());
            }
            else
            {
                ScriptDatum.MarkAsNull(ref result);
            }
        }

        /// <summary>
        /// Native implementation for String.trimStart() / trimLeft().
        /// Removes whitespace from the beginning of the string.
        /// </summary>
        internal static void TRIMLEFT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringValue str)
            {
                ScriptDatum.WriteAsString(ref result, str.Value.TrimStart());
            }
            else
            {
                ScriptDatum.MarkAsNull(ref result);
            }
        }

        /// <summary>
        /// Native implementation for String.trimEnd() / trimRight().
        /// Removes whitespace from the end of the string.
        /// </summary>
        internal static void TRIMRIGHT(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is StringValue str)
            {
                ScriptDatum.WriteAsString(ref result, str.Value.TrimEnd());
            }
            else
            {
                ScriptDatum.MarkAsNull(ref result);
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

        private static string InvokeReplaceCallback(ScriptContext ctx, ClosureFunction callback, Span<ScriptDatum> parameters)
        {
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
                case ValueKind.String when datum.String != null:
                    return datum.String.Value;
                case ValueKind.Number:
                    return datum.Number.ToString(System.Globalization.CultureInfo.InvariantCulture);
                case ValueKind.Boolean:
                    return datum.Boolean ? "true" : "false";
                default:
                    return string.Empty;
            }
        }
    }
}
