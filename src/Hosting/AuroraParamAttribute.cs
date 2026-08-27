using System;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// Describes coercion rules for a generated Datum adapter parameter.
    /// </summary>
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false, AllowMultiple = false)]
    public sealed class AuroraParamAttribute : Attribute
    {

        /// <summary>Describes coercion rules for a generated Datum adapter parameter.</summary>
        public AuroraParamAttribute(MatchLevel coercion)
        {
            Coercion = coercion;
        }

        /// <summary>Gets or sets the script-to-Core coercion rule.</summary>
        public readonly MatchLevel Coercion;
    }
}
