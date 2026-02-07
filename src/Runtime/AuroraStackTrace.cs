using System;
using System.IO;

namespace AuroraScript.Runtime
{
    /// <summary>
    /// Represents a single frame in an AuroraScript stack trace.
    /// Provides information about the source file, method name, and line number where an error occurred.
    /// </summary>
    public class AuroraStackTrace
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AuroraStackTrace"/> class with detailed location information.
        /// </summary>
        /// <param name="path">The full path to the source file.</param>
        /// <param name="method">The name of the method where the frame is located.</param>
        /// <param name="line">The line number within the source file.</param>
        public AuroraStackTrace(String path, string method, int line)
        {
            Method = method;
            FullPath = path;
            Line = line;
            if (!String.IsNullOrEmpty(path))
            {
                FileName = Path.GetFileName(path);
            }
        }

        /// <summary> The name of the source file (excluding the directory path). </summary>
        public readonly String FileName;

        /// <summary> The full absolute path to the source file. </summary>
        public readonly String FullPath;

        /// <summary> The name of the script method or function. </summary>
        public readonly String Method;

        /// <summary> The line number in the script source. </summary>
        public readonly int Line;


        /// <summary>
        /// Returns a string representation of the stack trace frame.
        /// </summary>
        /// <returns>A formatted string including the path, method, and line number.</returns>
        override public String ToString()
        {
            return $" at {FullPath} {Method}() line: {Line}";
        }


    }
}
