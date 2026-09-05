using System;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// On a type, defines its engine-owned primitive CLR representation.
    /// On an exported static Core, marks an instance operation: the first parameter after an
    /// optional ScriptContext is the receiver, not a script argument.
    /// Unmarked static exports remain members of the script type object.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
    public sealed class AuroraNativeReceiverAttribute : Attribute
    {
        /// <summary>Marks a receiver Core method. Its containing type must declare a receiver type.</summary>
        public AuroraNativeReceiverAttribute() { }

        /// <summary>Declares a type's primitive representation: string, double, long or ulong.</summary>
        public AuroraNativeReceiverAttribute(Type receiverType)
            => ReceiverType = receiverType ?? throw new ArgumentNullException(nameof(receiverType));

        /// <summary>CLR representation for a type-level declaration; null for a method-level marker.</summary>
        public Type ReceiverType { get; }
    }
}
