using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Hosting;
using AuroraScript.Runtime.Types;
using System;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    /// <summary>
    /// Direct CIL for host native instances. Once the typed analyzer proves a receiver
    /// holds a generated <c>IAuroraNativeInstance</c>, member access bypasses the dynamic
    /// property protocol and binds straight to the CLR field, method, or constructor.
    /// </summary>
    internal sealed partial class TypedCilEmitter
    {
        /// <summary>
        /// True when argument zero of the method being emitted is the script context.
        /// </summary>
        private bool HasContextArgument => !_directMode || _function.IsNativeDeclared;

        private bool TryGetNativeIndexer(Expression receiver, Expression index, out HostNativeObjectDescriptor owner)
        {
            owner = _code.GetNativeObjectType(receiver);
            return owner?.IndexGetter != null && owner.IndexSetter != null &&
                FlowValueTypeFacts.IsNumeric(_code.GetExpressionType(index));
        }

        private StackValueKind EmitNativeIndexRead(Expression receiver, Expression index, HostNativeObjectDescriptor owner)
        {
            EmitNativeReceiver(receiver, owner);
            EmitInt32Value(index);
            _il.Emit(OpCodes.Callvirt, owner.IndexGetter);
            return StackValueKind.Datum;
        }

        private void EmitNativeIndexTarget(Expression receiverExpression, Expression indexExpression,
            HostNativeObjectDescriptor owner, out LocalBuilder receiver, out LocalBuilder index)
        {
            receiver = DeclareLocal(owner.ClrType);
            index = DeclareLocal(typeof(int));
            EmitNativeReceiver(receiverExpression, owner);
            _il.Emit(OpCodes.Stloc, receiver);
            EmitInt32Value(indexExpression);
            _il.Emit(OpCodes.Stloc, index);
        }

        private StackValueKind EmitNativeIndexWrite(SetElementExpression expression, HostNativeObjectDescriptor owner)
        {
            EmitNativeIndexTarget(expression.Object, expression.Index, owner, out var receiver, out var index);
            var value = DeclareLocal(typeof(Runtime.ScriptDatum));
            EmitDatum(expression.Value);
            _il.Emit(OpCodes.Stloc, value);
            EmitNativeIndexStore(owner, receiver, index, value);
            _il.Emit(OpCodes.Ldloc, value);
            return StackValueKind.Datum;
        }

        private void EmitNativeIndexStore(HostNativeObjectDescriptor owner,
            LocalBuilder receiver, LocalBuilder index, LocalBuilder value)
        {
            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldloc, index);
            _il.Emit(OpCodes.Ldloc, value);
            _il.Emit(OpCodes.Callvirt, owner.IndexSetter);
        }

        private StackValueKind EmitNativeIndexCompound(CompoundExpression expression,
            GetElementExpression element, Operator op, HostNativeObjectDescriptor owner)
        {
            EmitNativeIndexTarget(element.Object, element.Index, owner, out var receiver, out var index);
            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldloc, index);
            _il.Emit(OpCodes.Callvirt, owner.IndexGetter);
            EmitDatum(expression.Right);
            _il.Emit(OpCodes.Call, GetDynamicBinary(op));
            var value = DeclareLocal(typeof(Runtime.ScriptDatum));
            _il.Emit(OpCodes.Stloc, value);
            EmitNativeIndexStore(owner, receiver, index, value);
            _il.Emit(OpCodes.Ldloc, value);
            return StackValueKind.Datum;
        }

        private bool TryGetNativeField(
            Expression receiver,
            string memberName,
            out HostNativeObjectDescriptor owner,
            out HostNativeFieldDescriptor field)
        {
            owner = _code.GetNativeObjectType(receiver);
            if (owner == null || !owner.TryGetField(memberName, out field))
            {
                field = null;
                return false;
            }
            return IsDirectFieldKind(field.Kind);
        }

        private static bool IsDirectFieldKind(AuroraExportValueKind kind)
        {
            return kind is AuroraExportValueKind.Number or
                AuroraExportValueKind.Int32 or
                AuroraExportValueKind.Boolean or
                AuroraExportValueKind.String;
        }

        private bool TryGetNativeMethodCall(
            FunctionCallExpression call,
            Expression receiver,
            string memberName,
            out HostNativeObjectDescriptor owner,
            out HostNativeMethodDescriptor method)
        {
            method = null;
            owner = _code.GetNativeObjectType(receiver);
            if (owner == null ||
                !owner.TryGetMethod(memberName, out var candidate))
            {
                return false;
            }

            var bestCost = int.MaxValue;
            for (; candidate != null; candidate = candidate.NextOverload)
            {
                if ((!candidate.TakesContext || HasContextArgument) &&
                    CanBindNativeArguments(
                        call,
                        candidate.ParameterKinds,
                        candidate.RequiredScriptParameterCount,
                        candidate.Method.GetParameters(),
                        candidate.TakesContext ? 1 : 0,
                        candidate.UseDynamicForExtraArguments))
                {
                    // Keep params as a fallback; compare fixed signatures by conversion cost.
                    if (HostExportArgumentFacts.HasParams(candidate.ParameterKinds))
                    {
                        if (bestCost == int.MaxValue) method = candidate;
                        continue;
                    }
                    var cost = 0;
                    for (var i = 0; i < Math.Min(call.Arguments.Count, candidate.ParameterKinds.Length); i++)
                        cost += HostExportArgumentFacts.ConversionCost(candidate.ParameterKinds[i], _code.GetExpressionType(call.Arguments[i]));
                    if (cost < bestCost)
                    {
                        method = candidate;
                        bestCost = cost;
                    }
                    if (cost == 0) return true;
                }
            }
            return method != null;
        }

        private bool TryGetNativeGetter(
            Expression receiver,
            string memberName,
            out HostNativeObjectDescriptor owner,
            out HostNativeMethodDescriptor getter)
        {
            owner = _code.GetNativeObjectType(receiver);
            if (owner == null || !owner.TryGetGetter(memberName, out getter) ||
                getter.TakesContext && !HasContextArgument)
            {
                getter = null;
                return false;
            }
            return true;
        }

        private bool TryGetNativeSetter(
            SetPropertyExpression expression,
            string memberName,
            out HostNativeObjectDescriptor owner,
            out HostNativeMethodDescriptor setter)
        {
            owner = _code.GetNativeObjectType(expression.Object);
            if (owner == null || !owner.TryGetSetter(memberName, out setter) ||
                setter.TakesContext && !HasContextArgument || setter.ParameterKinds.Length != 1 ||
                !HostExportArgumentFacts.CanPass(
                    setter.ParameterKinds[0],
                    setter.GetScriptParameterType(0),
                    _code.GetExpressionType(expression.Value),
                    _code.GetNativeObjectType(expression.Value)?.ClrType))
            {
                setter = null;
                return false;
            }
            return true;
        }

        private bool TryGetNativeConstruction(
            NewExpression expression,
            out HostNativeObjectDescriptor descriptor)
        {
            descriptor = null;
            var call = expression.Expression;
            if (call?.Target is not NameExpression target ||
                !_code.GetName(target).IsUnshadowedGlobal ||
                !_session.CompileSession.HostExports.TryGetNativeObject(
                    target.Identifier?.Value,
                    out var candidate) ||
                candidate.Constructor == null ||
                !CanBindNativeArguments(
                    call,
                    candidate.ConstructorParameterKinds,
                    candidate.RequiredConstructorParameterCount,
                    candidate.Constructor.GetParameters(),
                    prefix: 0))
            {
                return false;
            }

            descriptor = candidate;
            return true;
        }

        private bool CanBindNativeArguments(
            FunctionCallExpression call,
            AuroraExportValueKind[] parameterKinds,
            int requiredCount,
            System.Reflection.ParameterInfo[] clrParameters,
            int prefix,
            bool useDynamicForExtraArguments = false)
        {
            var hasParams = HostExportArgumentFacts.HasParams(parameterKinds);
            if (HasSpread(call.Arguments) || call.Arguments.Count < requiredCount ||
                !hasParams && useDynamicForExtraArguments && call.Arguments.Count > parameterKinds.Length)
            {
                return false;
            }

            var provided = hasParams ? call.Arguments.Count : Math.Min(call.Arguments.Count, parameterKinds.Length);
            for (var i = 0; i < provided; i++)
            {
                var argument = call.Arguments[i];
                HostExportArgumentFacts.GetArgumentParameter(parameterKinds, clrParameters, prefix, i,
                    out var parameterKind, out var parameterType);
                if (!HostExportArgumentFacts.CanPass(
                        parameterKind,
                        parameterType,
                        _code.GetExpressionType(argument),
                        _code.GetNativeObjectType(argument)?.ClrType))
                {
                    return false;
                }
            }
            return true;
        }

        private void EmitNativeReceiver(
            Expression expression,
            HostNativeObjectDescriptor descriptor)
        {
            if (expression is NameExpression name && !_savedOperands.ContainsKey(expression))
            {
                var binding = _code.GetName(name);
                if (binding.IsLocal &&
                    ReferenceEquals(
                        _code.GetLocalNativeObjectType(binding.Local),
                        descriptor))
                {
                    EmitLoadLocal(binding.Local);
                    return;
                }
            }

            EmitNativeObjectReference(expression, descriptor);
        }

        private void EmitNativeObjectReference(
            Expression expression,
            HostNativeObjectDescriptor descriptor)
        {
            var kind = EmitExpression(expression);
            if (kind == StackValueKind.Object &&
                ReferenceEquals(_code.GetNativeObjectType(expression), descriptor))
            {
                return;
            }
            if (kind == StackValueKind.Datum)
            {
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToObject);
            }
            else if (kind != StackValueKind.Object)
            {
                ConvertToDatum(kind);
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToObject);
            }
            _il.Emit(OpCodes.Castclass, descriptor.ClrType);
        }

        private StackValueKind EmitNativeFieldRead(
            Expression receiver,
            HostNativeObjectDescriptor owner,
            HostNativeFieldDescriptor field)
        {
            EmitNativeReceiver(receiver, owner);
            _il.Emit(OpCodes.Ldfld, field.Field);
            return GetNativeStackKind(field.Kind);
        }

        private StackValueKind EmitNativeGetter(
            Expression receiver,
            HostNativeObjectDescriptor owner,
            HostNativeMethodDescriptor getter)
        {
            EmitNativeReceiver(receiver, owner);
            if (getter.TakesContext)
            {
                _il.Emit(OpCodes.Ldarg_0);
            }
            _il.Emit(OpCodes.Callvirt, getter.Method);
            return GetNativeStackKind(getter.ReturnKind);
        }

        private bool TryEmitNativeSetterWrite(
            SetPropertyExpression expression,
            string memberName,
            out StackValueKind kind)
        {
            kind = StackValueKind.Datum;
            if (!TryGetNativeSetter(expression, memberName, out var owner, out var setter))
            {
                return false;
            }

            var receiver = DeclareLocal(owner.ClrType);
            EmitNativeReceiver(expression.Object, owner);
            _il.Emit(OpCodes.Stloc, receiver);

            var parameterType = setter.GetScriptParameterType(0);
            var value = DeclareLocal(parameterType);
            EmitHostExportArgument(expression.Value, setter.ParameterKinds[0], parameterType);
            _il.Emit(OpCodes.Stloc, value);

            _il.Emit(OpCodes.Ldloc, receiver);
            if (setter.TakesContext)
            {
                _il.Emit(OpCodes.Ldarg_0);
            }
            _il.Emit(OpCodes.Ldloc, value);
            _il.Emit(OpCodes.Callvirt, setter.Method);
            _il.Emit(OpCodes.Ldloc, value);
            kind = GetNativeStackKind(setter.ParameterKinds[0]);
            return true;
        }

        /// <summary>
        /// Writes an exported field in place. The generated dynamic setter silently
        /// drops values it cannot represent, so this path is only taken when the
        /// assigned value was already proven to match the field representation.
        /// </summary>
        private bool TryEmitNativeFieldWrite(
            SetPropertyExpression expression,
            string memberName,
            out StackValueKind kind)
        {
            kind = StackValueKind.Datum;
            if (!TryGetNativeField(expression.Object, memberName, out var owner, out var field) ||
                field.IsReadOnly)
            {
                return false;
            }
            if (TryEmitNativeFieldCompoundWrite(
                    expression,
                    memberName,
                    owner,
                    field,
                    out kind))
            {
                return true;
            }
            if (
                !HostExportArgumentFacts.CanPass(
                    field.Kind,
                    field.Field.FieldType,
                    _code.GetExpressionType(expression.Value)))
            {
                return false;
            }

            var receiverLocal = DeclareLocal(owner.ClrType);
            EmitNativeReceiver(expression.Object, owner);
            _il.Emit(OpCodes.Stloc, receiverLocal);

            var valueLocal = DeclareLocal(field.Field.FieldType);
            EmitNativeValue(expression.Value, field.Kind);
            _il.Emit(OpCodes.Stloc, valueLocal);

            _il.Emit(OpCodes.Ldloc, receiverLocal);
            _il.Emit(OpCodes.Ldloc, valueLocal);
            _il.Emit(OpCodes.Stfld, field.Field);
            _il.Emit(OpCodes.Ldloc, valueLocal);
            kind = GetNativeStackKind(field.Kind);
            return true;
        }

        private bool TryEmitNativeFieldCompoundWrite(
            SetPropertyExpression expression,
            string memberName,
            HostNativeObjectDescriptor owner,
            HostNativeFieldDescriptor field,
            out StackValueKind kind)
        {
            kind = StackValueKind.Datum;
            if (field.Kind is not (AuroraExportValueKind.Number or AuroraExportValueKind.Int32) ||
                expression.Value is not BinaryExpression binary ||
                binary.Left is not GetPropertyExpression read ||
                !ReferenceEquals(read.Object, expression.Object) ||
                !TryGetStaticPropertyName(read.Property, out var readName) ||
                !StringComparer.Ordinal.Equals(readName, memberName) ||
                !IsNativeFieldCompoundOperator(field.Kind, binary.Operator) ||
                !HostExportArgumentFacts.CanPass(
                    field.Kind,
                    field.Field.FieldType,
                    _code.GetExpressionType(binary)))
            {
                return false;
            }

            var receiver = DeclareLocal(owner.ClrType);
            EmitNativeReceiver(expression.Object, owner);
            _il.Emit(OpCodes.Stloc, receiver);

            var result = DeclareLocal(field.Field.FieldType);
            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldfld, field.Field);
            if (field.Kind == AuroraExportValueKind.Number)
            {
                EmitNumericBinaryRight(binary.Operator, binary.Right);
            }
            else
            {
                EmitNativeInt32BinaryRight(binary.Operator, binary.Right);
            }
            _il.Emit(OpCodes.Stloc, result);

            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldloc, result);
            _il.Emit(OpCodes.Stfld, field.Field);
            _il.Emit(OpCodes.Ldloc, result);
            kind = GetNativeStackKind(field.Kind);
            return true;
        }

        private StackValueKind? TryEmitNativeFieldMutation(
            UnaryExpression unary,
            GetPropertyExpression property)
        {
            if (!TryGetStaticPropertyName(property.Property, out var memberName) ||
                !TryGetNativeField(property.Object, memberName, out var owner, out var field) ||
                field.IsReadOnly ||
                field.Kind is not (AuroraExportValueKind.Number or AuroraExportValueKind.Int32))
            {
                return null;
            }

            var receiver = DeclareLocal(owner.ClrType);
            EmitNativeReceiver(property.Object, owner);
            _il.Emit(OpCodes.Stloc, receiver);

            var previous = DeclareLocal(field.Field.FieldType);
            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldfld, field.Field);
            _il.Emit(OpCodes.Stloc, previous);

            var current = DeclareLocal(field.Field.FieldType);
            _il.Emit(OpCodes.Ldloc, previous);
            if (field.Kind == AuroraExportValueKind.Int32)
            {
                _il.Emit(OpCodes.Ldc_I4_1);
            }
            else
            {
                _il.Emit(OpCodes.Ldc_R8, 1d);
            }
            _il.Emit(
                unary.Operator == Operator.PreIncrement ||
                    unary.Operator == Operator.PostIncrement
                    ? OpCodes.Add
                    : OpCodes.Sub);
            _il.Emit(OpCodes.Stloc, current);

            _il.Emit(OpCodes.Ldloc, receiver);
            _il.Emit(OpCodes.Ldloc, current);
            _il.Emit(OpCodes.Stfld, field.Field);

            var postfix = unary.Operator == Operator.PostIncrement ||
                unary.Operator == Operator.PostDecrement;
            _il.Emit(OpCodes.Ldloc, postfix ? previous : current);
            return GetNativeStackKind(field.Kind);
        }

        private static bool IsNativeFieldCompoundOperator(
            AuroraExportValueKind kind,
            Operator op)
        {
            if (kind == AuroraExportValueKind.Int32)
            {
                return op == Operator.Add || op == Operator.Subtract ||
                    op == Operator.Multiply || op == Operator.BitwiseAnd ||
                    op == Operator.BitwiseOr || op == Operator.BitwiseXor ||
                    op == Operator.LeftShift || op == Operator.SignedRightShift;
            }
            return op == Operator.Add || op == Operator.Subtract ||
                op == Operator.Multiply || op == Operator.Divide ||
                op == Operator.Modulo || op == Operator.BitwiseAnd ||
                op == Operator.BitwiseOr || op == Operator.BitwiseXor ||
                op == Operator.LeftShift || op == Operator.SignedRightShift ||
                op == Operator.UnSignedRightShift;
        }

        private void EmitNativeInt32BinaryRight(Operator op, Expression right)
        {
            EmitInt32Operand(right, truncateThroughInt64: false);
            _il.Emit(op == Operator.Add ? OpCodes.Add :
                op == Operator.Subtract ? OpCodes.Sub :
                op == Operator.Multiply ? OpCodes.Mul :
                op == Operator.BitwiseAnd ? OpCodes.And :
                op == Operator.BitwiseOr ? OpCodes.Or :
                op == Operator.BitwiseXor ? OpCodes.Xor :
                op == Operator.LeftShift ? OpCodes.Shl :
                op == Operator.SignedRightShift ? OpCodes.Shr :
                throw new NotSupportedException(
                    "Unsupported native Int32 field compound operator."));
        }

        private StackValueKind EmitNativeMethodCall(
            FunctionCallExpression call,
            Expression receiver,
            HostNativeObjectDescriptor owner,
            HostNativeMethodDescriptor method,
            bool materializeVoid = true)
        {
            EmitNativeReceiver(receiver, owner);
            if (method.TakesContext)
            {
                _il.Emit(OpCodes.Ldarg_0);
            }

            EmitNativeArguments(
                call,
                method.ParameterKinds,
                method.Method.GetParameters(),
                method.TakesContext ? 1 : 0);
            _il.Emit(OpCodes.Callvirt, method.Method);

            if (method.ReturnKind == AuroraExportValueKind.Void)
            {
                if (!materializeVoid) return StackValueKind.Void;
                EmitNull();
                return StackValueKind.Datum;
            }
            if (method.ReturnKind == AuroraExportValueKind.Object)
            {
                return StackValueKind.Object;
            }
            return GetNativeStackKind(method.ReturnKind);
        }

        private StackValueKind EmitNativeConstruction(
            NewExpression expression,
            HostNativeObjectDescriptor descriptor)
        {
            var call = expression.Expression;
            EmitNativeArguments(
                call,
                descriptor.ConstructorParameterKinds,
                descriptor.Constructor.GetParameters(),
                prefix: 0);
            _il.Emit(OpCodes.Newobj, descriptor.Constructor);
            return StackValueKind.Object;
        }

        private void EmitNativeArguments(
            FunctionCallExpression call,
            AuroraExportValueKind[] parameterKinds,
            System.Reflection.ParameterInfo[] clrParameters,
            int prefix)
        {
            var hasParams = HostExportArgumentFacts.HasParams(parameterKinds);
            var fixedCount = parameterKinds.Length - (hasParams ? 1 : 0);
            for (var i = 0; i < fixedCount; i++)
            {
                if (i < call.Arguments.Count)
                {
                    EmitHostExportArgument(
                        call.Arguments[i],
                        parameterKinds[i],
                        clrParameters[prefix + i].ParameterType);
                }
                else
                {
                    EmitHostExportDefault(clrParameters[prefix + i]);
                }
            }

            if (hasParams)
            {
                var elementType = clrParameters[prefix + fixedCount].ParameterType.GetElementType();
                var elementKind = HostExportArgumentFacts.ParamsElementKind(parameterKinds[fixedCount]);
                var count = Math.Max(0, call.Arguments.Count - fixedCount);
                if (count == 0)
                    _il.Emit(OpCodes.Call, typeof(Array).GetMethod(nameof(Array.Empty)).MakeGenericMethod(elementType));
                else
                {
                    EmitInt32(count);
                    _il.Emit(OpCodes.Newarr, elementType);
                    for (var i = 0; i < count; i++)
                    {
                        _il.Emit(OpCodes.Dup);
                        EmitInt32(i);
                        EmitHostExportArgument(call.Arguments[fixedCount + i], elementKind, elementType);
                        _il.Emit(OpCodes.Stelem, elementType);
                    }
                }
                return;
            }

            // Script calls evaluate surplus arguments before invoking the target.
            for (var i = parameterKinds.Length; i < call.Arguments.Count; i++)
            {
                EmitExpressionDiscarded(call.Arguments[i]);
            }
        }

        private void EmitNativeValue(Expression expression, AuroraExportValueKind kind)
        {
            switch (kind)
            {
                case AuroraExportValueKind.Number:
                    EmitNumber(expression);
                    return;
                case AuroraExportValueKind.Int32:
                    EmitInt32Value(expression);
                    return;
                case AuroraExportValueKind.Boolean:
                    EmitCondition(expression);
                    return;
                case AuroraExportValueKind.String:
                    EmitString(expression);
                    return;
                default:
                    throw new NotSupportedException(
                        "Unsupported native object field representation.");
            }
        }

        private static StackValueKind GetNativeStackKind(AuroraExportValueKind kind)
        {
            return kind switch
            {
                AuroraExportValueKind.Number => StackValueKind.Number,
                AuroraExportValueKind.Int32 => StackValueKind.Int32,
                AuroraExportValueKind.Int64 => StackValueKind.Int64,
                AuroraExportValueKind.UInt64 => StackValueKind.UInt64,
                AuroraExportValueKind.Boolean => StackValueKind.Boolean,
                AuroraExportValueKind.String => StackValueKind.String,
                AuroraExportValueKind.Object => StackValueKind.Object,
                _ => StackValueKind.Datum
            };
        }
    }
}
