using System;

namespace AuroraScript
{
    /// <summary>
    /// Represents an error that occurs during the compilation of a script module.
    /// </summary>
    public class AuroraCompileException : AuroraException
    {
        /// <summary> Gets the path of the module that failed to compile. </summary>
        public readonly string ModulePath;

        /// <summary>
        /// Initializes a new instance of the <see cref="AuroraCompileException"/> class.
        /// </summary>
        /// <param name="modulePath">The path of the module.</param>
        /// <param name="inner">The inner exception that caused the compilation failure.</param>
        public AuroraCompileException(string modulePath, Exception inner) : base($"Compile failed: {modulePath}", inner)
        {
            ModulePath = modulePath;
        }
    }
}
