using System;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// Marks a core implementation method that should be exposed to scripts through a
    /// generated Datum adapter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class AuroraExportAttribute : Attribute
    {
        /// <summary>Marks a Core method for generated script export.</summary>
        public AuroraExportAttribute(string scriptName = null)
        {
            ScriptName = scriptName;
        }

        /// <summary>Script member name. Defaults to the core method name without a <c>Core</c> suffix.</summary>
        public string ScriptName { get; }

        /// <summary>Failure behavior for generated Datum adapters on dynamic call sites.</summary>
        public AuroraExportFailure Failure { get; set; } = AuroraExportFailure.Default;
    }
}
