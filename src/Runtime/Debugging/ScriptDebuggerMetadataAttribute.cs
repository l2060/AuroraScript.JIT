using System;

namespace AuroraScript.Runtime.Debugging
{
    /// <summary>
    /// Stores debugger-only AuroraScript variable metadata on generated methods.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false, Inherited = false)]
    public sealed class ScriptDebuggerMetadataAttribute : Attribute
    {
        /// <summary>Creates debugger metadata for a generated script method.</summary>
        public ScriptDebuggerMetadataAttribute(string metadata)
        {
            Metadata = metadata ?? string.Empty;
        }

        /// <summary>Gets the encoded debugger metadata.</summary>
        public string Metadata { get; }
    }
}
