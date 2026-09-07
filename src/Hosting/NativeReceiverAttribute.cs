using System;

namespace AuroraScript.Hosting
{
    /// <summary>Declares the CLR representation of an engine-owned primitive type.</summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    internal sealed class NativeReceiverAttribute : Attribute
    {
        public NativeReceiverAttribute(Type receiverType)
        {
            ReceiverType = receiverType ?? throw new ArgumentNullException(nameof(receiverType));
        }

        public Type ReceiverType { get; }

        /// <summary>Exported static Core used for call and new, returning the receiver type.</summary>
        public string Constructor { get; set; }
    }
}
