using System;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a date and time object in AuroraScript.
    /// Wraps the CLI <see cref="DateTimeOffset"/> to provide time-related functionality.
    /// </summary>
    public sealed partial class ScriptDate : ScriptObject
    {
        /// <summary> Gets the underlying <see cref="DateTimeOffset"/> value. </summary>
        public DateTimeOffset DateTime { get; private set; }

        private ScriptDate() : base(Prototypes.DatePrototype)
        {
            EnableValueEquality();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptDate"/> class from a <see cref="DateTime"/>.
        /// </summary>
        /// <param name="date">The source date.</param>
        public ScriptDate(DateTime date) : this()
        {
            this.DateTime = date;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptDate"/> class from a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <param name="dateTimeOffset">The source date time offset.</param>
        public ScriptDate(DateTimeOffset dateTimeOffset) : this()
        {
            this.DateTime = dateTimeOffset;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptDate"/> class from ticks.
        /// </summary>
        /// <param name="ticks">The number of ticks representing the date.</param>
        public ScriptDate(long ticks) : this()
        {
            this.DateTime = new DateTime(ticks);
        }

        /// <summary> Gets the year component of the date. </summary>
        public int Year => DateTime.Year;
        /// <summary> Gets the month component (1-12) of the date. </summary>
        public int Month => DateTime.Month;
        /// <summary> Gets the day component of the date. </summary>
        public int Day => DateTime.Day;
        /// <summary> Gets the hour component of the date. </summary>
        public int Hour => DateTime.Hour;
        /// <summary> Gets the minute component of the date. </summary>
        public int Minute => DateTime.Minute;
        /// <summary> Gets the second component of the date. </summary>
        public int Second => DateTime.Second;
        /// <summary> Gets the millisecond component of the date. </summary>
        public int Millisecond => DateTime.Millisecond;
        /// <summary> Gets the day of the week. </summary>
        public DayOfWeek DayOfWeek => DateTime.DayOfWeek;
        /// <summary> Gets the day of the year. </summary>
        public int DayOfYear => DateTime.DayOfYear;
        /// <summary> Gets the number of ticks representing the date. </summary>
        public long Ticks => DateTime.Ticks;



        internal override bool ValueEquals(ScriptObject other)
        {
            return other is ScriptDate date && DateTime.Equals(date.DateTime);
        }






        /// <summary>
        /// Formats the date using the specified format string.
        /// </summary>
        /// <param name="format">The format string (e.g., "yyyy-MM-dd"). Defaults to null.</param>
        /// <returns>A formatted date string.</returns>
        public string Format(string format = null)
        {
            return DateTime.ToString(format);
        }
    }
}
