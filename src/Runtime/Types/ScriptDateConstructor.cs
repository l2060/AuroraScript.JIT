using System;
using System.Globalization;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents the native 'Date' constructor function in AuroraScript.
    /// Provides methods for retrieving the current time and parsing date strings.
    /// </summary>
    internal class ScriptDateConstructor : BondingFunction
    {
        /// <summary> The global singleton instance of the Date constructor. </summary>
        internal static ScriptDateConstructor INSTANCE = new ScriptDateConstructor();

        internal ScriptDateConstructor() : base(CONSTRUCTOR)
        {
            _prototype = Prototypes.DateConstructorPrototype;
        }

        /// <summary> Native implementation for the Date constructor (Date() or new Date()). </summary>
        internal static void CONSTRUCTOR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            PARSE(ctx, thisObject, args, ref result);
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
                "MM/dd/yyyy",
                "MM-dd-yyyy",
                "dd/MM/yyyy",
                "dd-MM-yyyy"
            };

        /// <summary> Native implementation for Date.now(). Returns the current local time. </summary>
        internal static void NOW(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsDate(ref result, new ScriptDate(DateTime.Now));
        }

        /// <summary> Native implementation for Date.utcNow(). Returns the current UTC time. </summary>
        internal static void UTC_NOW(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            ScriptDatum.WriteAsDate(ref result, new ScriptDate(DateTime.UtcNow));
        }

        /// <summary> Native implementation for Date.toString(). Supports an optional format string. </summary>
        internal new static void TOSTRING(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                if (!args.TryGetString(0, out var value))
                {
                    value = ctx.Engine.Options.DateTimeFormat;
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
                if (DateTime.TryParseExact(strValue, formats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dt))
                {
                    ScriptDatum.WriteAsDate(ref result, new ScriptDate(dt));
                }
            }
        }
    }
}
