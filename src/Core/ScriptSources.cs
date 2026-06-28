using AuroraScript.Source;
using System.Text;

namespace AuroraScript.Core
{
    /// <summary>
    /// Helper factory for common source resolver compositions.
    /// </summary>
    public static class ScriptSources
    {
        /// <summary>
        /// Creates a file-system source resolver.
        /// </summary>
        public static FileSystemScriptSourceResolver FileSystem(string root, Encoding encoding = null)
        {
            return new FileSystemScriptSourceResolver(root, encoding);
        }

        /// <summary>
        /// Creates an in-memory source resolver.
        /// </summary>
        public static MemorySourceResolver Memory(string root = "mem://")
        {
            return new MemorySourceResolver(root);
        }

        /// <summary>
        /// Creates a composite source resolver.
        /// </summary>
        public static CompositeScriptSourceResolver Composite(params IScriptSourceResolver[] resolvers)
        {
            var composite = new CompositeScriptSourceResolver();
            if (resolvers == null)
            {
                return composite;
            }

            for (var i = 0; i < resolvers.Length; i++)
            {
                composite.Add(resolvers[i]);
            }

            return composite;
        }
    }
}
