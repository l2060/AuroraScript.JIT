using System;
using System.Collections.Generic;
using System.Text;
using AuroraScript.Hosting;

namespace AuroraScript.Runtime.Types
{
    public partial class ScriptDate
    {
        /// <summary>Formats a date with the script string conversion for an explicit null.</summary>
        [Export("toString", DynamicAdapter = nameof(TOSTRING))]
        public string FormatCore(string format) => Format(format ?? "null");

        /// <summary>Reads year using the script Number representation.</summary>
        [Export("year", IsGetter = true, DynamicAdapter = nameof(YEAR))]
        public int YearCore() => Year;

        /// <summary>Reads month using the script Number representation.</summary>
        [Export("month", IsGetter = true, DynamicAdapter = nameof(MONTH))]
        public int MonthCore() => Month;

        /// <summary>Reads day using the script Number representation.</summary>
        [Export("day", IsGetter = true, DynamicAdapter = nameof(DAY))]
        public int DayCore() => Day;

        /// <summary>Reads hour using the script Number representation.</summary>
        [Export("hour", IsGetter = true, DynamicAdapter = nameof(HOUR))]
        public int HourCore() => Hour;

        /// <summary>Reads minute using the script Number representation.</summary>
        [Export("minute", IsGetter = true, DynamicAdapter = nameof(MINUTE))]
        public int MinuteCore() => Minute;

        /// <summary>Reads second using the script Number representation.</summary>
        [Export("second", IsGetter = true, DynamicAdapter = nameof(SECOND))]
        public int SecondCore() => Second;

        /// <summary>Reads millisecond using the script Number representation.</summary>
        [Export("millisecond", IsGetter = true, DynamicAdapter = nameof(MILLISECCOND))]
        public int MillisecondCore() => Millisecond;

        /// <summary>Reads dayOfWeek using the script Number representation.</summary>
        [Export("dayOfWeek", IsGetter = true, DynamicAdapter = nameof(DAYOFWEEK))]
        public int DayOfWeekCore() => (int) DayOfWeek;

        /// <summary>Reads dayOfYear using the script Number representation.</summary>
        [Export("dayOfYear", IsGetter = true, DynamicAdapter = nameof(DAYOFYEAR))]
        public int DayOfYearCore() => DayOfYear;

        /// <summary>Reads ticks using the script Number representation.</summary>
        [Export("ticks", IsGetter = true, DynamicAdapter = nameof(TICKS))]
        public double TicksCore() => Ticks;



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
