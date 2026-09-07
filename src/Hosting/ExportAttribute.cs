using System;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// Marks a core implementation method that should be exposed to scripts through a
    /// generated Datum adapter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
    public sealed class ExportAttribute : Attribute
    {
        /// <summary>
        /// Marks the sole constructor exposed through script <c>new</c>.
        /// </summary>
        public ExportAttribute()
            : this(null, MatchFailure.Default)
        {
        }

        /// <summary>Marks a Core method or constant field for generated script export.</summary>
        public ExportAttribute(string scriptName)
            : this(scriptName, MatchFailure.Default)
        {
        }

        /// <summary>Marks a Core method for generated script export.</summary>
        public ExportAttribute(string scriptName, MatchFailure failure)
        {
            ScriptName = scriptName;
            Failure = failure;
        }

        /// <summary>Script member name. Defaults to the core method name without a <c>Core</c> suffix.</summary>
        public string ScriptName { get; }

        /// <summary>Failure behavior for generated Datum adapters on dynamic call sites.</summary>
        public MatchFailure Failure { get; set; } = MatchFailure.Default;

        /// <summary>Optional existing dynamic adapter for an exported instance/static member or constructor. When omitted,
        /// the generator creates the adapter using the usual parameter coercion and failure rules.
        /// Overloads require an explicit shared compatibility adapter. Constructor adapters receive the full argument
        /// span; those constructors are not bypassed by direct CLR constructor emission.</summary>
        public string DynamicAdapter { get; set; }

        /// <summary>Exports a zero-argument instance Core method as a property getter.</summary>
        public bool IsGetter { get; set; }

        /// <summary>Exports a one-argument object-backed native instance Core method as a property setter.</summary>
        public bool IsSetter { get; set; }

        /// <summary>Allows scripts to replace the exported property slot.</summary>
        public bool Writable { get; set; }

        /// <summary>Includes the exported member in property enumeration.</summary>
        public bool Enumerable { get; set; }
    }
}
