using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Code
{
    /// <summary>
    /// Recovers the value type held by a closure cell.
    /// <para>
    /// A captured binding lives in a shared <c>Upvalue</c> whose storage is a
    /// <c>ScriptDatum</c>, so a closure body would otherwise read it as
    /// <see cref="FlowValueType.Dynamic"/> and lose every fact the declaring
    /// function proved. That erasure is what turns a captured packed array into
    /// a dynamic property lookup and a dynamic element read inside the hottest
    /// loop of a benchmark body.
    /// </para>
    /// <para>
    /// A cell is only typed when it is written exactly once, by the initializer
    /// of its own declaration, and every closure that captures it is created
    /// after that declaration runs. Anything else keeps the dynamic type.
    /// </para>
    /// </summary>
    internal static class CapturedCellTypes
    {
        public static Dictionary<FunctionId, FlowValueType[]> Analyze(
            ModulePlan module,
            TypedFunctionCode[] functions)
        {
            var plans = new Dictionary<int, FunctionPlan>();
            var plansByDeclaration =
                new Dictionary<FunctionDeclaration, FunctionPlan>(
                    ReferenceEqualityComparer.Instance);
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                plans[function.Id.Value] = function;
                if (function.Declaration != null)
                {
                    plansByDeclaration[function.Declaration] = function;
                }
            }

            var unstable = new HashSet<long>();
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                var code = GetCode(functions, function.Id);
                if (code == null) continue;
                new CellScanner(function, code, plans, plansByDeclaration, unstable)
                    .Scan(function.Declaration?.Body);
            }

            var result = new Dictionary<FunctionId, FlowValueType[]>();
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                if (function.UpvalueSlots.Length == 0) continue;
                var types = new FlowValueType[function.UpvalueSlots.Length];
                for (var slot = 0; slot < types.Length; slot++)
                {
                    types[slot] = TryResolveRoot(
                        function.UpvalueSlots[slot],
                        plans,
                        out var owner,
                        out var local)
                        ? GetCellType(owner, local, plans, functions, unstable)
                        : FlowValueType.Dynamic;
                }
                result[function.Id] = types;
            }
            return result;
        }

        public static bool SameTypes(
            IReadOnlyDictionary<FunctionId, FlowValueType[]> left,
            IReadOnlyDictionary<FunctionId, FlowValueType[]> right)
        {
            if (left == null || right == null) return ReferenceEquals(left, right);
            if (left.Count != right.Count) return false;
            foreach (var item in left)
            {
                if (!right.TryGetValue(item.Key, out var other) ||
                    other.Length != item.Value.Length)
                {
                    return false;
                }
                for (var i = 0; i < other.Length; i++)
                {
                    if (other[i] != item.Value[i]) return false;
                }
            }
            return true;
        }

        private static TypedFunctionCode GetCode(
            TypedFunctionCode[] functions,
            FunctionId id)
        {
            return functions != null && (uint)id.Value < (uint)functions.Length
                ? functions[id.Value]
                : null;
        }

        private static FlowValueType GetCellType(
            FunctionId owner,
            LocalSlotId local,
            IReadOnlyDictionary<int, FunctionPlan> plans,
            TypedFunctionCode[] functions,
            HashSet<long> unstable)
        {
            if (unstable.Contains(GetKey(owner, local)) ||
                !plans.TryGetValue(owner.Value, out var plan) ||
                (uint)local.Value >= (uint)plan.LocalSlots.Length)
            {
                return FlowValueType.Dynamic;
            }

            var code = GetCode(functions, owner);
            var slot = plan.LocalSlots[local.Value];
            if (code == null ||
                slot.IsParameter ||
                slot.Declaration is not VariableDeclaration declaration ||
                declaration.Pattern != null ||
                declaration.Initializer == null)
            {
                return FlowValueType.Dynamic;
            }

            var type = code.GetExpressionType(declaration.Initializer);
            return IsStableCellType(type) ? type : FlowValueType.Dynamic;
        }

        /// <summary>
        /// Restricts propagation to types the closure body can act on without
        /// extra structural facts. Object shapes are excluded because their
        /// field layout does not travel with the cell, so typing them would
        /// cost a cast without removing any dynamic lookup.
        /// </summary>
        private static bool IsStableCellType(FlowValueType type)
        {
            return type == FlowValueType.Boolean ||
                type == FlowValueType.String ||
                type == FlowValueType.Array ||
                FlowValueTypeFacts.IsNumeric(type) ||
                FlowValueTypeFacts.IsPackedArray(type);
        }

        private static bool TryResolveRoot(
            UpvalueSlot slot,
            IReadOnlyDictionary<int, FunctionPlan> plans,
            out FunctionId owner,
            out LocalSlotId local)
        {
            for (var depth = 0; depth < 64; depth++)
            {
                if (!slot.IsInherited)
                {
                    owner = slot.SourceFunction;
                    local = slot.SourceLocal;
                    return local.IsValid;
                }
                if (!slot.SourceUpvalue.IsValid ||
                    !plans.TryGetValue(slot.SourceFunction.Value, out var parent) ||
                    (uint)slot.SourceUpvalue.Value >= (uint)parent.UpvalueSlots.Length)
                {
                    break;
                }
                slot = parent.UpvalueSlots[slot.SourceUpvalue.Value];
            }
            owner = default;
            local = LocalSlotId.Invalid;
            return false;
        }

        private static long GetKey(FunctionId owner, LocalSlotId local)
        {
            return ((long)owner.Value << 32) | (uint)local.Value;
        }

        private sealed class CellScanner
        {
            private readonly FunctionPlan _function;
            private readonly TypedFunctionCode _code;
            private readonly IReadOnlyDictionary<int, FunctionPlan> _plans;
            private readonly IReadOnlyDictionary<FunctionDeclaration, FunctionPlan>
                _plansByDeclaration;
            private readonly HashSet<long> _unstable;
            private readonly HashSet<int> _declared = new();

            public CellScanner(
                FunctionPlan function,
                TypedFunctionCode code,
                IReadOnlyDictionary<int, FunctionPlan> plans,
                IReadOnlyDictionary<FunctionDeclaration, FunctionPlan> plansByDeclaration,
                HashSet<long> unstable)
            {
                _function = function;
                _code = code;
                _plans = plans;
                _plansByDeclaration = plansByDeclaration;
                _unstable = unstable;
            }

            public void Scan(AstNode node)
            {
                Visit(node);
            }

            private void Visit(AstNode node)
            {
                switch (node)
                {
                    case null:
                        return;
                    case BlockStatement block:
                        // Hoisted declarations can run before the statements
                        // above them, so they observe the cell first.
                        for (var i = 0; i < block.Functions.Count; i++)
                        {
                            Visit(block.Functions[i]);
                        }
                        for (var i = 0; i < block.Statements.Count; i++)
                        {
                            Visit(block.Statements[i]);
                        }
                        return;
                    case FunctionDeclaration nested:
                        RecordCapture(nested);
                        return;
                    case LambdaExpression lambda:
                        RecordCapture(lambda.Function);
                        return;
                    case VariableDeclaration variable:
                        Visit(variable.Initializer);
                        MarkDeclared(variable);
                        return;
                }

                switch (node)
                {
                    case AssignmentExpression assignment:
                        MarkWrite(assignment.Left);
                        break;
                    case CompoundExpression compound:
                        MarkWrite(compound.Left);
                        break;
                    case UnaryExpression unary when IsMutation(unary.Operator):
                        MarkWrite(unary.Expression);
                        break;
                    case ForInStatement forIn:
                        MarkWrite(forIn.Iterator?.Left);
                        break;
                }

                var visitor = new ChildVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private void MarkDeclared(VariableDeclaration declaration)
            {
                var slot = _code.GetDeclarationSlot(declaration);
                if (slot.IsValid) _declared.Add(slot.Value);
            }

            private void MarkWrite(Expression target)
            {
                if (target is not NameExpression name) return;
                var binding = _code.GetName(name);
                if (TryResolveBinding(binding, out var owner, out var local))
                {
                    _unstable.Add(GetKey(owner, local));
                }
            }

            private void RecordCapture(FunctionDeclaration declaration)
            {
                if (declaration == null ||
                    !_plansByDeclaration.TryGetValue(declaration, out var nested))
                {
                    return;
                }
                for (var i = 0; i < nested.UpvalueSlots.Length; i++)
                {
                    if (!TryResolveRoot(
                            nested.UpvalueSlots[i],
                            _plans,
                            out var owner,
                            out var local) ||
                        owner.Value != _function.Id.Value ||
                        _declared.Contains(local.Value))
                    {
                        continue;
                    }
                    _unstable.Add(GetKey(owner, local));
                }
            }

            private bool TryResolveBinding(
                BoundName binding,
                out FunctionId owner,
                out LocalSlotId local)
            {
                if (binding.IsLocal)
                {
                    owner = _function.Id;
                    local = binding.Local;
                    return true;
                }
                if (binding.Upvalue.IsValid &&
                    (uint)binding.Upvalue.Value < (uint)_function.UpvalueSlots.Length)
                {
                    return TryResolveRoot(
                        _function.UpvalueSlots[binding.Upvalue.Value],
                        _plans,
                        out owner,
                        out local);
                }
                owner = default;
                local = LocalSlotId.Invalid;
                return false;
            }

            private static bool IsMutation(Operator op)
            {
                return op == Operator.PreIncrement || op == Operator.PostIncrement ||
                    op == Operator.PreDecrement || op == Operator.PostDecrement;
            }

            private readonly struct ChildVisitor : IAstChildVisitor
            {
                private readonly CellScanner _owner;

                public ChildVisitor(CellScanner owner)
                {
                    _owner = owner;
                }

                public void Visit(AstNode node)
                {
                    _owner.Visit(node);
                }
            }
        }
    }
}
