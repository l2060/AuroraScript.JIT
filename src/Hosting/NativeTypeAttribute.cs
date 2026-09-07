using System;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// Marks a partial CLR type whose exported static members form a script type and
    /// whose exported instance members, when present, belong to native instances.
    /// Ordinary instance methods use a generated frozen prototype; native fields and
    /// accessor exports use generated property overrides. An exported constructor
    /// makes the script type constructible.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class NativeTypeAttribute : Attribute
    {
        /// <summary>Marks a generated native script type.</summary>
        public NativeTypeAttribute(string typeName)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        }

        /// <summary>Script global and <c>typeof</c> name, for example <c>Vec2</c>.</summary>
        public string TypeName { get; }

        /// <summary>CLR representation used by engine-owned value instances.</summary>
        public Type NativeReceiverType { get; set; }

        /// <summary>CLR name of an exported static Core used by both call and new for a native receiver type.
        /// The Core must return <see cref="NativeReceiverType"/>;
        /// its dynamic adapter handles unproven argument shapes.</summary>
        public string NativeConstructor { get; set; }
    }
}
