using AuroraScript.Hosting;
using System;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a date and time object in AuroraScript.
    /// Wraps the CLI <see cref="DateTimeOffset"/> to provide time-related functionality.
    /// </summary>
    [NativeType("Date")]
    public sealed partial class ScriptDate : ScriptObject
    {
        /// <summary> Gets the underlying <see cref="DateTimeOffset"/> value. </summary>
        public DateTimeOffset DateTime { get; private set; }

        /// <inheritdoc />
        protected internal override ScriptDatum TypeOfValue => TypeNames.Date;

        private ScriptDate() : base(NativePrototype)
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
        /// Initializes a new instance of the <see cref="ScriptDate"/> class from UTC ticks.
        /// </summary>
        /// <param name="ticks">The number of ticks since 0001-01-01T00:00:00+00:00.</param>
        [Export(DynamicAdapter = nameof(PARSE))]
        public ScriptDate(long ticks) : this()
        {
            this.DateTime = new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        /// <summary> Gets the year component of the date. </summary>
        public int Year
        {
            [Export("year", IsGetter = true, DynamicAdapter = nameof(YEAR))]
            get => DateTime.Year;
        }
        /// <summary> Gets the month component (1-12) of the date. </summary>
        public int Month
        {
            [Export("month", IsGetter = true, DynamicAdapter = nameof(MONTH))]
            get => DateTime.Month;
        }
        /// <summary> Gets the day component of the date. </summary>
        public int Day
        {
            [Export("day", IsGetter = true, DynamicAdapter = nameof(DAY))]
            get => DateTime.Day;
        }
        /// <summary> Gets the hour component of the date. </summary>
        public int Hour
        {
            [Export("hour", IsGetter = true, DynamicAdapter = nameof(HOUR))]
            get => DateTime.Hour;
        }
        /// <summary> Gets the minute component of the date. </summary>
        public int Minute
        {
            [Export("minute", IsGetter = true, DynamicAdapter = nameof(MINUTE))]
            get => DateTime.Minute;
        }
        /// <summary> Gets the second component of the date. </summary>
        public int Second
        {
            [Export("second", IsGetter = true, DynamicAdapter = nameof(SECOND))]
            get => DateTime.Second;
        }
        /// <summary> Gets the millisecond component of the date. </summary>
        public int Millisecond
        {
            [Export("millisecond", IsGetter = true, DynamicAdapter = nameof(MILLISECCOND))]
            get => DateTime.Millisecond;
        }
        /// <summary> Gets the day of the week. </summary>
        public DayOfWeek DayOfWeek => DateTime.DayOfWeek;
        /// <summary> Gets the day of the year. </summary>
        public int DayOfYear
        {
            [Export("dayOfYear", IsGetter = true, DynamicAdapter = nameof(DAYOFYEAR))]
            get => DateTime.DayOfYear;
        }

        /// <summary> Gets the number of ticks representing the date. </summary>
        public long Ticks
        {
            [Export("ticks", IsGetter = true, DynamicAdapter = nameof(TICKS))]
            get => DateTime.Ticks;
        }



        internal override bool ValueEquals(ScriptObject other)
        {
            return other is ScriptDate date && DateTime.Equals(date.DateTime);
        }
    }
}
