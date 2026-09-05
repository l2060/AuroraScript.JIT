using System;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// Marks a core implementation method that should be exposed to scripts through a
    /// generated Datum adapter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
    public sealed class AuroraExportAttribute : Attribute
    {
        /// <summary>
        /// Marks the sole constructor exposed through script <c>new</c>.
        /// </summary>
        public AuroraExportAttribute()
            : this(null, MatchFailure.Default)
        {
        }

        /// <summary>Marks a Core method or constant field for generated script export.</summary>
        public AuroraExportAttribute(string scriptName)
            : this(scriptName, MatchFailure.Default)
        {
        }

        /// <summary>Marks a Core method for generated script export.</summary>
        public AuroraExportAttribute(string scriptName, MatchFailure failure)
        {
            ScriptName = scriptName;
            Failure = failure;
        }

        /// <summary>Script member name. Defaults to the core method name without a <c>Core</c> suffix.</summary>
        public string ScriptName { get; }

        /// <summary>Failure behavior for generated Datum adapters on dynamic call sites.</summary>
        public MatchFailure Failure { get; set; } = MatchFailure.Default;

        /// <summary>Controls whether the export belongs to script instances or the script type object.</summary>
        public AuroraExportTarget Target { get; set; } = AuroraExportTarget.Auto;

        /// <summary>Optional existing dynamic adapter for a primitive instance or static member. When omitted,
        /// the generator creates the adapter using the usual parameter coercion and failure rules.
        /// Overloads and proof-dependent signatures require an explicit shared compatibility adapter.</summary>
        public string DynamicAdapter { get; set; }

        /// <summary>Exports a zero-argument value-receiver Core method as a read-only property.</summary>
        public bool IsGetter { get; set; }

        /// <summary>Only bind this signature when argument zero is proven to index the receiver in bounds.</summary>
        public bool RequiresIndexProof { get; set; }
    }
}
