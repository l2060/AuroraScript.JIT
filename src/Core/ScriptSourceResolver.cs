using System.IO;
using System.Text;

namespace AuroraScript.Core
{
    /// <summary>
    /// Resolves script imports to source objects. Custom implementations can load scripts
    /// from memory, embedded resources, databases, or a virtual file system.
    /// </summary>
    public interface IScriptSourceResolver
    {
        /// <summary>
        /// Resolves an import/include path from the current source into a stable source reference.
        /// </summary>
        /// <param name="baseDirectory">The source root configured for the compiler.</param>
        /// <param name="currentSourcePath">The absolute path or virtual identifier of the importing source.</param>
        /// <param name="requestedPath">The path text from the import/include statement.</param>
        /// <param name="extension">The configured script file extension.</param>
        /// <param name="source">The resolved source reference when resolution succeeds.</param>
        /// <returns><c>true</c> when the source exists; otherwise, <c>false</c>.</returns>
        bool TryResolve(
            string baseDirectory,
            string currentSourcePath,
            string requestedPath,
            string extension,
            out ScriptSourceReference source);

        /// <summary>
        /// Opens a previously resolved source reference.
        /// </summary>
        ScriptSource Open(ScriptSourceReference source, Encoding encoding);
    }

    /// <summary>
    /// Default script source resolver that loads imported scripts from the file system.
    /// </summary>
    public sealed class FileScriptSourceResolver : IScriptSourceResolver
    {
        /// <summary>
        /// Gets the shared file-system resolver instance.
        /// </summary>
        public static readonly FileScriptSourceResolver Instance = new FileScriptSourceResolver();

        private FileScriptSourceResolver()
        {
        }

        /// <inheritdoc />
        public bool TryResolve(
            string baseDirectory,
            string currentSourcePath,
            string requestedPath,
            string extension,
            out ScriptSourceReference source)
        {
            var currentDirectory = ScriptPath.GetDirectoryName(currentSourcePath);
            var fullPath = ScriptPath.EnsureExtension(ScriptPath.Combine(currentDirectory, requestedPath), extension);
            if (!File.Exists(fullPath))
            {
                source = default;
                return false;
            }

            source = new ScriptSourceReference(baseDirectory, fullPath);
            return true;
        }

        /// <inheritdoc />
        public ScriptSource Open(ScriptSourceReference source, Encoding encoding)
        {
            return new FileSource(source.BaseDirectory, source.FullPath, encoding);
        }
    }
}
