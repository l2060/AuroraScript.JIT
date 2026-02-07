using System;

namespace AuroraScript.Runtime.Types
{
    /// <summary>
    /// Represents a date and time object in AuroraScript.
    /// Wraps the CLI <see cref="DateTimeOffset"/> to provide time-related functionality.
    /// </summary>
    public sealed class ScriptDate : ScriptObject
    {
        /// <summary> Gets the underlying <see cref="DateTimeOffset"/> value. </summary>
        public DateTimeOffset DateTime { get; private set; }

        private ScriptDate()
        {
            _prototype = Prototypes.DatePrototype;
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
