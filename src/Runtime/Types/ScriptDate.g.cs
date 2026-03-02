using System;
using System.Collections.Generic;
using System.Text;

namespace AuroraScript.Runtime.Types
{
    public partial class ScriptDate
    {


        /// <summary> Native implementation for the 'year' property. </summary>
        internal static void YEAR(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Year);
            }
        }

        /// <summary> Native implementation for the 'month' property. </summary>
        internal static void MONTH(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Month);
            }
        }

        /// <summary> Native implementation for the 'day' property. </summary>
        internal static void DAY(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Day);
            }
        }
        /// <summary> Native implementation for the 'hour' property. </summary>
        internal static void HOUR(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Hour);
            }
        }

        /// <summary> Native implementation for the 'minute' property. </summary>
        internal static void MINUTE(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Minute);
            }
        }

        /// <summary> Native implementation for the 'second' property. </summary>
        internal static void SECOND(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Second);
            }
        }

        /// <summary> Native implementation for the 'millisecond' property. </summary>
        internal static void MILLISECCOND(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Millisecond);
            }
        }

        /// <summary> Native implementation for the 'dayOfWeek' property. </summary>
        internal static void DAYOFWEEK(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, (int)date.DayOfWeek);
            }
        }
        /// <summary> Native implementation for the 'dayOfYear' property. </summary>
        internal static void DAYOFYEAR(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.DayOfYear);
            }
        }

        /// <summary> Native implementation for the 'ticks' property. </summary>
        internal static void TICKS(ScriptObject thisObject, ref ScriptDatum result)
        {
            if (thisObject is ScriptDate date)
            {
                ScriptDatum.WriteAsNumber(ref result, date.Ticks);
            }
        }




    }
}
