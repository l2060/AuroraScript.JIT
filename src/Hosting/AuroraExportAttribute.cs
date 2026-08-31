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
    }
}
