using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Analysis
{
    internal static class ModuleUsageAnalyzer
    {
        public static void Apply(CompileSession session, ModulePlan modulePlan)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(modulePlan);

            if (!session.Capabilities.CanUseModuleDirectCall || modulePlan.Functions.Count == 0)
            {
                return;
            }

            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                var function = modulePlan.Functions[i];
                function.IsDirectCallCandidate = false;
                if (function.IsModuleFunction && function.Visibility == FunctionVisibility.InternalOnly)
                {
                    function.Visibility = FunctionVisibility.ModuleVisible;
                }
            }

            var candidateNames = BuildCandidateNameMap(session, modulePlan);
            if (candidateNames.Count == 0)
            {
                return;
            }

            var usages = new FunctionUsage[modulePlan.Functions.Count];
            var walker = new UsageWalker(candidateNames, usages);
            walker.Visit(modulePlan.Declaration);

            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                var function = modulePlan.Functions[i];
                if (!function.IsNativeDeclared)
                {
                    continue;
                }
                var usage = usages[i];
                if (usage.HasAssignment)
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Binding,
                        function.Declaration,
                        $"Native function '{function.Name}' cannot be assigned.");
                }

                function.IsDirectCallCandidate = true;
                if (function.Visibility != FunctionVisibility.Exported)
                {
                    // $native is an additional entry point, never the script
                    // function object used at dynamic boundaries.
                    function.Visibility = FunctionVisibility.ModuleVisible;
                }
            }
        }

        private static Dictionary<string, int> BuildCandidateNameMap(
            CompileSession session,
            ModulePlan modulePlan)
        {
            var occurrences = new Dictionary<string, int>(StringComparer.Ordinal);
            var firstIndexByName = new Dictionary<string, int>(StringComparer.Ordinal);

            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                var function = modulePlan.Functions[i];
                if (!function.IsModuleFunction ||
                    string.IsNullOrEmpty(function.Name) ||
                    !function.IsNativeDeclared)
                {
                    continue;
                }

                if (!modulePlan.TryGetSymbol(function.Name, out var symbolId))
                {
                    continue;
                }

                var symbol = session.Symbols[symbolId];
                if (symbol.Kind != BackendSymbolKind.Function || !ReferenceEquals(symbol.Declaration, function.Declaration))
                {
                    continue;
                }

                occurrences.TryGetValue(function.Name, out var count);
                occurrences[function.Name] = count + 1;
                firstIndexByName.TryAdd(function.Name, i);
            }

            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in firstIndexByName)
            {
                if (occurrences[pair.Key] == 1)
                {
                    result.Add(pair.Key, pair.Value);
                }
            }
            return result;
        }

        private struct FunctionUsage
        {
            public int DirectCallCount;
            public bool HasAssignment;
            public bool HasValueRead;
        }

        private sealed class UsageWalker
        {
            private readonly Dictionary<string, int> _candidateNames;
            private readonly Dictionary<string, int> _shadowedNames;
            private readonly List<string> _shadowStack;
            private readonly List<int> _scopeMarks;
            private readonly FunctionUsage[] _usages;

            public UsageWalker(Dictionary<string, int> candidateNames, FunctionUsage[] usages)
            {
                _candidateNames = candidateNames;
                _shadowedNames = new Dictionary<string, int>(StringComparer.Ordinal);
                _shadowStack = new List<string>();
                _scopeMarks = new List<int>();
                _usages = usages;
            }

            public void Visit(AstNode node)
            {
                if (node == null)
                {
                    return;
                }

                switch (node)
                {
                    case ModuleDeclaration module:
                        VisitModule(module);
                        return;
                    case TypedDocumentExpression tdoc:
                        Visit(tdoc.Value);
                        return;
                    case CheckExpression check:
                        Visit(check.Value);
                        return;
                    case NameExpression name:
                        MarkValueRead(name);
                        return;
                    case FunctionCallExpression call:
                        VisitCall(call);
                        return;
                    case NewExpression newExpression:
                        VisitConstructorCall(newExpression.Expression);
                        return;
                    case AssignmentExpression assignment:
                        MarkWriteTarget(assignment.Left);
                        Visit(assignment.Right);
                        return;
                    case CompoundExpression compound:
                        MarkWriteTarget(compound.Left);
                        Visit(compound.Right);
                        return;
                    case UnaryExpression unary when IsIncrementOrDecrement(unary.Operator):
                        MarkWriteTarget(unary.Expression);
                        return;
                    case VariableDeclaration variable:
                        Visit(variable.Initializer);
                        return;
                    case FunctionDeclaration function:
                        VisitFunction(function);
                        return;
                    case LambdaExpression lambda:
                        Visit(lambda.Function);
                        return;
                    case GetPropertyExpression getProperty:
                        Visit(getProperty.Object);
                        return;
                    case SetPropertyExpression setProperty:
                        Visit(setProperty.Object);
                        Visit(setProperty.Value);
                        return;
                    case MapKeyValueExpression mapEntry:
                        Visit(mapEntry.Value);
                        return;
                }

                var visitor = new ChildVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private void VisitModule(ModuleDeclaration module)
            {
                for (var i = 0; i < module.Imports.Count; i++)
                {
                    Visit(module.Imports[i]);
                }
                for (var i = 0; i < module.Statements.Count; i++)
                {
                    Visit(module.Statements[i]);
                }
                for (var i = 0; i < module.Functions.Count; i++)
                {
                    Visit(module.Functions[i]);
                }
            }

            private void VisitFunction(FunctionDeclaration function)
            {
                EnterScope();
                for (var i = 0; i < function.Parameters.Count; i++)
                {
                    DeclarePattern(function.Parameters[i]);
                }

                CollectLocalDeclarations(function.Body);

                for (var i = 0; i < function.Parameters.Count; i++)
                {
                    Visit(function.Parameters[i].Initializer);
                }
                Visit(function.Body);
                ExitScope();
            }

            private void VisitCall(FunctionCallExpression call)
            {
                if (call.Target is NameExpression target && TryGetCandidateIndex(target, out var index))
                {
                    if (HasSpreadArgument(call))
                    {
                        _usages[index].HasValueRead = true;
                    }
                    else
                    {
                        _usages[index].DirectCallCount++;
                    }
                }
                else
                {
                    Visit(call.Target);
                }

                for (var i = 0; i < call.Arguments.Count; i++)
                {
                    Visit(call.Arguments[i]);
                }
            }

            private static bool HasSpreadArgument(FunctionCallExpression call)
            {
                for (var i = 0; i < call.Arguments.Count; i++)
                {
                    if (call.Arguments[i] is SpreadExpression)
                    {
                        return true;
                    }
                }

                return false;
            }

            private void VisitConstructorCall(FunctionCallExpression call)
            {
                if (call == null)
                {
                    return;
                }

                Visit(call.Target);
                for (var i = 0; i < call.Arguments.Count; i++)
                {
                    Visit(call.Arguments[i]);
                }
            }

            private void MarkWriteTarget(Expression target)
            {
                if (target is NameExpression name && TryGetCandidateIndex(name, out var index))
                {
                    _usages[index].HasAssignment = true;
                    return;
                }

                Visit(target);
            }

            private void MarkValueRead(NameExpression name)
            {
                if (TryGetCandidateIndex(name, out var index))
                {
                    _usages[index].HasValueRead = true;
                }
            }

            private bool TryGetCandidateIndex(NameExpression name, out int index)
            {
                if (name.Identifier == null)
                {
                    index = -1;
                    return false;
                }

                if (_shadowedNames.ContainsKey(name.Identifier.Value))
                {
                    index = -1;
                    return false;
                }

                return _candidateNames.TryGetValue(name.Identifier.Value, out index);
            }

            private void EnterScope()
            {
                _scopeMarks.Add(_shadowStack.Count);
            }

            private void ExitScope()
            {
                var mark = _scopeMarks[^1];
                _scopeMarks.RemoveAt(_scopeMarks.Count - 1);
                for (var i = _shadowStack.Count - 1; i >= mark; i--)
                {
                    var name = _shadowStack[i];
                    var count = _shadowedNames[name] - 1;
                    if (count == 0)
                    {
                        _shadowedNames.Remove(name);
                    }
                    else
                    {
                        _shadowedNames[name] = count;
                    }
                    _shadowStack.RemoveAt(i);
                }
            }

            private void DeclareShadow(string name)
            {
                if (string.IsNullOrEmpty(name) || !_candidateNames.ContainsKey(name))
                {
                    return;
                }

                _shadowedNames.TryGetValue(name, out var count);
                _shadowedNames[name] = count + 1;
                _shadowStack.Add(name);
            }

            private void DeclarePattern(VariableDeclaration variable)
            {
                if (variable == null)
                {
                    return;
                }

                if (variable.IsDeclare)
                {
                    return;
                }

                if (variable.Name != null)
                {
                    DeclareShadow(variable.Name.Value);
                    return;
                }

                DeclarePattern(variable.Pattern);
            }

            private void DeclarePattern(Expression pattern)
            {
                switch (pattern)
                {
                    case NameExpression name:
                        DeclareShadow(name.Identifier.Value);
                        return;
                    case SpreadExpression { Expression: NameExpression spreadName }:
                        DeclareShadow(spreadName.Identifier.Value);
                        return;
                    case ObjectDestructuringPattern objectPattern:
                        for (var i = 0; i < objectPattern.Properties.Count; i++)
                        {
                            DeclareShadow(objectPattern.Properties[i].Value);
                        }
                        return;
                    case ArrayDestructuringPattern arrayPattern:
                        for (var i = 0; i < arrayPattern.Elements.Count; i++)
                        {
                            DeclarePattern(arrayPattern.Elements[i]);
                        }
                        return;
                }
            }

            public void CollectLocalDeclarations(AstNode node)
            {
                if (node == null)
                {
                    return;
                }

                switch (node)
                {
                    case VariableDeclaration variable:
                        DeclarePattern(variable);
                        return;
                    case FunctionDeclaration function:
                        if (function.Flags != FunctionFlags.Declare && function.Name != null)
                        {
                            DeclareShadow(function.Name.Value);
                        }
                        return;
                    case LambdaExpression:
                        return;
                    case ObjectDestructuringPattern:
                    case ArrayDestructuringPattern:
                        return;
                }

                var collector = new DeclarationCollector(this);
                AstTraversal.VisitChildren(node, ref collector);
            }

            private static bool IsIncrementOrDecrement(Operator op)
            {
                return op == Operator.PreIncrement ||
                    op == Operator.PostIncrement ||
                    op == Operator.PreDecrement ||
                    op == Operator.PostDecrement;
            }
        }

        private readonly struct DeclarationCollector : IAstChildVisitor
        {
            private readonly UsageWalker _walker;

            public DeclarationCollector(UsageWalker walker)
            {
                _walker = walker;
            }

            public void Visit(AstNode node)
            {
                _walker.CollectLocalDeclarations(node);
            }
        }

        private readonly struct ChildVisitor : IAstChildVisitor
        {
            private readonly UsageWalker _walker;

            public ChildVisitor(UsageWalker walker)
            {
                _walker = walker;
            }

            public void Visit(AstNode node)
            {
                _walker.Visit(node);
            }
        }
    }
}
