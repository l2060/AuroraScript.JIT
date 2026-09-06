using System;

namespace AuroraScript.Hosting
{
    /// <summary>Exports a static Core method as an instance member of a NativeReceiverType.</summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class AuroraReceiverExportAttribute : Attribute
    {
        /// <summary>Marks a native receiver Core method.</summary>
        public AuroraReceiverExportAttribute(string scriptName) : this(scriptName, MatchFailure.Default)
        {
        }

        /// <summary>Marks a native receiver Core method.</summary>
        public AuroraReceiverExportAttribute(string scriptName, MatchFailure failure)
        {
            ScriptName = scriptName;
            Failure = failure;
        }

        /// <summary>Script member name.</summary>
        public string ScriptName { get; }

        /// <summary>Failure behavior for generated dynamic adapters.</summary>
        public MatchFailure Failure { get; set; } = MatchFailure.Default;

        /// <summary>Optional existing dynamic adapter shared by receiver overloads.</summary>
        public string DynamicAdapter { get; set; }

        /// <summary>Exports a zero-argument receiver Core as a property getter.</summary>
        public bool IsGetter { get; set; }

        /// <summary>Allows scripts to replace the exported property slot.</summary>
        public bool Writable { get; set; }

        /// <summary>Includes the exported member in property enumeration.</summary>
        public bool Enumerable { get; set; }
    }
}
