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
        /// <param name="modulePath">The resolver-relative source path used for display and source-relative behavior.</param>
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
        /// Gets the resolver-relative source path. This is not a module name or module identity.
        /// </summary>
        public string ModulePath { get; }
    }
}
