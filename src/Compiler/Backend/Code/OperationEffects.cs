using AuroraScript.Runtime.Types;

namespace AuroraScript.Compiler.Backend.Code
{
    internal static class OperationEffects
    {
        // Built-in indexed storage does not invoke user code. An arbitrary native
        // indexer is still a call boundary, even when its signature is identical.
        internal static bool IsIntrinsicIndex(FlowValueType receiver,
            HostNativeObjectDescriptor native, FlowValueType index) =>
            FlowValueTypeFacts.IsNumeric(index) &&
            (FlowValueTypeFacts.IsPackedArray(receiver) || native?.ClrType == typeof(ScriptArray));

        internal static bool IsPrimitive(FlowValueType type) => type is
            FlowValueType.Null or FlowValueType.Boolean or FlowValueType.String or
            FlowValueType.Number or FlowValueType.Int32 or FlowValueType.UInt32 or
            FlowValueType.Int64 or FlowValueType.UInt64;
    }
}
