using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Linq;

namespace AuroraScript
{
    /// <summary>
    /// Represents an error that occurs during script execution in the Virtual Machine.
    /// Includes a script-level stack trace for debugging.
    /// </summary>
    public class AuroraRuntimeException : AuroraException
    {
        /// <summary> Gets the name of the module where the runtime error occurred. </summary>
        public readonly string ModuleName;

        /// <summary> Gets the line number where the runtime error occurred. </summary>
        public readonly int LineNumber;

        /// <summary> Gets the script stack trace at the time of the exception. </summary>
        public new IReadOnlyList<AuroraStackTrace> StackTrace;

        internal readonly ScriptError internalError;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuroraRuntimeException"/> class with an inner exception and a stack trace.
        /// </summary>
        /// <param name="ex">The inner exception that was caught during runtime.</param>
        /// <param name="stackTrace">The script stack trace.</param>
        public AuroraRuntimeException(Exception ex, IReadOnlyList<AuroraStackTrace> stackTrace) : base(ex.Message, ex)
        {
            StackTrace = stackTrace;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuroraRuntimeException"/> class with a specified message.
        /// </summary>
        /// <param name="message">The error message.</param>
        public AuroraRuntimeException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuroraRuntimeException"/> class with a ScriptError.
        /// </summary>
        /// <param name="error">The error.</param>
        public AuroraRuntimeException(ScriptError error) : base(error.Message)
        {
            internalError = error;
        }

        /// <summary>
        /// Returns a formatted string that represents the runtime exception and its script stack trace.
        /// </summary>
        /// <returns>A string representation of the exception.</returns>
        public override string ToString()
        {
            return Message + Environment.NewLine + string.Join(Environment.NewLine, StackTrace?.Select(e => e.ToString()) ?? Enumerable.Empty<string>());
        }
    }
}
