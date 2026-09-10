using System;
using System.Collections.Generic;
using AuroraScript.Runtime;

namespace AuroraScript
{
    /// <summary>
    /// Options for compiling a lightweight script block.
    /// </summary>
    public sealed class CompileBlockOptions
    {
        /// <summary>
        /// Names of positional arguments exposed as local variables in the compiled block.
        /// </summary>
        public IReadOnlyList<string> Parameters { get; init; } = Array.Empty<string>();

        /// <summary>
        /// Virtual source name used in diagnostics.
        /// </summary>
        public string SourceName { get; init; }

        /// <summary>The initialized domain whose modules imports bind to. Imported blocks stay in this domain.</summary>
        public ScriptDomain Domain { get; init; }

        /// <summary>Source root for relative imports. Defaults to the engine source resolver root.</summary>
        public string BaseDirectory { get; init; }
    }
}
