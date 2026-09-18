using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed partial class TypedCilEmitter
    {
        private Dictionary<FunctionTypeDeclaration, CallablePlan> _callablePlans;

        private bool TryEmitCallableCall(
            FunctionCallExpression call,
            bool materializeVoid,
            out StackValueKind returnKind)
        {
            returnKind = StackValueKind.Datum;
            if (!_code.TryGetCallableType(
                _module.Declaration, call.Target, out var callable, out var callableModule))
                return false;

            var plan = GetCallablePlan(callable, callableModule);
            ReportCallableCallWarnings(call, plan);
            if (plan.ReturnType.Type == FlowValueType.None)
                return false;

            if (CanUseCallableThunk(call, plan))
            {
                returnKind = EmitCallableThunkCall(call, plan, materializeVoid);
            }
            else
            {
                // Reuse the ordinary entry for wide calls, spread and arguments
                // that cannot safely round trip before the native target probe.
                EmitCall(call);
                returnKind = EmitCallableResult(_il, callable, plan.ReturnType, materializeVoid);
            }
            return true;
        }

        // Only contract facts are cached. Operand facts can differ between call
        // sites and guarded emission, so they are checked separately below.
        private CallablePlan GetCallablePlan(
            FunctionTypeDeclaration callable,
            ModuleDeclaration callableModule)
        {
            _callablePlans ??= new(ReferenceEqualityComparer.Instance);
            if (_callablePlans.TryGetValue(callable, out var plan))
                return plan;

            var returnFlow = TypeReferenceFacts.GetFlowType(
                callableModule, callable.ReturnType, _session.CompileSession.HostExports);
            TypeReferenceFacts.TryGetNativeObject(
                _session.CompileSession.HostExports, callable.ReturnType, out var nativeReturn);
            var returnType = new DirectParameterType(returnFlow, nativeObject: nativeReturn);
            var returnsVoid = TypeReferenceFacts.IsVoid(callable.ReturnType);
            var canUseThunk = callable.Parameters.Count < TypedRuntimeMetadata.Invoke.Length &&
                (returnsVoid || CanRoundTripThroughDatum(returnType));
            var parameterTypes = new DirectParameterType[callable.Parameters.Count];
            for (var i = 0; i < callable.Parameters.Count; i++)
            {
                var parameter = callable.Parameters[i];
                var declared = parameter.DeclaredType;
                var flow = declared == null ? FlowValueType.Dynamic :
                    TypeReferenceFacts.GetFlowType(
                        callableModule, declared, _session.CompileSession.HostExports);
                TypeReferenceFacts.TryGetNativeObject(
                    _session.CompileSession.HostExports, declared, out var native);
                var type = new DirectParameterType(flow, nativeObject: native);
                parameterTypes[i] = type;
                canUseThunk &= parameter.Initializer == null && !parameter.IsSpreadOperator &&
                    CanRoundTripThroughDatum(type);
            }
            Type[] delegateSignature = null;
            if (canUseThunk)
            {
                delegateSignature = new Type[parameterTypes.Length + 2];
                delegateSignature[0] = typeof(ScriptContext);
                for (var i = 0; i < parameterTypes.Length; i++)
                    delegateSignature[i + 1] = GetNativeParameterType(parameterTypes[i]);
                delegateSignature[^1] = returnsVoid ? typeof(void) : GetNativeParameterType(returnType);
            }
            plan = new CallablePlan(callable, parameterTypes, delegateSignature, returnType);
            _callablePlans.Add(callable, plan);
            return plan;
        }

        private bool CanUseCallableThunk(FunctionCallExpression call, CallablePlan plan)
        {
            if (plan.DelegateType == null || HasSpread(call.Arguments) ||
                plan.ParameterTypes.Length != call.Arguments.Count)
                return false;
            for (var i = 0; i < plan.ParameterTypes.Length; i++)
            {
                var type = plan.ParameterTypes[i];
                var flow = type.Type;
                var actual = _code.GetExpressionType(call.Arguments[i]);
                // Native argument compatibility permits narrowing and changes of
                // script kind. A thunk's fallback must instead recover the original
                // datum, without introducing a conversion failure before its probe.
                if (!(flow == FlowValueType.Dynamic || flow == actual ||
                        flow == FlowValueType.Number &&
                            actual is FlowValueType.Int32 or FlowValueType.UInt32) ||
                    type.NativeObject != null && !ReferenceEquals(
                        _code.GetNativeObjectType(call.Arguments[i]), type.NativeObject))
                    return false;
            }
            return true;
        }

        // Both the shared thunk and ordinary dynamic calls use this return boundary.
        private static StackValueKind EmitCallableResult(
            ILGenerator il,
            FunctionTypeDeclaration callable,
            DirectParameterType returnType,
            bool materializeVoid)
        {
            if (TypeReferenceFacts.IsVoid(callable.ReturnType))
            {
                il.Emit(OpCodes.Pop);
                if (!materializeVoid)
                    return StackValueKind.Void;
                il.Emit(OpCodes.Ldsfld, TypedRuntimeMetadata.DatumNull);
                return StackValueKind.Datum;
            }
            if (returnType.NativeObject == null &&
                FlowValueTypeFacts.FromCheckedTypeName(callable.ReturnType.Name) != FlowValueType.None)
                il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetTypeCheck(
                    FlowValueTypeFacts.GetCheckedType(callable.ReturnType.Name)));

            // Packed storage is not a script value; keep the checked datum when
            // no native object representation is available.
            if (returnType.NativeObject == null && FlowValueTypeFacts.IsPackedArray(returnType.Type))
                return StackValueKind.Datum;
            EmitDatumToNativeParameter(il, returnType);
            return GetCallableReturnKind(returnType);
        }

        private sealed class CallablePlan
        {
            public CallablePlan(
                FunctionTypeDeclaration callable,
                DirectParameterType[] parameterTypes,
                Type[] delegateSignature,
                DirectParameterType returnType)
            {
                Callable = callable;
                ParameterTypes = parameterTypes;
                DelegateSignature = delegateSignature;
                DelegateType = delegateSignature == null ? null :
                    System.Linq.Expressions.Expression.GetDelegateType(delegateSignature);
                ReturnType = returnType;
            }

            public FunctionTypeDeclaration Callable { get; }
            public DirectParameterType[] ParameterTypes { get; }
            public Type[] DelegateSignature { get; }
            public Type DelegateType { get; }
            public bool ReturnsVoid => TypeReferenceFacts.IsVoid(Callable.ReturnType);
            public DirectParameterType ReturnType { get; }
            public MethodInfo Thunk { get; set; }
        }
    }
}
