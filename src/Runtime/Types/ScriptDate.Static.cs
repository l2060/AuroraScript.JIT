using System;
using System.Globalization;
using System.Runtime.InteropServices;
using AuroraScript.Hosting;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents the native 'Date' constructor function in AuroraScript.
    /// Provides methods for retrieving the current time and parsing date strings.
    /// </summary>
    public sealed partial class ScriptDate
    {
        /// <summary>Creates a date containing the current local time.</summary>
        [Export("now", DynamicAdapter = nameof(NOW))]
        public static ScriptDate NowCore() => new ScriptDate(System.DateTime.Now);

        /// <summary>Creates a date containing the current UTC time.</summary>
        [Export("utcNow", DynamicAdapter = nameof(UTC_NOW))]
        public static ScriptDate UtcNowCore() => new ScriptDate(System.DateTime.UtcNow);

        /// <summary>Parses ticks or text with the existing date conversion rules.</summary>
        [Export("parse", DynamicAdapter = nameof(PARSE))]
        public static ScriptDatum ParseCore(ScriptContext ctx, ScriptDatum value)
        {
            var result = default(ScriptDatum);
            PARSE(ctx, null, MemoryMarshal.CreateSpan(ref value, 1), ref result);
            return result;
        }

        /// <summary> Supported date formats for parsing strings. </summary>
        private static string[] formats =
          {
                "yyyy-MM-dd",
                "yyyy/MM/dd",
                "yyyyMMdd",
                "yyyy-MM-dd HH:mm:ss",
                "yyyy/MM/dd HH:mm:ss",
                "yyyyMMddHHmmss",
                "yyyy-MM-ddTHH:mm:ss",
                "yyyy-MM-ddTHH:mm:ss.fff",
                "yyyy-MM-dd HH:mm:ss.fff",
                "yyyy-MM-dd HH:mm:ss fff",
                "yyyy/MM/dd HH:mm:ss.fff",
                "yyyy/MM/dd HH:mm:ss fff",
                "MM/dd/yyyy",
                "MM-dd-yyyy",
                "dd/MM/yyyy",
                "dd-MM-yyyy"
            };

        private static bool TryParseDate(ScriptContext ctx, string text, out DateTimeOffset value)
        {
            var configuredFormat = ctx?.Engine?.Options?.Runtime?.DateTimeFormat;
            if (!string.IsNullOrEmpty(configuredFormat))
            {
                try
                {
                    if (DateTimeOffset.TryParseExact(
                            text,
                            configuredFormat,
                            CultureInfo.InvariantCulture,
                            DateTimeStyles.None,
                            out value))
                    {
                        return true;
                    }
                }
                catch (Exception exception) when (exception is FormatException or ArgumentException)
                {
                    // Keep the legacy constructor formats available even if a
                    // host supplies an invalid custom format. TDoc's host API
                    // performs strict format validation when it reads/writes.
                }
            }

            if (System.DateTime.TryParseExact(
                text,
                formats,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var legacyValue))
            {
                value = new DateTimeOffset(legacyValue);
                return true;
            }

            value = default;
            return false;
        }

        /// <summary> Native implementation for Date.now(). Returns the current local time. </summary>
        internal static void NOW(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsDate(ref result, new ScriptDate(System.DateTime.Now));
        }

        /// <summary> Native implementation for Date.utcNow(). Returns the current UTC time. </summary>
        internal static void UTC_NOW(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsDate(ref result, new ScriptDate(System.DateTime.UtcNow));
        }

        /// <summary> Native implementation for Date.toString(). Supports an optional format string. </summary>
        internal new static void TOSTRING(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                if (!args.TryGetString(0, out var value))
                {
                    value = ctx.Engine.Options.Runtime.DateTimeFormat;
                }
                ScriptDatum.WriteAsString(ref result, date.Format(value));
            }
        }

        /// <summary> Internal helper to parse arguments into a Date object. Supports ticks or formatted strings. </summary>
        internal static void PARSE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (args.TryGetInteger(0, out var value)) // ticks
            {
                ScriptDatum.WriteAsDate(ref result, new ScriptDate(value));
            }
            else if (args.TryGetString(0, out var strValue)) // formatted string
            {
                if (TryParseDate(ctx, strValue, out var dateTime))
                {
                    ScriptDatum.WriteAsDate(ref result, new ScriptDate(dateTime));
                }
            }
        }
    }
}
