namespace AuroraScript.Source
{
    /// <summary>
    /// Represents a source of AuroraScript code, providing path resolution and content retrieval.
    /// Implementation can be file-based, memory-based, or custom.
    /// </summary>
    /// <remarks>
    /// <see cref="BaseDirectory"/> is the resolver root that produced the source.
    /// <see cref="FullPath"/> is the normalized source identity used by the compiler.
    /// Implementations should expose paths with '/' separators.
    /// </remarks>
    public interface ScriptSource
    {
        /// <summary>
        /// Gets the resolver root used for read routing and module-relative paths.
        /// </summary>
        string BaseDirectory { get; }

        /// <summary>
        /// Gets the source path relative to the resolver root when available. This is not a module name or module identity.
        /// </summary>
        string SourcePath { get; }

        /// <summary>
        /// Gets the normalized absolute full path or virtual identifier for the script source.
        /// </summary>
        string FullPath { get; }

        /// <summary>
        /// Gets the raw script code. This might return the cached code or trigger a read depending on the implementation.
        /// </summary>
        string Code { get; }

        /// <summary>
        /// Reads the source code from its underlying storage and returns it.
        /// </summary>
        /// <returns>The script content as a string.</returns>
        string ReadSource();
    }
}
