using System;
using System.Globalization;

namespace AuroraScript.Runtime.Types.TypeConstruct
{
    /// <summary>
    /// Represents the native 'Date' constructor function in AuroraScript.
    /// Provides methods for retrieving the current time and parsing date strings.
    /// </summary>
    internal class ScriptDateConstructor : ScriptType
    {
        /// <summary> The global singleton instance of the Date constructor. </summary>
        internal static ScriptDateConstructor INSTANCE = new ScriptDateConstructor();

        internal ScriptDateConstructor() : base("Date", true)
        {
            Define("now", new BondingFunction(NOW), writeable: false, enumerable: false);
            Define("utcNow", new BondingFunction(UTC_NOW), writeable: false, enumerable: false);
            Define("parse", new BondingFunction(PARSE), writeable: false, enumerable: false);
            Frozen();
        }



        public override void Construct(ScriptContext ctx, ScriptDatum[] args, ref ScriptDatum result)
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
