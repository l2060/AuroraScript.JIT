using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System;

namespace AuroraScript.Compiler.Backend.Code
{
    /// <summary>
    /// Decides whether a script argument was proven compatible with a generated
    /// host export parameter. The typed analyzer and the emitter must agree, so
    /// both go through this single predicate.
    /// </summary>
    internal static class HostExportArgumentFacts
    {
        /// <param name="parameterKind">Declared host representation of the parameter.</param>
        /// <param name="parameterType">CLR type of the parameter.</param>
        /// <param name="argumentType">Proven flow type of the argument.</param>
        /// <param name="argumentClrType">
        /// The native object type the argument was proven to hold, or null when only
        /// the coarse flow type is known.
        /// </param>
        public static bool CanPass(
            AuroraExportValueKind parameterKind,
            Type parameterType,
            FlowValueType argumentType,
            Type argumentClrType = null)
        {
            return parameterKind switch
            {
                AuroraExportValueKind.Number =>
                    argumentType is FlowValueType.Number or FlowValueType.Int32 or
                        FlowValueType.UInt32,
                AuroraExportValueKind.Int32 =>
                    argumentType == FlowValueType.Int32,
                AuroraExportValueKind.Boolean =>
                    argumentType == FlowValueType.Boolean,
                AuroraExportValueKind.String =>
                    argumentType == FlowValueType.String,
                AuroraExportValueKind.Object =>
                    CanPassObject(parameterType, argumentType, argumentClrType),
                AuroraExportValueKind.Datum => true,
                _ => false
            };
        }

        private static bool CanPassObject(
            Type parameterType,
            FlowValueType argumentType,
            Type argumentClrType)
        {
            if (argumentClrType != null &&
                parameterType.IsAssignableFrom(argumentClrType))
            {
                return true;
            }
            if (parameterType == typeof(ScriptObject))
            {
                return argumentType == FlowValueType.Object ||
                    argumentType == FlowValueType.Array ||
                    FlowValueTypeFacts.IsPackedArray(argumentType);
            }
            if (parameterType == typeof(ScriptArray))
            {
                return argumentType == FlowValueType.Array;
            }
            if (parameterType == typeof(ScriptPackedArray))
            {
                return FlowValueTypeFacts.IsPackedArray(argumentType);
            }

            return (parameterType == typeof(ScriptInt32Array) &&
                    argumentType == FlowValueType.Int32Array) ||
                (parameterType == typeof(ScriptInt8Array) &&
                    argumentType == FlowValueType.Int8Array) ||
                (parameterType == typeof(ScriptFloat32Array) &&
                    argumentType == FlowValueType.Float32Array) ||
                (parameterType == typeof(ScriptFloat64Array) &&
                    argumentType == FlowValueType.Float64Array) ||
                (parameterType == typeof(ScriptBooleanArray) &&
                    argumentType == FlowValueType.BooleanArray) ||
                (parameterType == typeof(ScriptUInt8Array) &&
                    argumentType == FlowValueType.UInt8Array) ||
                (parameterType == typeof(ScriptInt16Array) &&
                    argumentType == FlowValueType.Int16Array) ||
                (parameterType == typeof(ScriptUInt16Array) &&
                    argumentType == FlowValueType.UInt16Array) ||
                (parameterType == typeof(ScriptUInt32Array) &&
                    argumentType == FlowValueType.UInt32Array) ||
                (parameterType == typeof(ScriptInt64Array) &&
                    argumentType == FlowValueType.Int64Array) ||
                (parameterType == typeof(ScriptUInt64Array) &&
                    argumentType == FlowValueType.UInt64Array);
        }
    }
}
