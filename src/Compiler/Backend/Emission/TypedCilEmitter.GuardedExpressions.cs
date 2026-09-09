using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed partial class TypedCilEmitter
    {
        private readonly Dictionary<Expression, LocalBuilder> _savedOperands =
            new(ReferenceEqualityComparer.Instance);
        private readonly HashSet<Expression> _guardedRoots = new(ReferenceEqualityComparer.Instance);

        private bool TryEmitSavedOperand(Expression expression, out StackValueKind kind)
        {
            kind = default;
            if (expression == null || !_savedOperands.TryGetValue(expression, out var local)) return false;
            _il.Emit(OpCodes.Ldloc, local);
            var type = _code.GetExpressionType(expression);
            kind = IsProvenValueType(type) && type != FlowValueType.Null
                ? EmitCheckedDatumConversion(type) : StackValueKind.Datum;
            return true;
        }

        private bool TryEmitGuardedExpression(Expression expression, out StackValueKind kind)
        {
            kind = default;
            if (_code.Prediction is not { } prediction ||
                prediction.Types == null && prediction.NativeTypes == null || _guardedRoots.Contains(expression)) return false;
            var operands = GetGuardOperands(expression);
            if (operands == null) return false;
            var guards = new Dictionary<Expression, FlowValueType>(ReferenceEqualityComparer.Instance);
            var nativeGuards = new Dictionary<Expression, HostNativeObjectDescriptor>(ReferenceEqualityComparer.Instance);
            foreach (var operand in operands)
            {
                // Proven primitive/storage facts outrank a prediction. In
                // particular, do not widen an existing UInt32 native kernel
                // back to Number and introduce datum checks into its ABI.
                var actualType = _code.GetExpressionType(operand);
                if (IsProvenValueType(actualType) || _code.GetNativeObjectType(operand) != null) continue;
                var type = prediction.GetExpressionType(operand, actualType);
                if (prediction.GetNativeObjectType(operand) is { } native)
                {
                    nativeGuards[operand] = native;
                    guards[operand] = FlowValueType.Object;
                    continue;
                }
                if (type != _code.GetExpressionType(operand) && CanGuardType(type))
                    guards[operand] = type;
            }
            if (guards.Count == 0) return false;

            var original = _code;
            var guardedCode = original.WithGuardedTypes(guards, nativeGuards);
            if (expression is SetPropertyExpression writeProperty && guardedCode.GetNativeObjectType(writeProperty.Object) == null ||
                expression is AssignmentExpression { Left: GetPropertyExpression writeTarget } &&
                    guardedCode.GetNativeObjectType(writeTarget.Object) == null) return false;
            if (expression is GetPropertyExpression readProperty &&
                guardedCode.GetNativeObjectType(readProperty.Object) == null &&
                TryGetStaticPropertyName(readProperty.Property, out var propertyName) &&
                !(FlowValueTypeFacts.IsPackedArray(guardedCode.GetExpressionType(readProperty.Object)) && propertyName == "length") &&
                !(_session.CompileSession.HostExports.TryGetNativeValue(guardedCode.GetExpressionType(readProperty.Object), out var valueOwner) &&
                    valueOwner.GetValueGetter(propertyName) != null)) return false;
            if (expression is GetElementExpression get && !CanImproveElementAccess(get.Object, get.Index, guardedCode) ||
                expression is SetElementExpression set && !CanImproveElementAccess(set.Object, set.Index, guardedCode) ||
                expression is AssignmentExpression { Left: GetElementExpression target } &&
                    !CanImproveElementAccess(target.Object, target.Index, guardedCode)) return false;
            if (expression is FunctionCallExpression { Target: GetPropertyExpression property } call &&
                TryGetStaticPropertyName(property.Property, out var memberName))
            {
                var ordinaryTarget = GetGuardCallTarget(call, property.Object, memberName);
                object guardedTarget;
                _code = guardedCode;
                try { guardedTarget = GetGuardCallTarget(call, property.Object, memberName); }
                finally { _code = original; }
                if (ReferenceEquals(ordinaryTarget, guardedTarget)) return false;
            }
            var saved = new List<(Expression Expression, LocalBuilder Previous)>();
            _guardedRoots.Add(expression);
            try
            {
                // Capture operands in script evaluation order, before selecting
                // either branch. A failed guard never repeats calls, getters or
                // argument evaluation and never assumes the old function identity.
                foreach (var operand in operands)
                {
                    _savedOperands.TryGetValue(operand, out var previous);
                    var local = DeclareLocal(typeof(ScriptDatum));
                    EmitDatum(operand);
                    _il.Emit(OpCodes.Stloc, local);
                    saved.Add((operand, previous));
                    _savedOperands[operand] = local;
                }

                var fallback = _il.DefineLabel();
                var done = _il.DefineLabel();
                foreach (var guard in guards)
                {
                    if (nativeGuards.TryGetValue(guard.Key, out var native))
                        EmitObjectTypeGuard(_savedOperands[guard.Key], native.ClrType, fallback);
                    else EmitTypeGuard(_savedOperands[guard.Key], guard.Value, fallback);
                }

                // Only the immediate guarded operands and this operation are
                // refined. In particular, speculative local storage/shape facts
                // and facts about descendants cannot leak into the fast path.
                if (expression is BinaryExpression binary)
                    guards[expression] = TypedFunctionBuilder.GetGuardedBinaryType(binary,
                        operand => guards.TryGetValue(operand, out var guarded) ? guarded : original.GetExpressionType(operand));
                else if (expression is UnaryExpression unary && guards.ContainsKey(unary.Expression))
                    guards[expression] = prediction.GetExpressionType(expression, original.GetExpressionType(expression));
                _code = guardedCode;
                var fastKind = EmitExpression(expression, materializeVoid: true);
                ConvertToDatum(fastKind);
                _il.Emit(OpCodes.Br, done);
                _il.MarkLabel(fallback);
                _code = original;
                var slowKind = EmitExpression(expression, materializeVoid: true);
                ConvertToDatum(slowKind);
                _il.MarkLabel(done);
                kind = StackValueKind.Datum;
                return true;
            }
            finally
            {
                _code = original;
                _guardedRoots.Remove(expression);
                foreach (var item in saved)
                {
                    if (item.Previous == null) _savedOperands.Remove(item.Expression);
                    else _savedOperands[item.Expression] = item.Previous;
                }
            }
        }

        private bool CanImproveElementAccess(Expression receiver, Expression index, TypedFunctionCode guardedCode) =>
            FlowValueTypeFacts.IsNumeric(guardedCode.GetExpressionType(index)) &&
                (FlowValueTypeFacts.IsPackedArray(guardedCode.GetExpressionType(receiver)) ||
                    guardedCode.GetNativeObjectType(receiver) is { IndexGetter: not null, IndexSetter: not null }) ||
            (_code.GetExpressionType(index) == FlowValueType.Int32) !=
                (guardedCode.GetExpressionType(index) == FlowValueType.Int32) ||
            FlowValueTypeFacts.IsNumberCompatible(_code.GetExpressionType(index)) !=
                FlowValueTypeFacts.IsNumberCompatible(guardedCode.GetExpressionType(index));

        private object GetGuardCallTarget(FunctionCallExpression call, Expression receiver, string name)
        {
            if (TryGetNativeMethodCall(call, receiver, name, out _, out var native)) return native;
            if (TryGetHostExportCall(call, receiver, name, out var host)) return host;
            var value = GetNativeValueCall(call, receiver, name);
            return value != null && (!value.TakesContext || HasContextArgument) ? value : null;
        }

        private IReadOnlyList<Expression> GetGuardOperands(Expression expression)
        {
            switch (expression)
            {
                case BinaryExpression binary when binary.Operator != Operator.LogicalAnd &&
                    binary.Operator != Operator.LogicalOr:
                    return new[] { binary.Left, binary.Right };
                case UnaryExpression unary when unary.Operator == Operator.Negate ||
                    unary.Operator == Operator.LogicalNot || unary.Operator == Operator.BitwiseNot:
                    return new[] { unary.Expression };
                case GetPropertyExpression property:
                    return new[] { property.Object };
                case GetElementExpression element:
                    return new[] { element.Object, element.Index };
                case SetPropertyExpression property:
                    return new[] { property.Object, property.Value };
                case SetElementExpression element:
                    return new[] { element.Object, element.Index, element.Value };
                case AssignmentExpression { Left: GetPropertyExpression property } assignment:
                    return new[] { property.Object, assignment.Right };
                case AssignmentExpression { Left: GetElementExpression element } assignment:
                    return new[] { element.Object, element.Index, assignment.Right };
                case FunctionCallExpression call when call.Target is GetPropertyExpression property &&
                    !HasSpread(call.Arguments):
                    var operands = new List<Expression>(call.Arguments.Count + 1) { property.Object };
                    operands.AddRange(call.Arguments);
                    return operands;
                case FunctionCallExpression call when call.Target is NameExpression name &&
                    _code.GetName(name).DirectFunction.IsValid && !HasSpread(call.Arguments):
                    var nativeOperands = new List<Expression>(call.Arguments.Count + 1) { call.Target };
                    nativeOperands.AddRange(call.Arguments);
                    return nativeOperands;
                case NewExpression creation when !HasSpread(creation.Expression.Arguments):
                    var constructorOperands = new List<Expression>(creation.Expression.Arguments.Count + 1)
                        { creation.Expression.Target };
                    constructorOperands.AddRange(creation.Expression.Arguments);
                    return constructorOperands;
                default:
                    return null;
            }
        }

        private static bool CanGuardType(FlowValueType type)
        {
            return type is FlowValueType.Null or FlowValueType.Number or FlowValueType.String or
                FlowValueType.Boolean or FlowValueType.Int64 or FlowValueType.UInt64 ||
                FlowValueTypeFacts.IsPackedArray(type);
        }

        private static bool IsProvenValueType(FlowValueType type)
        {
            return CanGuardType(type) || type is FlowValueType.Int32 or FlowValueType.UInt32;
        }

        private void EmitTypeGuard(LocalBuilder value, FlowValueType type, Label fallback)
        {
            if (FlowValueTypeFacts.IsPackedArray(type))
            {
                EmitObjectTypeGuard(value, TypedRuntimeMetadata.PackedArray(type).Items.DeclaringType, fallback);
                return;
            }
            var expected = type switch
            {
                FlowValueType.Null => ValueKind.Null,
                FlowValueType.Number => ValueKind.Number,
                FlowValueType.String => ValueKind.String,
                FlowValueType.Boolean => ValueKind.Boolean,
                FlowValueType.Int64 => ValueKind.Int64,
                FlowValueType.UInt64 => ValueKind.UInt64,
                _ => throw new InvalidOperationException("Unsupported guarded type.")
            };
            _il.Emit(OpCodes.Ldloca, value);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumKind);
            EmitInt32((int)expected);
            _il.Emit(OpCodes.Bne_Un, fallback);
        }

        private void EmitObjectTypeGuard(LocalBuilder value, Type expected, Label fallback)
        {
            _il.Emit(OpCodes.Ldloc, value);
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.DatumToObject);
            _il.Emit(OpCodes.Isinst, expected);
            _il.Emit(OpCodes.Brfalse, fallback);
        }
    }
}
