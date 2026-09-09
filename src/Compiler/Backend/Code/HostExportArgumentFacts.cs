using AuroraScript.Hosting;
using AuroraScript.Compiler.Ast.Expressions;
using System.Collections.Generic;
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
        public static bool TrySelectOverload(
            HostExportDescriptor descriptor,
            IReadOnlyList<Expression> arguments,
            Func<Expression, FlowValueType> getType,
            Func<Expression, Type> getClrType,
            out HostExportDescriptor selected)
        {
            HostExportDescriptor match = null;
            var bestCost = int.MaxValue;
            var ambiguous = false;
            for (var candidate = descriptor; candidate != null; candidate = candidate.NextOverload)
            {
                if (arguments.Count < candidate.RequiredScriptParameterCount ||
                    candidate.UseDynamicForExtraArguments &&
                        arguments.Count > candidate.ParameterKinds.Length)
                {
                    continue;
                }
                var provided = Math.Min(arguments.Count, candidate.ParameterKinds.Length);
                var compatible = true;
                for (var i = 0; i < provided; i++)
                {
                    if (HostExportArgumentFacts.CanPass(
                            candidate.ParameterKinds[i],
                            candidate.GetScriptParameterType(i),
                            getType(arguments[i]),
                            getClrType?.Invoke(arguments[i])))
                    {
                        continue;
                    }
                    compatible = false;
                    break;
                }
                if (!compatible) continue;
                var cost = 0;
                for (var i = 0; i < Math.Min(arguments.Count, candidate.ParameterKinds.Length); i++)
                    cost += HostExportArgumentFacts.ConversionCost(candidate.ParameterKinds[i], getType(arguments[i]));
                if (cost > bestCost) continue;
                if (cost == bestCost) { ambiguous = true; continue; }
                match = candidate;
                bestCost = cost;
                ambiguous = false;
            }
            selected = ambiguous ? null : match;
            return selected != null;
        }

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
                AuroraExportValueKind.Int64 =>
                    argumentType == FlowValueType.Int64,
                AuroraExportValueKind.UInt64 =>
                    argumentType == FlowValueType.UInt64,
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

        public static int ConversionCost(AuroraExportValueKind kind, FlowValueType argumentType) =>
            kind == AuroraExportValueKind.Datum ? 2 :
            kind == AuroraExportValueKind.Number && argumentType != FlowValueType.Number ? 1 : 0;

        public static bool HasParams(AuroraExportValueKind[] kinds) => kinds.Length > 0 &&
            kinds[kinds.Length - 1] is AuroraExportValueKind.DatumParams or AuroraExportValueKind.NumberParams;

        public static AuroraExportValueKind ParamsElementKind(AuroraExportValueKind kind) =>
            kind == AuroraExportValueKind.DatumParams ? AuroraExportValueKind.Datum : AuroraExportValueKind.Number;

        public static void GetArgumentParameter(AuroraExportValueKind[] kinds,
            System.Reflection.ParameterInfo[] parameters, int prefix, int argumentIndex,
            out AuroraExportValueKind kind, out Type type)
        {
            var index = Math.Min(argumentIndex, kinds.Length - 1);
            kind = kinds[index];
            type = parameters[prefix + index].ParameterType;
            if (kind is AuroraExportValueKind.DatumParams or AuroraExportValueKind.NumberParams)
            {
                kind = ParamsElementKind(kind);
                type = type.GetElementType();
            }
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
                    FlowValueTypeFacts.IsPackedArray(argumentType);
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
