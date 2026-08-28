using System;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// Marks a partial instantiable script object whose instance fields and methods
    /// are generated from <see cref="AuroraExportAttribute"/> members.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class AuroraNativeObjectAttribute : Attribute
    {
        /// <summary>Marks a generated instantiable native object type.</summary>
        public AuroraNativeObjectAttribute(string typeName)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        }

        /// <summary>Script constructor and <c>typeof</c> name, for example <c>Vec2</c>.</summary>
        public string TypeName { get; }
    }
}
