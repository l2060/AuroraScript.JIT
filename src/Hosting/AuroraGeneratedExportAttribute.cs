using System;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// Compiler metadata emitted for an <see cref="AuroraExportAttribute"/> core method.
    /// Host code should not apply this attribute directly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
    public sealed class AuroraGeneratedExportAttribute : Attribute
    {
        /// <summary>Creates generated metadata for one immutable global member.</summary>
        public AuroraGeneratedExportAttribute(
            string globalName,
            string memberName,
            Type declaringType,
            string methodName,
            AuroraExportValueKind returnKind,
            AuroraExportValueKind[] parameterKinds)
        {
            GlobalName = globalName ?? throw new ArgumentNullException(nameof(globalName));
            MemberName = memberName ?? throw new ArgumentNullException(nameof(memberName));
            DeclaringType = declaringType ?? throw new ArgumentNullException(nameof(declaringType));
            MethodName = methodName ?? throw new ArgumentNullException(nameof(methodName));
            ReturnKind = returnKind;
            ParameterKinds = parameterKinds ?? throw new ArgumentNullException(nameof(parameterKinds));
        }

        /// <summary>Immutable global object name.</summary>
        public string GlobalName { get; }

        /// <summary>Exported script member name.</summary>
        public string MemberName { get; }

        /// <summary>Type that declares the Core method.</summary>
        public Type DeclaringType { get; }

        /// <summary>Core method name.</summary>
        public string MethodName { get; }

        /// <summary>Core return representation.</summary>
        public AuroraExportValueKind ReturnKind { get; }

        /// <summary>Core parameter representations.</summary>
        public AuroraExportValueKind[] ParameterKinds { get; }
    }

    /// <summary>Native representations supported by generated host exports.</summary>
    public enum AuroraExportValueKind : byte
    {
        /// <summary>No return value.</summary>
        Void,

        /// <summary>A CLR <see cref="double"/>.</summary>
        Number,

        /// <summary>A CLR <see cref="int"/> represented as a script number.</summary>
        Int32,

        /// <summary>A CLR <see cref="bool"/>.</summary>
        Boolean,

        /// <summary>A CLR <see cref="string"/>.</summary>
        String,

        /// <summary>An AuroraScript <c>ScriptObject</c> reference.</summary>
        Object
    }
}
