using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using AuroraScript.Runtime.Types;

namespace AuroraScript.Compiler.Backend.Emission
{
    internal sealed partial class TypedCilEmitter
    {
        private readonly Dictionary<TypeDeclaration, MethodInfo> _shapeChecks = new();
        private readonly Dictionary<int, LocalBuilder> _shapeProofs = new();
        private LocalBuilder _lastShapeProof;

        private void EmitStructuralCheck(TypeDeclaration shape)
        {
            _shapeProofs.Clear();
            var value = DeclareLocal(typeof(ScriptDatum));
            _il.Emit(OpCodes.Stloc, value);
            var pure = DeclareLocal(typeof(bool));
            _il.Emit(OpCodes.Ldc_I4_1);
            _il.Emit(OpCodes.Stloc, pure);
            _il.Emit(OpCodes.Ldarg_0);
            _il.Emit(OpCodes.Ldloc, value);
            _il.Emit(OpCodes.Ldnull);
            _il.Emit(OpCodes.Ldloca, pure);
            _il.Emit(OpCodes.Call, GetShapeCheck(shape));
            _lastShapeProof = pure;
        }

        private MethodInfo GetShapeCheck(TypeDeclaration shape)
        {
            if (_shapeChecks.TryGetValue(shape, out var method)) return method;
            var visitedType = typeof(HashSet<(ScriptObject, string)>);
            var definition = _session.Builder.DefineMethod(_module.Source.FullPath,
                shape.Name.Value + "$check" + _shapeChecks.Count, typeof(ScriptDatum),
                [typeof(ScriptContext), typeof(ScriptDatum), visitedType, typeof(bool).MakeByRefType()]);
            _shapeChecks.Add(shape, definition.Method);
            var previous = _il;
            _il = definition.IL;
            try
            {
                var done = _il.DefineLabel();
                _il.Emit(OpCodes.Ldarg_1);
                _session.Builder.LoadStringConstant(_il, shape.Range.FileName + ":" + shape.Name.Value);
                _il.Emit(OpCodes.Ldarga, 2);
                _il.Emit(IsRecursiveShape(shape, new HashSet<TypeDeclaration>()) ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                _il.Emit(OpCodes.Call, typeof(TypeCheckOps).GetMethod(nameof(TypeCheckOps.EnterStructure)));
                _il.Emit(OpCodes.Brfalse, done);
                var scope = shape.Parent as ModuleDeclaration ?? _module.Declaration;
                foreach (var field in shape.Fields)
                {
                    _il.Emit(OpCodes.Ldarg_1);
                    _il.Emit(OpCodes.Ldarg_0);
                    _session.Builder.LoadStringConstant(_il, field.Name.Value);
                    _il.Emit(OpCodes.Ldarg_3);
                    _il.Emit(OpCodes.Call, typeof(TypeCheckOps).GetMethod(nameof(TypeCheckOps.ReadStructuralField)));
                    if (TypeReferenceFacts.TryGetCustomType(scope, field.Type, out var nested))
                    {
                        var item = _il.DeclareLocal(typeof(ScriptDatum));
                        _il.Emit(OpCodes.Stloc, item);
                        _il.Emit(OpCodes.Ldarg_0);
                        _il.Emit(OpCodes.Ldloc, item);
                        _il.Emit(OpCodes.Ldarg_2);
                        _il.Emit(OpCodes.Ldarg_3);
                        _il.Emit(OpCodes.Call, GetShapeCheck(nested));
                    }
                    else EmitDatumBoundary(field.Type, scope);
                    _il.Emit(OpCodes.Pop);
                }
                _il.MarkLabel(done);
                _il.Emit(OpCodes.Ldarg_1);
                _il.Emit(OpCodes.Ret);
            }
            finally { _il = previous; }
            return definition.Method;
        }

        private bool IsRecursiveShape(TypeDeclaration shape, HashSet<TypeDeclaration> path)
        {
            if (!path.Add(shape)) return true;
            var scope = shape.Parent as ModuleDeclaration ?? _module.Declaration;
            foreach (var field in shape.Fields)
                if (TypeReferenceFacts.TryGetCustomType(scope, field.Type, out var nested) &&
                    IsRecursiveShape(nested, path)) return true;
            path.Remove(shape);
            return false;
        }

        private TypeReference GetStructuralFieldType(GetPropertyExpression property)
        {
            return TryGetStaticPropertyName(property.Property, out var name)
                ? GetStructuralFieldType(property.Object, name) : null;
        }

        private TypeReference GetStructuralFieldType(Expression owner, string name)
        {
            var shape = _code.GetStructuralType(owner);
            if (shape == null) return null;
            foreach (var field in shape.Fields)
                if (field.Name.Value == name) return field.Type;
            return null;
        }

        private void EmitPropertyValue(SetPropertyExpression property)
        {
            var target = TryGetStaticPropertyName(property.Property, out var name)
                ? GetStructuralFieldType(property.Object, name) : null;
            if (target == null) EmitDatum(property.Value);
            else ConvertToDatum(EmitBoundary(property.Value, target));
        }

        private LocalBuilder GetShapeProof(Expression owner)
        {
            while (owner is GroupExpression group && group.Expressions.Count == 1) owner = group.Expression;
            return owner is NameExpression name && _code.GetName(name) is var binding &&
                binding.IsLocal && _shapeProofs.TryGetValue(binding.Local.Value, out var proof) ? proof : null;
        }
        private TypeReference GetBindingContract(BoundName binding)
        {
            var function = _function;
            var local = binding.Local;
            var upvalue = binding.Upvalue;
            while (!local.IsValid && upvalue.IsValid)
            {
                var source = function.UpvalueSlots[upvalue.Value];
                function = _module.Functions[_module.GetFunctionIndex(source.SourceFunction)];
                local = source.SourceLocal;
                upvalue = source.SourceUpvalue;
            }
            return local.IsValid && function.LocalSlots[local.Value].Declaration is ParameterDeclaration parameter
                ? parameter.DeclaredType : null;
        }

        private void ValidateCallBoundary(FunctionCallExpression call)
        {
            FunctionPlan target = null;
            if (call.Target is NameExpression name && _code.GetName(name).DirectFunction is var id && id.IsValid)
                target = _module.Functions[_module.GetFunctionIndex(id)];
            else _function.ImportedNativeCalls.TryGetValue(call, out target);
            if (target == null && call.Target is NameExpression moduleName &&
                _code.GetName(moduleName).ModuleSymbol.IsValid)
                foreach (var candidate in _module.Functions)
                    if (candidate.Name == moduleName.Identifier.Value) { target = candidate; break; }
            if (target == null || HasSpread(call.Arguments)) return;
            for (var i = 0; i < Math.Min(call.Arguments.Count, target.Declaration.Parameters.Count); i++)
                ValidateBoundary(call.Arguments[i], target.Declaration.Parameters[i].DeclaredType);
        }

        private void ValidateBoundary(Expression value, TypeReference target)
        {
            if (target == null || value == null) return;
            var expected = TypeReferenceFacts.GetFlowType(_module.Declaration, target, _session.CompileSession.HostExports);
            var actual = _code.GetExpressionType(value);
            if (actual == FlowValueType.Dynamic || actual == FlowValueType.None) return;
            var compatible = expected == actual ||
                FlowValueTypeFacts.IsNumberCompatible(expected) && FlowValueTypeFacts.IsNumberCompatible(actual) ||
                expected is FlowValueType.Int64 or FlowValueType.UInt64 && FlowValueTypeFacts.IsNumeric(actual) ||
                actual == FlowValueType.Null && IsNullableContract(target) ||
                expected == FlowValueType.Object && !FlowValueTypeFacts.IsNumeric(actual) && actual != FlowValueType.Boolean && actual != FlowValueType.String;
            // A union is an uncertain value, not a statically known mismatch.
            var bits = (uint)actual;
            if (!compatible && (bits & (bits - 1)) == 0)
                throw new AuroraCompilationException(AuroraCompilationStage.Emission, value,
                    $"Type mismatch: expected {target.DisplayName}, actual {actual}.");
            if (TypedDocumentLiteralConstants.TryGetNumber(value, out var number) &&
                (expected == FlowValueType.Int32 && !TypeCheckOps.IsInt32(number) ||
                 expected == FlowValueType.UInt32 && !TypeCheckOps.IsUInt32(number)))
                throw new AuroraCompilationException(AuroraCompilationStage.Emission, value,
                    $"Value does not satisfy {target.DisplayName}.");
        }

        private bool IsNullableContract(TypeReference type)
        {
            var flow = TypeReferenceFacts.GetFlowType(_module.Declaration, type, _session.CompileSession.HostExports);
            return flow is FlowValueType.Object or FlowValueType.String or FlowValueType.Null ||
                FlowValueTypeFacts.IsPackedArray(flow);
        }

        // Consumes and returns a Datum. Used only where native proof was lost.
        private void EmitDatumBoundary(TypeReference target, ModuleDeclaration scope = null)
        {
            if (target == null) return;
            scope ??= _module.Declaration;
            if (FlowValueTypeFacts.IsCheckedTypeName(target.Name))
            {
                _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetTypeCheck(FlowValueTypeFacts.GetCheckedType(target.Name)));
                return;
            }
            if (TypeReferenceFacts.TryGetCustomType(scope, target, out var shape))
            {
                EmitStructuralCheck(shape);
                return;
            }
            if (TypeReferenceFacts.TryGetFunctionType(scope, target, out var callable))
            {
                if (callable.IsStrong)
                    _il.Emit(OpCodes.Call, typeof(TypeCheckOps).GetMethod(nameof(TypeCheckOps.CheckCallable)));
                return;
            }
            if (TypeReferenceFacts.TryGetNativeObject(_session.CompileSession.HostExports, target, out var native))
            {
                _il.Emit(OpCodes.Call, typeof(TypeCheckOps).GetMethod(nameof(TypeCheckOps.CheckNativeObject)).MakeGenericMethod(native.ClrType));
                return;
            }
            if (TypeReferenceFacts.TryGetClrType(_session.CompileSession.HostExports, target, out var clr))
            {
                _il.Emit(OpCodes.Call, typeof(ClrDirectOps).GetMethod(nameof(ClrDirectOps.CheckInstance)).MakeGenericMethod(clr));
                return;
            }
            _il.Emit(OpCodes.Call, TypedRuntimeMetadata.GetTypeCheck(FlowValueTypeFacts.GetCheckedType(target.Name)));
        }

        private StackValueKind EmitBoundary(Expression value, TypeReference target)
        {
            ValidateBoundary(value, target);
            var type = FlowValueTypeFacts.FromCheckedTypeName(target.Name);
            if (value != null && TryEmitProvenCheck(value, type, out var proven)) return proven;
            EmitDatumOrNull(value);
            return EmitDatumBoundaryValue(target);
        }

        private StackValueKind EmitDatumBoundaryValue(TypeReference target)
        {
            var type = FlowValueTypeFacts.FromCheckedTypeName(target.Name);
            if (type is FlowValueType.Int32 or FlowValueType.UInt32 or FlowValueType.Int64 or FlowValueType.UInt64)
            {
                _il.Emit(OpCodes.Call, typeof(TypeCheckOps).GetMethod("Check" + type + "Value"));
                return type switch
                {
                    FlowValueType.Int32 => StackValueKind.Int32,
                    FlowValueType.UInt32 => StackValueKind.UInt32,
                    FlowValueType.Int64 => StackValueKind.Int64,
                    _ => StackValueKind.UInt64
                };
            }
            EmitDatumBoundary(target);
            return type == FlowValueType.None ? StackValueKind.Datum : EmitCheckedDatumConversion(type);
        }

        // Only the final operation crosses an int32 contract. Do not push checked
        // arithmetic into a Number expression: an intermediate overflow may cancel.
        private bool TryEmitInt32BoundaryArithmetic(Expression value)
        {
            while (value is GroupExpression group && group.Expressions.Count == 1) value = group.Expression;
            var (left, right, op) = value switch
            {
                BinaryExpression binary => (binary.Left, binary.Right, binary.Operator),
                CompoundExpression compound => (compound.Left, compound.Right, compound.Operator.SimplerOperator),
                _ => (null, null, null)
            };
            if ((op != Operator.Add && op != Operator.Subtract) ||
                _code.GetExpressionType(left) != FlowValueType.Int32 ||
                _code.GetExpressionType(right) != FlowValueType.Int32) return false;
            EmitInt32Value(left);
            EmitInt32Value(right);
            _il.Emit(op == Operator.Add ? OpCodes.Add_Ovf : OpCodes.Sub_Ovf);
            return true;
        }

        private void ConvertBoundaryToLocal(LocalSlotId slot, StackValueKind kind)
        {
            if (TryGetCapturedIndex(slot, out _))
            {
                ConvertToDatum(kind);
                return;
            }
            switch (_code.GetLocalType(slot))
            {
                case FlowValueType.Int32: ConvertStackToInt32(kind, false); break;
                case FlowValueType.UInt32: ConvertStackToUInt32(kind); break;
                case FlowValueType.Int64: ConvertStackToInt64(kind); break;
                case FlowValueType.UInt64: ConvertStackToUInt64(kind); break;
                case FlowValueType.Number:
                    if (_code.UsesWideIntegerStorage(slot)) ConvertStackToIntegerNumber(kind);
                    else ConvertStackToNumber(kind);
                    break;
                case FlowValueType.Boolean: ConvertStackToBoolean(kind); break;
                case FlowValueType.String:
                    if (kind != StackValueKind.String)
                    {
                        ConvertToDatum(kind);
                        _il.Emit(OpCodes.Call, typeof(TypeCheckOps).GetMethod(nameof(TypeCheckOps.GetStringValue)));
                    }
                    break;
                default:
                    if (!FlowValueTypeFacts.IsPackedArray(_code.GetLocalType(slot)))
                    {
                        ConvertToDatum(kind);
                        if (_locals[slot.Value].LocalType != typeof(ScriptDatum))
                        {
                            _il.Emit(OpCodes.Call, typeof(TypeCheckOps).GetMethod(nameof(TypeCheckOps.GetNullableNativeObject))
                                .MakeGenericMethod(_locals[slot.Value].LocalType));
                        }
                    }
                    break;
            }
        }

        private bool TryEmitContractCompound(CompoundExpression expression, BoundName binding, out StackValueKind kind)
        {
            kind = StackValueKind.Datum;
            var contract = GetBindingContract(binding);
            if (contract == null) return false;
            ValidateBoundary(expression, contract);
            var expected = FlowValueTypeFacts.FromCheckedTypeName(contract.Name);
            var right = _code.GetExpressionType(expression.Right);
            if (binding.IsLocal && !TryGetCapturedIndex(binding.Local, out _) &&
                expected is FlowValueType.Number or FlowValueType.Int32 &&
                _code.GetLocalType(binding.Local) == FlowValueType.Int32 &&
                IsBitwise(expression.Operator.SimplerOperator) &&
                expression.Operator.SimplerOperator != Operator.UnSignedRightShift &&
                FlowValueTypeFacts.IsNumberCompatible(right)) return false;
            if (binding.IsLocal && !TryGetCapturedIndex(binding.Local, out _) &&
                expected is FlowValueType.Int64 or FlowValueType.UInt64 && right == expected)
                return false;
            if (binding.IsLocal && !TryGetCapturedIndex(binding.Local, out _) &&
                FlowValueTypeFacts.IsNumberCompatible(right) &&
                expected is FlowValueType.Number or FlowValueType.Int32 or FlowValueType.UInt32)
            {
                if (expected == FlowValueType.Int32 && TryEmitInt32BoundaryArithmetic(expression))
                {
                    kind = StackValueKind.Int32;
                    _il.Emit(OpCodes.Dup);
                    EmitStoreLocalFromStack(binding.Local);
                    return true;
                }
                EmitNumericBinary(expression.Operator.SimplerOperator, expression.Left, expression.Right);
                kind = StackValueKind.Number;
                if (expected == FlowValueType.Int32)
                {
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.CheckInt32Number);
                    kind = StackValueKind.Int32;
                }
                else if (expected == FlowValueType.UInt32)
                {
                    _il.Emit(OpCodes.Call, TypedRuntimeMetadata.CheckUInt32Number);
                    kind = StackValueKind.UInt32;
                }
                _il.Emit(OpCodes.Dup);
                EmitStoreLocalFromStack(binding.Local);
                return true;
            }
            var result = DeclareLocal(typeof(ScriptDatum));
            EmitDatum(expression.Left);
            EmitDatum(expression.Right);
            _il.Emit(OpCodes.Call, GetDynamicBinary(expression.Operator.SimplerOperator));
            _il.Emit(OpCodes.Stloc, result);
            EmitStoreBoundName(binding, result);
            _il.Emit(OpCodes.Ldloc, result);
            return true;
        }
    }
}
