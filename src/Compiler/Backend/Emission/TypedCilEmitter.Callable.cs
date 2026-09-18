using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Runtime;
using System;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed partial class TypedCilEmitter
    {
        private bool TryEmitCallableCall(
            FunctionCallExpression call,
            bool materializeVoid,
            out StackValueKind returnKind)
        {
            returnKind = StackValueKind.Datum;
            if (UnwrapGroup(call.Target) is not NameExpression name)
                return false;

            var binding = _code.GetName(name);
            if (!binding.IsLocal ||
                _function.LocalSlots[binding.Local.Value].Declaration
                    is not ParameterDeclaration parameter ||
                !TypeReferenceFacts.TryGetFunctionType(
                    _module.Declaration, parameter.DeclaredType, out var callable))
                return false;

            var callableModule = callable.Parent as ModuleDeclaration ?? _module.Declaration;
            ReportCallableCallWarnings(call, callable, callableModule);
            var returnFlow = TypeReferenceFacts.GetFlowType(
                callableModule, callable.ReturnType, _session.CompileSession.HostExports);
            if (returnFlow == FlowValueType.None)
                return false;

            TypeReferenceFacts.TryGetNativeObject(
                _session.CompileSession.HostExports, callable.ReturnType, out var nativeReturn);
            var returnType = new DirectParameterType(returnFlow, nativeObject: nativeReturn);
            if (TryPlanCallableCall(call, callable, callableModule, returnType, out var plan))
            {
                returnKind = EmitCallableThunkCall(call, plan, materializeVoid);
            }
            else
            {
                // Reuse the ordinary entry for wide calls, spread and arguments
                // that cannot safely round trip before the native target probe.
                EmitCall(call);
                returnKind = EmitCallableResult(_il, callable, returnType, materializeVoid);
            }
            return true;
        }

        private bool TryPlanCallableCall(
            FunctionCallExpression call,
            FunctionTypeDeclaration callable,
            ModuleDeclaration callableModule,
            DirectParameterType returnType,
            out CallablePlan plan)
        {
            plan = default;
            var returnsVoid = TypeReferenceFacts.IsVoid(callable.ReturnType);
            if (HasSpread(call.Arguments) ||
                callable.Parameters.Count != call.Arguments.Count ||
                call.Arguments.Count >= TypedRuntimeMetadata.Invoke.Length ||
                !returnsVoid && !CanRoundTripThroughDatum(returnType))
                return false;

            var parameterTypes = new DirectParameterType[callable.Parameters.Count];
            var delegateSignature = new Type[callable.Parameters.Count + 2];
            delegateSignature[0] = typeof(ScriptContext);
            for (var i = 0; i < callable.Parameters.Count; i++)
            {
                var parameter = callable.Parameters[i];
                if (parameter.Initializer != null || parameter.IsSpreadOperator)
                    return false;
                var declared = parameter.DeclaredType;
                var flow = declared == null ? FlowValueType.Dynamic :
                    TypeReferenceFacts.GetFlowType(
                        callableModule, declared, _session.CompileSession.HostExports);
                TypeReferenceFacts.TryGetNativeObject(
                    _session.CompileSession.HostExports, declared, out var native);
                var type = new DirectParameterType(flow, nativeObject: native);
                var actual = _code.GetExpressionType(call.Arguments[i]);
                // Native argument compatibility permits narrowing and changes of
                // script kind. A thunk's fallback must instead recover the original
                // datum, without introducing a conversion failure before its probe.
                if (!CanRoundTripThroughDatum(type) ||
                    !(flow == FlowValueType.Dynamic || flow == actual ||
                        flow == FlowValueType.Number &&
                            actual is FlowValueType.Int32 or FlowValueType.UInt32) ||
                    native != null && !ReferenceEquals(
                        _code.GetNativeObjectType(call.Arguments[i]), native))
                    return false;

                parameterTypes[i] = type;
                delegateSignature[i + 1] = GetNativeParameterType(type);
            }
            delegateSignature[^1] = returnsVoid ? typeof(void) : GetNativeParameterType(returnType);
            plan = new CallablePlan(callable, parameterTypes, delegateSignature, returnType);
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

        private static Expression UnwrapGroup(Expression expression)
        {
            while (expression is GroupExpression group && group.Expressions.Count == 1)
                expression = group.Expressions[0];
            return expression;
        }

        private readonly struct CallablePlan
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
                DelegateType = System.Linq.Expressions.Expression.GetDelegateType(delegateSignature);
                ReturnType = returnType;
            }

            public FunctionTypeDeclaration Callable { get; }
            public DirectParameterType[] ParameterTypes { get; }
            public Type[] DelegateSignature { get; }
            public Type DelegateType { get; }
            public bool ReturnsVoid => TypeReferenceFacts.IsVoid(Callable.ReturnType);
            public DirectParameterType ReturnType { get; }
        }
    }
}
