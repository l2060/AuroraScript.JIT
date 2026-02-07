using System;
using System.Collections.Generic;
using System.Linq;

namespace AuroraScript
{
    /// <summary>
    /// Represents an exception that aggregates multiple compilation errors.
    /// </summary>
    public class AuroraCompileReportException : AuroraException
    {
        /// <summary> Gets the collection of individual compilation errors. </summary>
        public readonly IReadOnlyList<Exception> Errors;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuroraCompileReportException"/> class.
        /// </summary>
        /// <param name="errors">An enumerable of exceptions representing compilation errors.</param>
        public AuroraCompileReportException(IEnumerable<Exception> errors) : base($"Compilation failed with {errors.Count()} errors")
        {
            Errors = errors.ToArray();
        }
    }
}
