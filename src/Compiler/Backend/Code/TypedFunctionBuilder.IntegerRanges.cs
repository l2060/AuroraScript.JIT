using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
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
            // Bounds are scoped to a dominating condition. Versions invalidate
            // them on writes, including writes in only one branch of a join.
            private readonly List<(int Index, int Version, int String, long Offset)> _stringIndexBounds = new();
            private readonly Dictionary<int, int> _indexWriteVersions = new();

            private void RestoreStringBounds(int count) => _stringIndexBounds.RemoveRange(count, _stringIndexBounds.Count - count);
            private int IndexVersion(int slot) => _indexWriteVersions.TryGetValue(slot, out var version) ? version : 0;

            private void InvalidateStringBounds(AstNode node)
            {
                foreach (var bound in _stringIndexBounds)
                    if (bound.Version == IndexVersion(bound.Index) && WritesLocal(node, new LocalSlotId(bound.Index)))
                        _indexWriteVersions[bound.Index] = bound.Version + 1;
            }

            private static int StatementIndex(BlockStatement block, Statement statement)
            {
                for (var i = 0; i < block.Statements.Count; i++)
                    if (ReferenceEquals(block.Statements[i], statement)) return i;
                return -1;
            }

            private bool TryGetIndexOffset(Expression expression, out int slot, out long offset)
            {
                expression = UnwrapGroups(expression);
                offset = 0;
                if (expression is BinaryExpression binary &&
                    (binary.Operator == Operator.Add || binary.Operator == Operator.Subtract) &&
                    TryEvaluateInt32Constant(binary.Right, out var delta))
                {
                    offset = binary.Operator == Operator.Add ? delta : -(long)delta;
                    expression = UnwrapGroups(binary.Left);
                }
                slot = -1;
                if (expression is not NameExpression name || !_names.TryGetValue(name, out var binding) ||
                    !binding.IsLocal || IsCaptured(binding.Local)) return false;
                slot = binding.Local.Value;
                return true;
            }

            private bool TryGetStableString(Expression expression, out int slot)
            {
                slot = -1;
                if (UnwrapGroups(expression) is not NameExpression name || !_names.TryGetValue(name, out var binding) ||
                    !binding.IsLocal || IsCaptured(binding.Local) || FunctionWritesLocal(binding.Local) ||
                    !_expressionTypes.TryGetValue(expression, out var type) || type != FlowValueType.String) return false;
                slot = binding.Local.Value;
                return true;
            }

            private void RefineStringIndexBound(BinaryExpression condition, bool truth)
            {
                if (condition.Operator != (truth ? Operator.LessThan : Operator.GreaterThanOrEqual) ||
                    !TryGetIndexOffset(condition.Left, out var index, out var offset)) return;
                var length = UnwrapGroups(condition.Right);
                if (length is NameExpression name && _names.TryGetValue(name, out var binding) &&
                    binding.IsLocal && !IsCaptured(binding.Local) && !FunctionWritesLocal(binding.Local))
                    length = (_function.LocalSlots[binding.Local.Value].Declaration as VariableDeclaration)?.Initializer;
                if (length is GetPropertyExpression property && IsStaticProperty(property.Property, "length") &&
                    TryGetStableString(property.Object, out var receiver))
                    _stringIndexBounds.Add((index, IndexVersion(index), receiver, offset));
            }

            private bool IsBoundedCharacterRead(FunctionCallExpression call, GetPropertyExpression target)
            {
                if (call.Arguments.Count != 1 || !TryGetStableString(target.Object, out var receiver) ||
                    !TryGetIntegerRange(call.Arguments[0], out var min, out var max) || min < 0 || max > int.MaxValue ||
                    !TryGetIndexOffset(call.Arguments[0], out var index, out var offset)) return false;
                foreach (var bound in _stringIndexBounds)
                    if (bound.Index == index && bound.Version == IndexVersion(index) &&
                        bound.String == receiver && offset <= bound.Offset) return true;
                return false;
            }

            private bool TryGetCountedAccumulationRange(int slot, out long min, out long max)
            {
                min = max = 0;
                if (_function.LocalSlots[slot].Declaration is not VariableDeclaration declaration ||
                    declaration.Initializer == null || !_expressionIntegerRanges.TryGetValue(declaration.Initializer, out var initial))
                    return false;
                ForStatement loop = null;
                foreach (var definition in _localDefinitions[slot])
                {
                    if (ReferenceEquals(definition, declaration.Initializer)) continue;
                    AstNode parent = definition;
                    while (parent != null && parent is not (ForStatement or WhileStatement or ForInStatement)) parent = parent.Parent;
                    if (parent is not ForStatement found || loop != null && !ReferenceEquals(loop, found)) return false;
                    loop = found;
                }
                if (loop == null || declaration.Parent is not BlockStatement block || !ReferenceEquals(loop.Parent, block) ||
                    StatementIndex(block, declaration) >= StatementIndex(block, loop) ||
                    !TryGetSafeInt32Induction(loop, out var induction) || induction.Value == slot ||
                    loop.Initializer is not VariableDeclaration iterator ||
                    !_expressionIntegerRanges.TryGetValue(iterator.Initializer, out var start) ||
                    !_expressionIntegerRanges.TryGetValue(((BinaryExpression)loop.Condition).Right, out var end) ||
                    WritesLocal(loop.Initializer, new LocalSlotId(slot)) ||
                    WritesLocal(loop.Condition, induction) ||
                    WritesLocal(loop.Condition, new LocalSlotId(slot))) return false;
                // Require an unconditional positive step; body increments may
                // shorten the loop, but cannot increase this iteration bound.
                var increment = loop.Incrementor;
                if (!(increment is UnaryExpression unary && IsLocalName(unary.Expression, induction) &&
                        (unary.Operator == Operator.PreIncrement || unary.Operator == Operator.PostIncrement)) &&
                    !(increment is CompoundExpression compound && IsLocalName(compound.Left, induction) &&
                        compound.Operator.SimplerOperator == Operator.Add &&
                        TryEvaluateInt32Constant(compound.Right, out var step) && step > 0)) return false;
                var writes = new Int32InductionWriteAnalyzer(this, new LocalSlotId(slot));
                writes.Analyze(loop.Body, rejectNestedLoops: true);
                writes.Analyze(loop.Incrementor, rejectNestedLoops: true);
                if (!writes.IsValid || writes.MaximumDelta <= 0) return false;
                var iterations = Math.Max(0, end.Max - start.Min + (((BinaryExpression)loop.Condition).Operator == Operator.LessThanOrEqual ? 1 : 0));
                try { max = checked(initial.Max + checked(iterations * writes.MaximumDelta)); }
                catch (OverflowException) { return false; }
                min = initial.Min;
                return min >= 0 && max <= 9007199254740991L;
            }

            private bool TryGetResetCounterRange(int slot, out long min, out long max)
            {
                min = max = 0;
                int step = 0, limit = 0;
                // Every increment must be immediately followed by its reset.
                // Constant definitions must occupy the same residue class, so
                // equality with the limit cannot be skipped by an overshoot.
                foreach (var definition in _localDefinitions[slot])
                {
                    if (definition is not CompoundExpression compound) continue;
                    if (compound.Operator.SimplerOperator != Operator.Add ||
                        !TryEvaluateInt32Constant(compound.Right, out var delta) || delta <= 0 || step != 0 && step != delta ||
                        compound.Parent is not ExpressionStatement statement || statement.Parent is not BlockStatement block)
                        return false;
                    var index = StatementIndex(block, statement);
                    if (index + 1 >= block.Statements.Count || block.Statements[index + 1] is not IfStatement conditional ||
                        conditional.Condition is not BinaryExpression condition || condition.Operator != Operator.Equal ||
                        !IsLocalName(condition.Left, new LocalSlotId(slot)) || !TryEvaluateInt32Constant(condition.Right, out var bound) ||
                        bound <= 0 || limit != 0 && limit != bound) return false;
                    var body = conditional.Body is BlockStatement resetBlock && resetBlock.Statements.Count != 0
                        ? resetBlock.Statements[0] : conditional.Body;
                    if (body is not ExpressionStatement { Expression: AssignmentExpression reset } ||
                        !IsLocalName(reset.Left, new LocalSlotId(slot)) || !TryEvaluateInt32Constant(reset.Right, out var resetValue) ||
                        resetValue < 0 || resetValue >= bound || (bound - resetValue) % delta != 0) return false;
                    step = delta;
                    limit = bound;
                }
                if (step == 0) return false;
                foreach (var definition in _localDefinitions[slot])
                    if (definition is not CompoundExpression &&
                        (!TryEvaluateInt32Constant(definition, out var value) || value < 0 || value >= limit || (limit - value) % step != 0))
                        return false;
                max = limit;
                return true;
            }
        }
    }
}
