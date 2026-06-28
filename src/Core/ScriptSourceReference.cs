namespace AuroraScript.Core
{
    /// <summary>
    /// Identifies a resolved script source in a script source resolver.
    /// </summary>
    public readonly struct ScriptSourceReference
    {
        /// <summary>
        /// Initializes a new script source reference.
        /// </summary>
        /// <param name="baseDirectory">The source root used to compute module-relative paths.</param>
        /// <param name="fullPath">The absolute file path or virtual source identifier.</param>
        public ScriptSourceReference(string baseDirectory, string fullPath)
            : this(baseDirectory, fullPath, null)
        {
        }

        /// <summary>
        /// Initializes a new script source reference.
        /// </summary>
        /// <param name="baseDirectory">The source root used to compute module-relative paths.</param>
        /// <param name="fullPath">The absolute file path or virtual source identifier.</param>
        /// <param name="modulePath">The module-relative path used by the compiler and runtime.</param>
        public ScriptSourceReference(string baseDirectory, string fullPath, string modulePath)
        {
            BaseDirectory = ScriptPath.NormalizeBaseDirectory(baseDirectory);
            FullPath = ScriptPath.GetFullPath(BaseDirectory, fullPath);
            ModulePath = string.IsNullOrWhiteSpace(modulePath)
                ? ScriptPath.GetModulePath(BaseDirectory, FullPath)
                : modulePath.Replace('\\', '/');
        }

        /// <summary>
        /// Gets the source root used to compute module-relative paths.
        /// </summary>
        public string BaseDirectory { get; }

        /// <summary>
        /// Gets the absolute file path or virtual source identifier.
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// Gets the module-relative path used by the compiler and runtime.
        /// </summary>
        public string ModulePath { get; }
    }
}
