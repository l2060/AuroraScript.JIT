using System;

namespace AuroraScript
{
    /// <summary>
    /// The base exception class for AuroraScript, representing errors that occur during script engine operations.
    /// </summary>
    public class AuroraException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuroraException"/> class.
        /// </summary>
        public AuroraException()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuroraException"/> class with a specified error message.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public AuroraException(string message) : base(message)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="AuroraException"/> class with a specified error message 
        /// and a reference to the inner exception that is the cause of this exception.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public AuroraException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
