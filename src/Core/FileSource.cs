using System.IO;
using System.Text;

namespace AuroraScript.Core
{
    /// <summary>
    /// Represents a script source loaded from the filesystem.
    /// Implements <see cref="ScriptSource"/> to provide file-based script content.
    /// </summary>
    internal class FileSource : ScriptSource
    {
        /// <summary> Gets the base directory used for relative path resolution. </summary>
        public string BaseDirectory { get; private set; }

        /// <summary> Gets the relative source path of the script. </summary>
        public string SourcePath { get; private set; }

        /// <summary> Gets the absolute full path to the script file. </summary>
        public string FullPath { get; private set; }

        /// <summary> Gets the raw script code. Only populated if cached or read. </summary>
        public string Code { get; private set; }

        /// <summary> Gets the encoding used to read the file. </summary>
        public Encoding Encoding { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSource"/> class.
        /// </summary>
        /// <param name="basePath">The base directory path.</param>
        /// <param name="path">The path to the script file (absolute or relative to basePath).</param>
        /// <param name="encoding">The encoding used for reading the file content.</param>
        public FileSource(string basePath, string path, Encoding encoding)
        {
            BaseDirectory = basePath;
            if (!Path.IsPathRooted(path))
            {
                path = Path.Combine(basePath, path);
            }

            FullPath = path;
            SourcePath = Path.GetRelativePath(basePath, path);
            Encoding = encoding;
        }

        /// <summary>
        /// Reads the script source code from the file on disk.
        /// </summary>
        /// <returns>The script content as a string.</returns>
        public string ReadSource()
        {
            return File.ReadAllText(FullPath, Encoding);
        }
    }
}
