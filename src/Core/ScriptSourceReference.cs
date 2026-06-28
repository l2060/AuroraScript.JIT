namespace AuroraScript.Core
{
    /// <summary>
    /// Identifies a resolved script source in a script source resolver.
    /// </summary>
    /// <remarks>
    /// <see cref="BaseDirectory"/> is the resolver root that can read this reference.
    /// <see cref="FullPath"/> is the normalized target path. Both use '/' separators.
    /// </remarks>
    public readonly struct ScriptSourceReference
    {
        /// <summary>
        /// Initializes a new script source reference.
        /// </summary>
        /// <param name="baseDirectory">The resolver root used for source read routing and module-relative paths.</param>
        /// <param name="fullPath">The absolute file path or virtual source identifier.</param>
        public ScriptSourceReference(string baseDirectory, string fullPath)
            : this(baseDirectory, fullPath, null)
        {
        }

        /// <summary>
        /// Initializes a new script source reference.
        /// </summary>
        /// <param name="baseDirectory">The resolver root used for source read routing and module-relative paths.</param>
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
        /// Gets the resolver root used for source read routing and module-relative paths.
        /// </summary>
        public string BaseDirectory { get; }

        /// <summary>
        /// Gets the normalized absolute file path or virtual source identifier.
        /// </summary>
        public string FullPath { get; }

        /// <summary>
        /// Gets the module-relative path used by the compiler and runtime.
        /// </summary>
        public string ModulePath { get; }
    }
}
