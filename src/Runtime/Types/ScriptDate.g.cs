using System;
using AuroraScript.Hosting;

namespace AuroraScript.Runtime.Types
{
    public partial class ScriptDate
    {
        /// <summary>Formats a script date using the current engine's format when no format is supplied.</summary>
        [Export("toString", DynamicAdapter = nameof(TOSTRING))]
        public string ToStringCore(ScriptContext ctx, string format = null) => FormatCore(format ?? ctx.Engine.Options.Runtime.DateTimeFormat);

        /// <summary>Formats a date using the supplied CLR format string.</summary>
        public string FormatCore(string format = null) => DateTime.ToString(format);

        /// <summary>Reads dayOfWeek using the script Number representation.</summary>
        [Export("dayOfWeek", IsGetter = true, DynamicAdapter = nameof(DAYOFWEEK))]
        public int DayOfWeekCore() => (int)DayOfWeek;




        /// <summary> Native implementation for the 'year' property. </summary>
        internal static void YEAR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Year);
            }
        }

        /// <summary> Native implementation for the 'month' property. </summary>
        internal static void MONTH(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Month);
            }
        }

        /// <summary> Native implementation for the 'day' property. </summary>
        internal static void DAY(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Day);
            }
        }
        /// <summary> Native implementation for the 'hour' property. </summary>
        internal static void HOUR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Hour);
            }
        }

        /// <summary> Native implementation for the 'minute' property. </summary>
        internal static void MINUTE(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Minute);
            }
        }

        /// <summary> Native implementation for the 'second' property. </summary>
        internal static void SECOND(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Second);
            }
        }

        /// <summary> Native implementation for the 'millisecond' property. </summary>
        internal static void MILLISECCOND(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Millisecond);
            }
        }

        /// <summary> Native implementation for the 'dayOfWeek' property. </summary>
        internal static void DAYOFWEEK(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, (int)date.DayOfWeek);
            }
        }
        /// <summary> Native implementation for the 'dayOfYear' property. </summary>
        internal static void DAYOFYEAR(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.DayOfYear);
            }
        }

        /// <summary> Native implementation for the 'ticks' property. </summary>
        internal static void TICKS(ScriptContext ctx, ScriptObject thisObject, Span<ScriptDatum> args, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Ticks);
            }
        }




    }
}
