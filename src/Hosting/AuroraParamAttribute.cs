using System;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// Describes coercion rules for a generated Datum adapter parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class AuroraParamAttribute : Attribute
    {
        /// <summary>Gets or sets the script-to-Core coercion rule.</summary>
        public AuroraParamCoercion Coercion { get; set; } = AuroraParamCoercion.Weak;
    }
}
