using System;
using System.Collections.Generic;

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
    }
}
