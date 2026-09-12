using System;

namespace AuroraScript.Hosting
{
    /// <summary>
    /// Describes a native object value supplied to a parameter of a script callback.
    /// The compiler treats this as guarded specialization metadata; the dynamic
    /// callback ABI remains authoritative.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public sealed class AuroraCallbackArgumentAttribute : Attribute
    {
        /// <summary>Creates callback argument type metadata for an exported method.</summary>
        /// <param name="callbackArgumentFromEnd">
        /// Zero-based position of the callback in script arguments, counted from the end.
        /// </param>
        /// <param name="callbackParameterIndex">Zero-based parameter position within the callback.</param>
        /// <param name="nativeType">Generated NativeType CLR representation supplied to the callback.</param>
        public AuroraCallbackArgumentAttribute(
            int callbackArgumentFromEnd,
            int callbackParameterIndex,
            Type nativeType)
        {
            if (callbackArgumentFromEnd < 0)
                throw new ArgumentOutOfRangeException(nameof(callbackArgumentFromEnd));
            if (callbackParameterIndex < 0)
                throw new ArgumentOutOfRangeException(nameof(callbackParameterIndex));
            CallbackArgumentFromEnd = callbackArgumentFromEnd;
            CallbackParameterIndex = callbackParameterIndex;
            NativeType = nativeType ?? throw new ArgumentNullException(nameof(nativeType));
        }

        /// <summary>Callback position counted from the end of script arguments.</summary>
        public int CallbackArgumentFromEnd { get; }

        /// <summary>Parameter position within the callback.</summary>
        public int CallbackParameterIndex { get; }

        /// <summary>Generated NativeType CLR representation supplied to the callback.</summary>
        public Type NativeType { get; }
    }
}
