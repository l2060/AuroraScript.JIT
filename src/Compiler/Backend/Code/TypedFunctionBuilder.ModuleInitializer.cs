using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Tokens;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Code
{
    internal static partial class TypedFunctionBuilder
    {
        private sealed partial class TypeAnalyzer
        {
            private Dictionary<SymbolId, ModuleValue> _moduleValues;
            private long _moduleValueEpoch;
            private Dictionary<NameExpression, VariableDeclaration> _moduleCachedReads;

            private readonly record struct ModuleValue(
                FlowValueType Type, TypeDeclaration StructuralType,
                HostNativeObjectDescriptor NativeType, bool IsConst, long Epoch,
                VariableDeclaration CacheDeclaration = null);

            private bool TryGetModuleValue(NameExpression name, out ModuleValue value)
            {
                value = default;
                if (_moduleValues == null || !_names.TryGetValue(name, out var binding) ||
                    !binding.ModuleSymbol.IsValid || !_moduleValues.TryGetValue(binding.ModuleSymbol, out value))
                    return false;
                if (value.Epoch == _moduleValueEpoch) return true;
                if (!value.IsConst) return false;
                value = value with { StructuralType = null, CacheDeclaration = null };
                return true;
            }

            private void RecordModuleVariable(VariableDeclaration variable)
            {
                if (variable.IsDeclare) return;
                var type = variable.Initializer == null ? FlowValueType.Null : _expressionTypes[variable.Initializer];
                if (variable.Name == null || !_module.TryGetSymbol(variable.Name.Value, out var symbol)) return;
                TypeDeclaration structural = null;
                HostNativeObjectDescriptor native = null;
                if (variable.Initializer != null)
                {
                    _structuralTypes.TryGetValue(variable.Initializer, out structural);
                    _nativeObjectTypes.TryGetValue(variable.Initializer, out native);
                }
                _moduleValues ??= new Dictionary<SymbolId, ModuleValue>();
                _moduleValues[symbol] = new ModuleValue(type, structural, native, variable.IsConst, _moduleValueEpoch, variable);
            }

            private void WriteModuleTarget(Expression target, FlowValueType type,
                TypeDeclaration structural, HostNativeObjectDescriptor native)
            {
                if (!_function.IsModuleInitializer || target is not NameExpression name ||
                    !_names.TryGetValue(name, out var binding) || !binding.ModuleSymbol.IsValid) return;
                _moduleValues ??= new Dictionary<SymbolId, ModuleValue>();
                // A write may sit behind a short-circuit operator. Retain both outcomes.
                if (!TryGetModuleValue(name, out var previous))
                    previous = new ModuleValue(FlowValueType.Dynamic, null, null, false, _moduleValueEpoch);
                _moduleValues[binding.ModuleSymbol] = new ModuleValue(
                    FlowValueTypeFacts.Merge(previous.Type, type),
                    ReferenceEquals(previous.StructuralType, structural) ? structural : null,
                    ReferenceEquals(previous.NativeType, native) ? native : null,
                    previous.IsConst, _moduleValueEpoch, previous.CacheDeclaration);
            }

            private void InvalidateModuleValuesAfter(Expression expression)
            {
                if (_moduleValues != null && ModuleExpressionMayInvoke(expression)) InvalidateModuleValues();
            }

            private void InvalidateModuleCoercion(Expression expression)
            {
                if (_moduleValues != null && !IsModulePrimitive(expression)) InvalidateModuleValues();
            }

            private void InvalidateModuleValues()
            {
                // Module storage is observable by callbacks and other script functions.
                // Lazy invalidation avoids scanning every module binding at each call.
                _moduleValueEpoch++;
            }

            private bool IsModulePrimitive(Expression expression)
            {
                return _expressionTypes.TryGetValue(expression, out var type) && OperationEffects.IsPrimitive(type);
            }

            private bool ModuleExpressionMayInvoke(Expression expression)
            {
                if (expression is GetElementExpression read)
                    return !IsNonInvokingIndex(read.Object, read.Index);
                if (expression is SetElementExpression write)
                    return !IsNonInvokingIndex(write.Object, write.Index);
                if (expression is FunctionCallExpression or NewExpression or SpreadExpression or
                    SetPropertyExpression) return true;
                // Implicit coercion may run host code just like an explicit call.
                if (expression is BinaryExpression binary)
                    return !IsModulePrimitive(binary.Left) || !IsModulePrimitive(binary.Right);
                if (expression is CompoundExpression compound)
                    return !IsModulePrimitive(compound.Left) || !IsModulePrimitive(compound.Right);
                if (expression is UnaryExpression unary)
                {
                    // ChangeByOne only converts datum primitives; it never calls user
                    // conversion hooks. Only the target getter/setter can invoke code.
                    if (IsMutation(unary.Operator))
                        return unary.Expression switch
                        {
                            GetElementExpression element => !IsNonInvokingIndex(element.Object, element.Index),
                            NameExpression name => !(_names.TryGetValue(name, out var targetBinding) &&
                                (targetBinding.IsLocal || TryGetModuleValue(name, out _))),
                            _ => true
                        };
                    return unary.Operator != Operator.TypeOf && !IsModulePrimitive(unary.Expression);
                }
                if (expression is TemplateStringExpression template)
                {
                    foreach (var part in template.Parts)
                        if (!part.IsLiteral && !IsModulePrimitive(part.Expression)) return true;
                }
                if (expression is not GetPropertyExpression property)
                    return expression is not (LiteralExpression or NameExpression or GroupExpression or
                        AssignmentExpression or ArrayLiteralExpression or MapExpression or
                        MapKeyValueExpression or LambdaExpression);
                if (_nativeObjectTypes.TryGetValue(property.Object, out var receiver) &&
                    TryGetStaticPropertyName(property.Property, out var propertyName) &&
                    receiver.TryGetField(propertyName, out _)) return false;
                // Resolving a generated export member does not execute a script getter.
                if (property.Object is NameExpression owner &&
                    _names.TryGetValue(owner, out var binding) &&
                    TryGetStaticPropertyName(property.Property, out var member) &&
                    _hostExports.TryResolveExportOwner(binding, owner.Identifier?.Value,
                        _module.Declaration.Imports, out var ownerName) &&
                    _hostExports.TryGetGlobal(ownerName, member, out _)) return false;
                return true;
            }

            private bool IsNonInvokingIndex(Expression receiver, Expression index)
            {
                _expressionTypes.TryGetValue(index, out var indexType);
                _expressionTypes.TryGetValue(receiver, out var receiverType);
                _nativeObjectTypes.TryGetValue(receiver, out var native);
                return OperationEffects.IsIntrinsicIndex(receiverType, native, indexType);
            }
        }
    }
}
