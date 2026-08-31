using System;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// Compiler metadata emitted for an <see cref="AuroraNativeTypeAttribute"/> type
    /// that has native instances.
    /// Host code should not apply this attribute directly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class AuroraGeneratedNativeObjectAttribute : Attribute
    {
        /// <summary>Creates generated metadata for one instantiable native object type.</summary>
        public AuroraGeneratedNativeObjectAttribute(
            string typeName,
            Type objectType,
            AuroraExportValueKind[] constructorParameterKinds,
            bool constructible = true)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            ObjectType = objectType ?? throw new ArgumentNullException(nameof(objectType));
            ConstructorParameterKinds = constructorParameterKinds ??
                throw new ArgumentNullException(nameof(constructorParameterKinds));
            Constructible = constructible;
        }

        /// <summary>Script constructor and <c>typeof</c> name.</summary>
        public string TypeName { get; }

        /// <summary>CLR type that implements the native object.</summary>
        public Type ObjectType { get; }

        /// <summary>Public constructor parameter representations.</summary>
        public AuroraExportValueKind[] ConstructorParameterKinds { get; }

        /// <summary>
        /// False when the type has no public constructor the compiler can call directly.
        /// </summary>
        public bool Constructible { get; }
    }

    /// <summary>
    /// Compiler metadata emitted for an exported instance field of a native object.
    /// Host code should not apply this attribute directly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class AuroraGeneratedNativeFieldAttribute : Attribute
    {
        /// <summary>Creates generated metadata for one native object instance field.</summary>
        public AuroraGeneratedNativeFieldAttribute(
            string typeName,
            string memberName,
            Type declaringType,
            string fieldName,
            AuroraExportValueKind kind,
            bool isReadOnly)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            MemberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
            DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
            FieldName = fieldName ?? throw new ArgumentNullException(nameof(fieldName));
            Kind = kind;
            IsReadOnly = isReadOnly;
        }

        /// <summary>Owning native object type name.</summary>
        public string TypeName { get; }

        /// <summary>Exported script member name.</summary>
        public string MemberName { get; }

        /// <summary>Type that declares the instance field.</summary>
        public Type DeclaringType { get; }

        /// <summary>Public instance field name.</summary>
        public string FieldName { get; }

        /// <summary>Field representation.</summary>
        public AuroraExportValueKind Kind { get; }

        /// <summary>True when script assignment to the member is ignored.</summary>
        public bool IsReadOnly { get; }
    }

    /// <summary>
    /// Compiler metadata emitted for an exported instance method of a native object.
    /// Host code should not apply this attribute directly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class AuroraGeneratedNativeMethodAttribute : Attribute
    {
        /// <summary>Creates generated metadata for one native object instance method.</summary>
        public AuroraGeneratedNativeMethodAttribute(
            string typeName,
            string memberName,
            Type declaringType,
            string methodName,
            AuroraExportValueKind returnKind,
            AuroraExportValueKind[] parameterKinds,
            bool takesContext = false)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            MemberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
            DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            ReturnKind = returnKind;
            ParameterKinds = parameterKinds ??
                throw new ArgumentNullException(nameof(parameterKinds));
            TakesContext = takesContext;
        }

        /// <summary>Owning native object type name.</summary>
        public string TypeName { get; }

        /// <summary>Exported script member name.</summary>
        public string MemberName { get; }

        /// <summary>Type that declares the Core method.</summary>
        public Type DeclaringType { get; }

        /// <summary>Public instance Core method name.</summary>
        public string MethodName { get; }

        /// <summary>Core return representation.</summary>
        public AuroraExportValueKind ReturnKind { get; }

        /// <summary>Core parameter representations.</summary>
        public AuroraExportValueKind[] ParameterKinds { get; }

        /// <summary>
        /// True when the Core method takes a leading <c>ScriptContext</c>
        /// that is not a script argument.
        /// </summary>
        public bool TakesContext { get; }
    }
}
