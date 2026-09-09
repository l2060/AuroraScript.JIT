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
    internal sealed class CapturedCellTypes
    {
        private readonly Dictionary<FunctionId, (FunctionId Owner, Expression Initializer)[]> _cells = new();

        public CapturedCellTypes(
            ModulePlan module,
            TypedFunctionBuilder.FunctionBinding[] bindings)
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

            var roots = new Dictionary<FunctionId, (FunctionId Owner, LocalSlotId Local)[]>();
            foreach (var function in module.Functions)
            {
                if (function.UpvalueSlots.Length == 0) continue;
                var slots = new (FunctionId Owner, LocalSlotId Local)[function.UpvalueSlots.Length];
                for (var slot = 0; slot < slots.Length; slot++)
                    TryResolveRoot(function.UpvalueSlots[slot], plans, out slots[slot].Owner, out slots[slot].Local);
                roots[function.Id] = slots;
            }
            if (roots.Count == 0) return;
            var unstable = new HashSet<long>();
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                if (function.CapturedLocalSlots.Length == 0 && function.UpvalueSlots.Length == 0) continue;
                new CellScanner(function, bindings[function.Id.Value], roots, plansByDeclaration, unstable)
                    .Scan(function.Declaration?.Body);
            }

            foreach (var pair in roots)
            {
                var cells = new (FunctionId Owner, Expression Initializer)[pair.Value.Length];
                for (var slot = 0; slot < cells.Length; slot++)
                {
                    var (owner, local) = pair.Value[slot];
                    if (!local.IsValid || unstable.Contains(GetKey(owner, local)) ||
                        !plans.TryGetValue(owner.Value, out var plan) ||
                        (uint)local.Value >= (uint)plan.LocalSlots.Length) continue;
                    var source = plan.LocalSlots[local.Value];
                    if (!source.IsParameter && source.Declaration is VariableDeclaration declaration &&
                        declaration.Pattern == null)
                        cells[slot] = (owner, declaration.Initializer);
                }
                _cells[pair.Key] = cells;
            }
        }

        public Dictionary<FunctionId, FlowValueType[]> Analyze(TypedFunctionCode[] functions, ModulePlan module = null)
        {
            var result = new Dictionary<FunctionId, FlowValueType[]>(_cells.Count);
            foreach (var pair in _cells)
            {
                var types = new FlowValueType[pair.Value.Length];
                for (var slot = 0; slot < types.Length; slot++)
                {
                    var (owner, initializer) = pair.Value[slot];
                    var index = module == null ? owner.Value : module.GetFunctionIndex(owner);
                    var type = initializer != null && (uint)index < (uint)functions.Length &&
                        functions[index] is { } code
                        ? code.GetExpressionType(initializer) : FlowValueType.Dynamic;
                    types[slot] = IsStableCellType(type) ? type : FlowValueType.Dynamic;
                }
                result[pair.Key] = types;
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
                if (!right.TryGetValue(item.Key, out var other) || !SameTypes(item.Value, other))
                {
                    return false;
                }
            }
            return true;
        }

        internal static bool SameTypes(FlowValueType[] left, FlowValueType[] right)
        {
            if (ReferenceEquals(left, right)) return true;
            if (left == null || right == null || left.Length != right.Length) return false;
            for (var i = 0; i < left.Length; i++)
                if (left[i] != right[i]) return false;
            return true;
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
            private readonly TypedFunctionBuilder.FunctionBinding _binding;
            private readonly IReadOnlyDictionary<FunctionId, (FunctionId Owner, LocalSlotId Local)[]> _roots;
            private readonly IReadOnlyDictionary<FunctionDeclaration, FunctionPlan>
                _plansByDeclaration;
            private readonly HashSet<long> _unstable;
            private readonly HashSet<int> _declared = new();

            public CellScanner(
                FunctionPlan function,
                TypedFunctionBuilder.FunctionBinding binding,
                IReadOnlyDictionary<FunctionId, (FunctionId Owner, LocalSlotId Local)[]> roots,
                IReadOnlyDictionary<FunctionDeclaration, FunctionPlan> plansByDeclaration,
                HashSet<long> unstable)
            {
                _function = function;
                _binding = binding;
                _roots = roots;
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
                if (_binding.Declarations.TryGetValue(declaration, out var slot) && slot.IsValid)
                    _declared.Add(slot.Value);
            }

            private void MarkWrite(Expression target)
            {
                if (target is not NameExpression name) return;
                if (!_binding.Names.TryGetValue(name, out var binding)) return;
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
                if (!_roots.TryGetValue(nested.Id, out var slots)) return;
                foreach (var (owner, local) in slots)
                {
                    if (!local.IsValid ||
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
                    (owner, local) = _roots[_function.Id][binding.Upvalue.Value];
                    return local.IsValid;
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
