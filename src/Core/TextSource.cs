using System.IO;

namespace AuroraScript.Core
{
    /// <summary>
    /// Represents a script source provided as a raw string in memory.
    /// Implements <see cref="ScriptSource"/> for non-file based script content.
    /// </summary>
    internal class TextSource : ScriptSource
    {
        /// <summary> Gets the base directory used for relative path resolution. </summary>
        public string BaseDirectory { get; private set; }

        /// <summary> Gets the relative source path assigned to this text source. </summary>
        public string SourcePath { get; private set; }

        /// <summary> Gets the absolute full path or virtual identifier for this text source. </summary>
        public string FullPath { get; private set; }

        /// <summary> Gets the raw script code. </summary>
        public string Code { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TextSource"/> class.
        /// </summary>
        /// <param name="basePath">The base directory path.</param>
        /// <param name="absPath">The absolute path or identifier for the source.</param>
        /// <param name="text">The raw script content.</param>
        public TextSource(string basePath, string absPath, string text)
        {
            BaseDirectory = basePath;
            FullPath = absPath;
            SourcePath = Path.GetRelativePath(basePath, absPath);
            Code = text;
        }

        /// <summary>
        /// Returns the script source code from memory.
        /// </summary>
        /// <returns>The script content as a string.</returns>
        public string ReadSource()
        {
            return Code;
        }
    }
}
