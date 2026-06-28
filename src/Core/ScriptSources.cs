using AuroraScript.Source;
using System.Text;

namespace AuroraScript.Core
{
    /// <summary>
    /// Helper factory for common source resolver compositions.
    /// </summary>
    /// <remarks>
    /// Source resolvers normalize roots and full paths to use '/' separators. Relative
    /// import/include paths are resolved from the importing source path; entry paths are
    /// resolved from the resolver root.
    /// </remarks>
    public static class ScriptSources
    {
        /// <summary>
        /// Creates a file-system source resolver rooted at <paramref name="root"/>.
        /// </summary>
        /// <remarks>
        /// The resolver enumerates files under its root for <c>BuildAsync()</c>. During
        /// dependency resolution it follows the importer's full path, so a relative
        /// import can point outside the configured root when the target file exists; the
        /// returned reference still routes back to this resolver root.
        /// </remarks>
        public static FileSystemScriptSourceResolver FileSystem(string root, Encoding encoding = null)
        {
            return new FileSystemScriptSourceResolver(root, encoding);
        }

        /// <summary>
        /// Creates an in-memory source resolver rooted at <paramref name="root"/>.
        /// </summary>
        /// <remarks>
        /// Added paths are normalized under the memory root. A memory resolver only
        /// resolves targets that fall under that root and are present in its source table.
        /// </remarks>
        public static MemorySourceResolver Memory(string root = "mem://")
        {
            return new MemorySourceResolver(root);
        }

        /// <summary>
        /// Creates an ordered composite source resolver.
        /// </summary>
        /// <remarks>
        /// Resolvers are queried in the order supplied. The first resolver that can
        /// resolve a target wins. <c>GetSourceAsync</c> later routes by exact
        /// <c>ScriptSourceReference.BaseDirectory</c>, and <c>GetAllSourcesAsync</c>
        /// de-duplicates by normalized full path so earlier overlays hide later sources
        /// with the same source identity.
        /// </remarks>
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
