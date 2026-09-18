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
                // Spread changes positional correspondence. The ordinary call
                // remains valid, but supplies no contextual parameter proof.
                for (var i = 0; i < call.Arguments.Count; i++)
                    if (call.Arguments[i] is SpreadExpression)
                        return;

                var target = Unwrap(call.Target) as NameExpression;
                FunctionPlan callee = null;
                if (target != null)
                    _moduleFunctions.TryGetValue(
                        target.Identifier.Value,
                        out callee);

                if (callee != null &&
                    !HasLocal(owner, target.Identifier.Value))
                {
                    var count = Math.Min(
                        call.Arguments.Count,
                        callee.Declaration.Parameters.Count);
                    for (var i = 0; i < count; i++)
                    {
                        if (TypeReferenceFacts.TryGetFunctionType(
                                _module.Declaration,
                                callee.Declaration.Parameters[i].DeclaredType,
                                out var callable))
                        {
                            ApplyArgument(
                                call.Arguments[i],
                                callable,
                                owner);
                        }
                    }
                    return;
                }

                if (!TryGetHostCallableContract(
                        call,
                        owner,
                        out var argument,
                        out var hostCallable,
                        out var objectArgumentsAllowNull))
                {
                    return;
                }
                ApplyArgument(
                    argument,
                    hostCallable,
                    owner,
                    objectArgumentsAllowNull);
            }

            private void ApplyArgument(
                Expression argumentExpression,
                FunctionTypeDeclaration callable,
                FunctionPlan owner,
                bool objectArgumentsAllowNull = false)
            {
                var argument = Unwrap(argumentExpression);
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
                    return;

                if (function.IsLambda)
                    RecordParameterPredictions(function, callable);
                if (!ValidateCompatibility(
                        argumentExpression,
                        function,
                        callable) ||
                    !HasCompleteNativeSignature(callable))
                {
                    return;
                }
                Apply(
                    function,
                    callable,
                    objectArgumentsAllowNull);
            }

            // Discover each lambda's contextual facts once, alongside its ABI
            // contract. Partial contracts remain predictions, never declarations.
            private void RecordParameterPredictions(
                FunctionPlan function,
                FunctionTypeDeclaration callable)
            {
                var module = callable.Parent as ModuleDeclaration ?? _module.Declaration;
                var count = Math.Min(function.Declaration.Parameters.Count, callable.Parameters.Count);
                for (var i = 0; i < count; i++)
                {
                    var declared = callable.Parameters[i].DeclaredType;
                    var flow = TypeReferenceFacts.GetFlowType(module, declared, _session.HostExports);
                    if (flow == FlowValueType.None)
                        continue;
                    TypeReferenceFacts.TryGetNativeObject(_session.HostExports, declared, out var native);
                    _module.RecordContextualParameter(function.Id, i, new ContextualParameterType(flow, native));
                }
            }

            private bool TryGetHostCallableContract(
                FunctionCallExpression call,
                FunctionPlan owner,
                out Expression argument,
                out FunctionTypeDeclaration callable,
                out bool objectArgumentsAllowNull)
            {
                argument = null;
                callable = null;
                objectArgumentsAllowNull = false;
                if (Unwrap(call.Target) is not GetPropertyExpression property ||
                    Unwrap(property.Object) is not NameExpression receiver ||
                    Unwrap(property.Property) is not NameExpression member ||
                    HasLocal(owner, receiver.Identifier.Value))
                {
                    return false;
                }

                ImportDeclaration import = null;
                for (var i = 0; i < _module.Declaration.Imports.Count; i++)
                {
                    var candidate = _module.Declaration.Imports[i];
                    if (!candidate.Include &&
                        candidate.Name != null &&
                        StringComparer.Ordinal.Equals(
                            candidate.Name.Value,
                            receiver.Identifier.Value))
                    {
                        import = candidate;
                        break;
                    }
                }
                if (import?.Module == null ||
                    !_session.HostExports.TryGetPackageTypeName(
                        import.Reference,
                        out var ownerName) ||
                    !_session.HostExports.TryGetGlobal(
                        ownerName,
                        member.Identifier.Value,
                        out var descriptor))
                {
                    return false;
                }

                string callableName = null;
                var argumentFromEnd = 0;
                for (var candidate = descriptor;
                    candidate != null;
                    candidate = candidate.NextOverload)
                {
                    if (candidate.CallableTypeName == null)
                    {
                        continue;
                    }
                    if (callableName != null &&
                        (!StringComparer.Ordinal.Equals(
                            callableName,
                            candidate.CallableTypeName) ||
                         argumentFromEnd !=
                            candidate.CallableArgumentFromEnd ||
                         objectArgumentsAllowNull !=
                            candidate.CallableObjectArgumentsAllowNull))
                    {
                        return false;
                    }
                    callableName = candidate.CallableTypeName;
                    argumentFromEnd =
                        candidate.CallableArgumentFromEnd;
                    objectArgumentsAllowNull =
                        candidate.CallableObjectArgumentsAllowNull;
                }
                var argumentIndex =
                    call.Arguments.Count - argumentFromEnd - 1;
                if (callableName == null ||
                    (uint)argumentIndex >= (uint)call.Arguments.Count ||
                    !import.Module.TryGetFunctionType(
                        callableName,
                        out callable) ||
                    callable.Access != MemberAccess.Export)
                {
                    return false;
                }
                argument = call.Arguments[argumentIndex];
                return argument != null;
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
                FunctionTypeDeclaration callable,
                bool objectArgumentsAllowNull)
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
                        expected != null &&
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
                        var expected =
                            callable.Parameters[i].DeclaredType;
                        if (function.Declaration.Parameters[i].DeclaredType == null &&
                            expected != null)
                        {
                            function.Declaration.Parameters[i].DeclaredType =
                                objectArgumentsAllowNull
                                    ? new TypeReference(
                                        expected.Qualifier,
                                        expected.Token)
                                    {
                                        AllowsNull = true
                                    }
                                    : expected;
                        }
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
                    if (callable.Parameters[i].Initializer != null ||
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
