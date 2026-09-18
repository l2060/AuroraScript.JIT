using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;

namespace AuroraScript.Compiler.Backend.Emission
{
    // The guard a dynamic callable value needs is the same for every call of a
    // contract: unwrap the closure, probe its native entry, enter its frame and
    // otherwise fall back to a dynamic invoke. One thunk per call shape keeps
    // that sequence out of the calling body.
    internal sealed partial class TypedCilEmitter
    {
        private Dictionary<string, MethodInfo> _callableThunks;

        private StackValueKind EmitCallableThunkCall(
            FunctionCallExpression call,
            CallablePlan plan,
            bool materializeVoid)
        {
            var thunk = plan.Thunk ??= GetCallableThunk(plan);

            _il.Emit(OpCodes.Ldarg_0);
            EmitDatum(call.Target);
            for (var i = 0; i < call.Arguments.Count; i++)
            {
                EmitDirectArgument(call.Arguments[i], plan.ParameterTypes[i]);
            }
            _il.Emit(OpCodes.Call, thunk);
            if (plan.ReturnsVoid)
            {
                if (materializeVoid)
                {
                    EmitNull();
                    return StackValueKind.Datum;
                }
                return StackValueKind.Void;
            }

            return GetCallableReturnKind(plan.ReturnType);
        }

        // A thunk restores the script value of every argument on its fallback
        // path, so each one must round trip through a datum without storage
        // specific conversions.
        private static bool CanRoundTripThroughDatum(DirectParameterType type)
        {
            return !FlowValueTypeFacts.IsPackedArray(type.Type) &&
                (type.NativeObject != null || type.Type is FlowValueType.Int32 or FlowValueType.UInt32 or
                    FlowValueType.Int64 or FlowValueType.UInt64 or
                    FlowValueType.Number or FlowValueType.Boolean or
                    FlowValueType.String or FlowValueType.Object or
                    FlowValueType.Dynamic);
        }

        private MethodInfo GetCallableThunk(CallablePlan plan)
        {
            var key = BuildCallableThunkKey(plan);
            _callableThunks ??= new(StringComparer.Ordinal);
            if (_callableThunks.TryGetValue(key, out var existing))
            {
                return existing;
            }

            var parameters = new Type[plan.ParameterTypes.Length + 2];
            parameters[0] = typeof(ScriptContext);
            parameters[1] = typeof(ScriptDatum);
            for (var i = 0; i < plan.ParameterTypes.Length; i++)
            {
                parameters[i + 2] = GetNativeParameterType(plan.ParameterTypes[i]);
            }

            var name = plan.Callable.Name?.Value;
            var defined = _session.Builder.DefineMethod(
                _module.Source.FullPath,
                (string.IsNullOrEmpty(name) ? "callable" : name) +
                    "$callable" + _callableThunks.Count,
                plan.ReturnsVoid ? typeof(void) : plan.DelegateSignature[^1],
                parameters);
            EmitCallableThunkBody(defined.IL, plan);
            _callableThunks[key] = defined.Method;
            return defined.Method;
        }

        private static string BuildCallableThunkKey(CallablePlan plan)
        {
            var key = new StringBuilder(plan.DelegateType.FullName);
            for (var i = 0; i < plan.ParameterTypes.Length; i++)
            {
                key.Append('|').Append(plan.ParameterTypes[i].Type);
                if (plan.ParameterTypes[i].NativeObject is { } native)
                {
                    key.Append(':').Append(native.ClrType.FullName);
                }
            }
            key.Append('>').Append(plan.ReturnsVoid
                ? "void"
                : plan.ReturnType.NativeObject?.ClrType.FullName ??
                    plan.ReturnType.Type.ToString());
            key.Append('#').Append(plan.Callable.ReturnType?.Name);
            return key.ToString();
        }

        private void EmitCallableThunkBody(ILGenerator il, CallablePlan plan)
        {
            var returnsVoid = plan.ReturnsVoid;
            var closure = il.DeclareLocal(typeof(ClosureFunction));
            var nativeTarget = il.DeclareLocal(plan.DelegateType);
            var frame = il.DeclareLocal(typeof(int));
            var result = returnsVoid
                ? null
                : il.DeclareLocal(plan.DelegateSignature[^1]);
            var fallback = il.DefineLabel();
            var done = il.DefineLabel();

            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToObject);
            il.Emit(OpCodes.Isinst, typeof(ClosureFunction));
            il.Emit(OpCodes.Stloc, closure);
            il.Emit(OpCodes.Ldloc, closure);
            il.Emit(OpCodes.Brfalse, fallback);
            il.Emit(OpCodes.Ldloc, closure);
            il.Emit(
                OpCodes.Call,
                typeof(CallFrameOps).GetMethod(
                    nameof(CallFrameOps.GetNativeTarget)));
            il.Emit(OpCodes.Isinst, plan.DelegateType);
            il.Emit(OpCodes.Stloc, nativeTarget);
            il.Emit(OpCodes.Ldloc, nativeTarget);
            il.Emit(OpCodes.Brfalse, fallback);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, closure);
            il.Emit(
                OpCodes.Call,
                typeof(CallFrameOps).GetMethod(
                    nameof(CallFrameOps.EnterClosure)));
            il.Emit(OpCodes.Stloc, frame);
            il.BeginExceptionBlock();
            il.Emit(OpCodes.Ldloc, nativeTarget);
            il.Emit(OpCodes.Ldarg_0);
            for (var i = 0; i < plan.ParameterTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg, i + 2);
            }
            il.Emit(
                OpCodes.Callvirt,
                plan.DelegateType.GetMethod(nameof(Action.Invoke)));
            if (!returnsVoid)
            {
                il.Emit(OpCodes.Stloc, result);
            }
            il.BeginCatchBlock(typeof(Exception));
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, frame);
            il.Emit(OpCodes.Call, typeof(CallFrameOps).GetMethod(nameof(CallFrameOps.Abort)));
            il.Emit(OpCodes.Rethrow);
            il.EndExceptionBlock();
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldloc, frame);
            il.Emit(
                OpCodes.Call,
                typeof(CallFrameOps).GetMethod(nameof(CallFrameOps.Leave)));
            if (!returnsVoid)
            {
                il.Emit(OpCodes.Ldloc, result);
            }
            il.Emit(OpCodes.Br, done);

            il.MarkLabel(fallback);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_0);
            for (var i = 0; i < plan.ParameterTypes.Length; i++)
            {
                il.Emit(OpCodes.Ldarg, i + 2);
                EmitNativeToDatum(il, plan.ParameterTypes[i]);
            }
            il.Emit(
                OpCodes.Call,
                TypedRuntimeMetadata.Invoke[plan.ParameterTypes.Length]);
            EmitCallableResult(il, plan.Callable, plan.ReturnType, materializeVoid: false);

            il.MarkLabel(done);
            il.Emit(OpCodes.Ret);
        }

        private static void EmitNativeToDatum(
            ILGenerator il,
            DirectParameterType type)
        {
            switch (GetCallableReturnKind(type))
            {
                case StackValueKind.Int32:
                    il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromInt32);
                    return;
                case StackValueKind.UInt32:
                    il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromUInt32);
                    return;
                case StackValueKind.Int64:
                    il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromInt64);
                    return;
                case StackValueKind.UInt64:
                    il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromUInt64);
                    return;
                case StackValueKind.Number:
                    il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromNumber);
                    return;
                case StackValueKind.Boolean:
                    il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromBoolean);
                    return;
                case StackValueKind.String:
                    il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromString);
                    return;
                case StackValueKind.Object:
                    il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumFromObject);
                    return;
            }
        }
    }
}
