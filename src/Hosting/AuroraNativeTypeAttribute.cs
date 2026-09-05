using System;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// Marks a partial CLR type whose exported static members form a script type and
    /// whose exported instance members, when present, belong to native instances.
    /// An exported constructor makes the script type constructible.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AuroraNativeTypeAttribute : Attribute
    {
        /// <summary>Marks a generated native script type.</summary>
        public AuroraNativeTypeAttribute(string typeName)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        }

        /// <summary>Script global and <c>typeof</c> name, for example <c>Vec2</c>.</summary>
        public string TypeName { get; }

        /// <summary>CLR name of an exported static Core used by both call and new for a primitive type.
        /// The Core must return the type declared by AuroraNativeReceiver;
        /// its dynamic adapter handles unproven argument shapes.</summary>
        public string ConstructorFactory { get; set; }
    }
}
