namespace AuroraScript.Core
{
    /// <summary>
    /// Represents a script source backed by an in-memory string.
    /// </summary>
    public sealed class MemoryScriptSource : ScriptSource
    {
        /// <summary>
        /// Initializes a new in-memory script source.
        /// </summary>
        /// <param name="baseDirectory">The source root used to compute relative module paths.</param>
        /// <param name="fullPath">The absolute file path or virtual source identifier.</param>
        /// <param name="text">The script text.</param>
        public MemoryScriptSource(string baseDirectory, string fullPath, string text)
        {
            BaseDirectory = ScriptPath.NormalizeBaseDirectory(baseDirectory);
            FullPath = ScriptPath.GetFullPath(BaseDirectory, fullPath);
            SourcePath = ScriptPath.GetModulePath(BaseDirectory, FullPath);
            Code = text ?? string.Empty;
        }

        /// <inheritdoc />
        public string BaseDirectory { get; }

        /// <inheritdoc />
        public string SourcePath { get; }

        /// <inheritdoc />
        public string FullPath { get; }

        /// <inheritdoc />
        public string Code { get; }

        /// <inheritdoc />
        public string ReadSource()
        {
            return Code;
        }
    }
}
