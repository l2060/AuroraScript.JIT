using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Analysis
{
    /// <summary>
    /// Connects strongly typed callable slots to function values before typed
    /// analysis chooses native entry signatures.
    /// </summary>
    internal static class CallableContractAnalyzer
    {
        public static void Apply(
            CompileSession session,
            ModulePlan module)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(module);
            var functionsByDeclaration =
                new Dictionary<FunctionDeclaration, FunctionPlan>(
                    ReferenceEqualityComparer.Instance);
            var moduleFunctions =
                new Dictionary<string, FunctionPlan>(StringComparer.Ordinal);
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                if (function.Declaration != null)
                {
                    functionsByDeclaration[function.Declaration] = function;
                }
                if (function.IsModuleFunction &&
                    !string.IsNullOrEmpty(function.Name))
                {
                    moduleFunctions[function.Name] = function;
                }
            }

            var analyzer = new Analyzer(
                session,
                module,
                functionsByDeclaration,
                moduleFunctions);
            analyzer.Visit(
                module.Declaration,
                module.InitializerFunction);
            for (var i = 0; i < module.Functions.Count; i++)
            {
                var function = module.Functions[i];
                analyzer.Visit(
                    function.Declaration?.Body,
                    function);
            }
        }

        private sealed class Analyzer
        {
            private readonly ModulePlan _module;
            private readonly CompileSession _session;
            private readonly Dictionary<FunctionDeclaration, FunctionPlan>
                _functionsByDeclaration;
            private readonly Dictionary<string, FunctionPlan> _moduleFunctions;

            public Analyzer(
                CompileSession session,
                ModulePlan module,
                Dictionary<FunctionDeclaration, FunctionPlan>
                    functionsByDeclaration,
                Dictionary<string, FunctionPlan> moduleFunctions)
            {
                _session = session;
                _module = module;
                _functionsByDeclaration = functionsByDeclaration;
                _moduleFunctions = moduleFunctions;
            }

            public void Visit(AstNode node, FunctionPlan owner)
            {
                if (node == null)
                {
                    return;
                }
                if (node is FunctionDeclaration or LambdaExpression)
                {
                    return;
                }
                if (node is FunctionCallExpression call)
                {
                    TryApplyCall(call, owner);
                }
                var visitor = new ChildVisitor(this, owner);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private void TryApplyCall(
                FunctionCallExpression call,
                FunctionPlan owner)
            {
                var target = Unwrap(call.Target) as NameExpression;
                if (target == null ||
                    !_moduleFunctions.TryGetValue(
                        target.Identifier.Value,
                        out var callee) ||
                    HasLocal(owner, target.Identifier.Value))
                {
                    return;
                }

                var count = Math.Min(
                    call.Arguments.Count,
                    callee.Declaration.Parameters.Count);
                for (var i = 0; i < count; i++)
                {
                    if (!TypeReferenceFacts.TryGetFunctionType(
                            _module.Declaration,
                            callee.Declaration.Parameters[i].DeclaredType,
                            out var callable))
                    {
                        continue;
                    }

                    var argument = Unwrap(call.Arguments[i]);
                    FunctionPlan function = null;
                    if (argument is LambdaExpression lambda)
                    {
                        _functionsByDeclaration.TryGetValue(
                            lambda.Function,
                            out function);
                    }
                    else if (argument is NameExpression name &&
                        _moduleFunctions.TryGetValue(
                            name.Identifier.Value,
                            out var named) &&
                        !HasLocal(owner, name.Identifier.Value))
                    {
                        function = named;
                    }
                    if (function == null)
                    {
                        continue;
                    }
                    if (!ValidateCompatibility(
                            call.Arguments[i],
                            function,
                            callable) ||
                        !HasCompleteNativeSignature(callable))
                    {
                        continue;
                    }
                    Apply(function, callable);
                }
            }

            private bool ValidateCompatibility(
                Expression argument,
                FunctionPlan function,
                FunctionTypeDeclaration callable)
            {
                if (!callable.IsStrong)
                {
                    return true;
                }
                if (function.Declaration.Parameters.Count !=
                    callable.Parameters.Count)
                {
                    _session.ReportWarning(
                        argument,
                        $"Function value is incompatible with function type '{callable.Name.Value}': expected {callable.Parameters.Count} parameters but found {function.Declaration.Parameters.Count}.");
                    return false;
                }
                for (var i = 0; i < callable.Parameters.Count; i++)
                {
                    var expected =
                        callable.Parameters[i].DeclaredType;
                    var actual =
                        function.Declaration.Parameters[i].DeclaredType;
                    if (!function.IsLambda &&
                        expected != null &&
                        actual == null)
                    {
                        _session.ReportWarning(
                            argument,
                            $"Function value parameter {i + 1} has no declared type required by function type '{callable.Name.Value}'.");
                        return false;
                    }
                    if (actual != null &&
                        expected != null &&
                        !StringComparer.Ordinal.Equals(
                            actual.DisplayName,
                            expected.DisplayName))
                    {
                        _session.ReportWarning(
                            argument,
                            $"Function value parameter {i + 1} is incompatible with function type '{callable.Name.Value}': expected '{expected.DisplayName}' but found '{actual.DisplayName}'.");
                        return false;
                    }
                }
                if (!function.IsLambda &&
                    callable.ReturnType != null &&
                    function.Declaration.ReturnType == null)
                {
                    _session.ReportWarning(
                        argument,
                        $"Function value has no declared return type required by function type '{callable.Name.Value}'.");
                    return false;
                }
                if (function.Declaration.ReturnType != null &&
                    callable.ReturnType != null &&
                    !StringComparer.Ordinal.Equals(
                        function.Declaration.ReturnType.DisplayName,
                        callable.ReturnType.DisplayName))
                {
                    _session.ReportWarning(
                        argument,
                        $"Function value return type is incompatible with function type '{callable.Name.Value}': expected '{callable.ReturnType.DisplayName}' but found '{function.Declaration.ReturnType.DisplayName}'.");
                    return false;
                }
                return true;
            }

            private void Apply(
                FunctionPlan function,
                FunctionTypeDeclaration callable)
            {
                if (function.Declaration.Parameters.Count !=
                    callable.Parameters.Count)
                {
                    return;
                }
                if (function.HasCallableTypeConflict)
                {
                    return;
                }
                if (function.CallableType != null &&
                    !ReferenceEquals(function.CallableType, callable))
                {
                    function.CallableType = null;
                    function.CallableTypeModule = null;
                    function.IsDirectCallCandidate = false;
                    function.HasCallableTypeConflict = true;
                    return;
                }

                for (var i = 0; i < callable.Parameters.Count; i++)
                {
                    var expected = callable.Parameters[i].DeclaredType;
                    var actual = function.Declaration.Parameters[i].DeclaredType;
                    if (actual != null &&
                        !StringComparer.Ordinal.Equals(
                            actual.DisplayName,
                            expected.DisplayName))
                    {
                        return;
                    }
                }
                if (function.Declaration.ReturnType != null &&
                    !StringComparer.Ordinal.Equals(
                        function.Declaration.ReturnType.DisplayName,
                        callable.ReturnType.DisplayName))
                {
                    return;
                }
                if (!function.IsLambda)
                {
                    for (var i = 0;
                        i < function.Declaration.Parameters.Count;
                        i++)
                    {
                        if (function.Declaration.Parameters[i]
                                .DeclaredType == null)
                        {
                            return;
                        }
                    }
                    if (function.Declaration.ReturnType == null)
                    {
                        return;
                    }
                }
                for (var i = 0; i < callable.Parameters.Count; i++)
                {
                    if (function.IsLambda)
                    {
                        function.Declaration.Parameters[i].DeclaredType ??=
                            callable.Parameters[i].DeclaredType;
                    }
                }
                if (function.IsLambda)
                {
                    function.Declaration.ApplyContextualReturnType(
                        callable.ReturnType);
                }
                function.CallableType = callable;
                function.CallableTypeModule =
                    callable.Parent as ModuleDeclaration ??
                    _module.Declaration;
                function.IsDirectCallCandidate = true;
            }

            private static bool HasCompleteNativeSignature(
                FunctionTypeDeclaration callable)
            {
                if (callable.ReturnType == null)
                {
                    return false;
                }
                for (var i = 0; i < callable.Parameters.Count; i++)
                {
                    if (callable.Parameters[i].DeclaredType == null ||
                        callable.Parameters[i].Initializer != null ||
                        callable.Parameters[i].IsSpreadOperator)
                    {
                        return false;
                    }
                }
                return true;
            }

            private static bool HasLocal(
                FunctionPlan function,
                string name)
            {
                if (function == null)
                {
                    return false;
                }
                for (var i = 0; i < function.LocalSlots.Length; i++)
                {
                    if (StringComparer.Ordinal.Equals(
                            function.LocalSlots[i].Name,
                            name))
                    {
                        return true;
                    }
                }
                return false;
            }

            private static Expression Unwrap(Expression expression)
            {
                while (expression is GroupExpression group &&
                    group.Expressions.Count == 1)
                {
                    expression = group.Expressions[0];
                }
                return expression;
            }

            private readonly struct ChildVisitor : IAstChildVisitor
            {
                private readonly Analyzer _owner;
                private readonly FunctionPlan _function;

                public ChildVisitor(
                    Analyzer owner,
                    FunctionPlan function)
                {
                    _owner = owner;
                    _function = function;
                }

                public void Visit(AstNode node)
                {
                    _owner.Visit(node, _function);
                }
            }
        }
    }
}
