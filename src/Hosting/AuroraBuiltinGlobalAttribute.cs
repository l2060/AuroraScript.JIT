using System;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// Marks a partial script-global object implementation whose script exports are
    /// generated from <see cref="AuroraExportAttribute"/> core methods.
    /// </summary>
    [Obsolete("Use AuroraNativeTypeAttribute. Static-only native types omit an exported constructor.")]
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AuroraNativeModuleAttribute : Attribute
    {
        /// <summary>Marks a generated immutable global object.</summary>
        public AuroraNativeModuleAttribute(string globalName)
        {
            GlobalName = globalName ?? throw new ArgumentNullException(nameof(globalName));
        }

        /// <summary>Script global name, for example <c>Stats</c>.</summary>
        public string GlobalName { get; }

        /// <summary>Whether script code may assign to the global.</summary>
        public bool Writable { get; set; }

        /// <summary>Whether the global appears in enumeration.</summary>
        public bool Enumerable { get; set; }
    }
}
