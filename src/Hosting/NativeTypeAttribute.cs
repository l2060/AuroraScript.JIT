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
    }
}
