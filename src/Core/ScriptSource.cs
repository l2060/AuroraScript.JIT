using System;

namespace AuroraScript.Core
{
    /// <summary>
    /// Represents a source of AuroraScript code, providing path resolution and content retrieval.
    /// Implementation can be file-based, memory-based, or custom.
    /// </summary>
    public interface ScriptSource
    {
        /// <summary>
        /// Gets the base directory used for resolving relative paths within this source.
        /// </summary>
        string BaseDirectory { get; }

        /// <summary>
        /// Gets the relative source path of the script.
        /// </summary>
        string SourcePath { get; }

        /// <summary>
        /// Gets the absolute full path to the script source on the filesystem or virtual storage.
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
