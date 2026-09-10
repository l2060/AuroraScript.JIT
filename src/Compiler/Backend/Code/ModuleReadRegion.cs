using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Tokens;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Code
{
    // Common module loads can span statements only while no intervening operation
    // can write module state or call user code. Only private lexical bindings with
    // no writers in the module participate; their values keep the dynamic ABI.
    internal sealed class ModuleReadRegion
    {
        internal readonly record struct BindingValue(FlowValueType Type, HostNativeObjectDescriptor Native);
        internal int StatementCount { get; private set; }
        internal Dictionary<SymbolId, List<NameExpression>> Reads { get; } = new();

        internal static Dictionary<SymbolId, BindingValue> GetStableBindings(ModulePlan module,
            TypedFunctionCode initializer, CallableReturnPredictions functions)
        {
            var bindings = new Dictionary<SymbolId, BindingValue>();
            foreach (var statement in module.Declaration.Statements)
            {
                if (statement is not VariableDeclaration { IsDeclare: false } variable ||
                    variable.Access == MemberAccess.Export || variable.Name == null ||
                    !module.TryGetSymbol(variable.Name.Value, out var symbol)) continue;
                var type = initializer.GetExpressionType(variable.Initializer);
                if (type is FlowValueType.Int32 or FlowValueType.UInt32) type = FlowValueType.Number;
                var native = initializer.GetNativeObjectType(variable.Initializer);
                // An unknown initializer can itself produce a runtime accessor.
                // Such a binding must keep ordinary reads, even without script writes.
                if (native != null || OperationEffects.IsPrimitive(type) || FlowValueTypeFacts.IsPackedArray(type))
                    bindings[symbol] = new BindingValue(type, native);
            }
            RemoveWrittenBindings(functions.GetInitializerBinding(module), bindings);
            foreach (var function in module.Functions)
                RemoveWrittenBindings(functions.Bindings[function.Id.Value], bindings);
            return bindings;
        }

        private static void RemoveWrittenBindings(TypedFunctionBuilder.FunctionBinding function,
            Dictionary<SymbolId, BindingValue> bindings)
        {
            foreach (var entry in function.Names)
            {
                var name = entry.Key;
                if (!bindings.ContainsKey(entry.Value.ModuleSymbol)) continue;
                if (name.Parent is AssignmentExpression assignment && ReferenceEquals(assignment.Left, name) ||
                    name.Parent is CompoundExpression compound && ReferenceEquals(compound.Left, name) ||
                    name.Parent is UnaryExpression unary && IsMutation(unary.Operator) ||
                    name.Parent is DeleteStatement ||
                    name.Parent is InExpression iterator && iterator.Parent is ForInStatement && ReferenceEquals(iterator.Left, name))
                    bindings.Remove(entry.Value.ModuleSymbol);
            }
        }

        internal static ModuleReadRegion Analyze(IReadOnlyList<Statement> statements, int start,
            TypedFunctionCode code, IReadOnlyDictionary<SymbolId, BindingValue> bindings)
        {
            var region = new ModuleReadRegion();
            for (var i = start; i < statements.Count; i++)
            {
                var expression = statements[i] switch
                {
                    ExpressionStatement statement => statement.Expression,
                    VariableDeclaration { Pattern: null, IsDeclare: false } variable => variable.Initializer,
                    ReturnStatement statement => statement.Expression,
                    _ => null
                };
                var reads = new List<NameExpression>();
                if (expression == null || !Visit(expression, code, bindings, reads)) break;
                region.StatementCount++;
                foreach (var name in reads)
                {
                    var symbol = code.GetName(name).ModuleSymbol;
                    if (!region.Reads.TryGetValue(symbol, out var references)) region.Reads[symbol] = references = new();
                    references.Add(name);
                }
                if (statements[i] is ReturnStatement) break;
            }
            return region.StatementCount > 0 ? region : null;
        }

        private static bool Visit(Expression expression, TypedFunctionCode code,
            IReadOnlyDictionary<SymbolId, BindingValue> bindings, List<NameExpression> reads)
        {
            bool Child(Expression child) => Visit(child, code, bindings, reads);
            FlowValueType Type(Expression child) => child is NameExpression name &&
                bindings.TryGetValue(code.GetName(name).ModuleSymbol, out var value) ? value.Type : code.GetExpressionType(child);
            HostNativeObjectDescriptor Native(Expression child) => child is NameExpression name &&
                bindings.TryGetValue(code.GetName(name).ModuleSymbol, out var value) ? value.Native : code.GetNativeObjectType(child);
            bool Index(GetElementExpression element) =>
                OperationEffects.IsIntrinsicIndex(Type(element.Object), Native(element.Object), Type(element.Index)) &&
                Child(element.Object) && Child(element.Index);
            bool Local(Expression target) => target is NameExpression name &&
                (code.GetName(name).IsLocal || code.GetName(name).Upvalue.IsValid);

            switch (expression)
            {
                case LiteralExpression: return true;
                case NameExpression name:
                    var binding = code.GetName(name);
                    if (binding.IsContext) return false;
                    if (binding.IsLocal || binding.Upvalue.IsValid || binding.HasConstant) return true;
                    if (!binding.ModuleSymbol.IsValid || !bindings.ContainsKey(binding.ModuleSymbol)) return false;
                    reads.Add(name);
                    return true;
                case GroupExpression group:
                    foreach (var child in group.Expressions) if (!Child(child)) return false;
                    return true;
                case GetElementExpression element: return Index(element);
                case GetPropertyExpression property:
                    return Native(property.Object) is { } native && property.Property is NameExpression member &&
                        native.TryGetField(member.Identifier.Value, out _) && Child(property.Object);
                case AssignmentExpression assignment:
                    return Local(assignment.Left) && Child(assignment.Right);
                case CompoundExpression compound:
                    return Local(compound.Left) && OperationEffects.IsPrimitive(Type(compound.Left)) &&
                        OperationEffects.IsPrimitive(Type(compound.Right)) && Child(compound.Right);
                case UnaryExpression unary:
                    if (IsMutation(unary.Operator))
                        return Local(unary.Expression) || unary.Expression is GetElementExpression target && Index(target);
                    return OperationEffects.IsPrimitive(Type(unary.Expression)) && Child(unary.Expression);
                case BinaryExpression binary:
                    return binary.Operator != Operator.LogicalAnd && binary.Operator != Operator.LogicalOr &&
                        OperationEffects.IsPrimitive(Type(binary.Left)) && OperationEffects.IsPrimitive(Type(binary.Right)) &&
                        Child(binary.Left) && Child(binary.Right);
                default: return false;
            }
        }

        private static bool IsMutation(Operator op) => op == Operator.PreIncrement || op == Operator.PostIncrement ||
            op == Operator.PreDecrement || op == Operator.PostDecrement;
    }
}
