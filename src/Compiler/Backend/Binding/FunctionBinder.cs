using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Analysis;
using AuroraScript.Compiler.Backend.Code;
using AuroraScript.Compiler.Backend.Plans;
using AuroraScript.Compiler.Backend.Traversal;
using AuroraScript.Runtime;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Binding
{
    internal static class FunctionBinder
    {
        public static FunctionPlanRegistry RegisterNestedFunctions(CompileSession session, ModulePlan modulePlan)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(modulePlan);

            var functions = new FunctionPlanRegistry(modulePlan);
            RegisterModuleInitializerFunctions(session, modulePlan, functions);
            return functions;
        }

        public static void BindFunctionBodies(
            CompileSession session,
            ModulePlan modulePlan,
            FunctionPlanRegistry functions)
        {
            ArgumentNullException.ThrowIfNull(session);
            ArgumentNullException.ThrowIfNull(modulePlan);
            ArgumentNullException.ThrowIfNull(functions);

            for (var i = 0; i < modulePlan.Functions.Count; i++)
            {
                BindFunction(session, modulePlan, modulePlan.Functions[i], functions);
            }
        }

        private static void RegisterModuleInitializerFunctions(
            CompileSession session,
            ModulePlan modulePlan,
            FunctionPlanRegistry functions)
        {
            var collector = new ModuleInitializerFunctionCollector(session, modulePlan, functions);
            for (var i = 0; i < modulePlan.Declaration.Statements.Count; i++)
            {
                collector.Visit(modulePlan.Declaration.Statements[i]);
            }
        }

        private static void BindFunction(
            CompileSession session,
            ModulePlan modulePlan,
            FunctionPlan function,
            FunctionPlanRegistry functions)
        {
            if (function.Declaration == null)
            {
                return;
            }

            var binder = new FunctionBodyBinder(session, modulePlan, function, functions);
            binder.Bind();
        }

        internal sealed class FunctionPlanRegistry
        {
            private readonly ModulePlan _modulePlan;
            private Dictionary<FunctionDeclaration, FunctionPlan> _map;

            public FunctionPlanRegistry(ModulePlan modulePlan)
            {
                _modulePlan = modulePlan ?? throw new ArgumentNullException(nameof(modulePlan));
            }

            public bool TryGetValue(FunctionDeclaration declaration, out FunctionPlan function)
            {
                if (declaration == null)
                {
                    function = null;
                    return false;
                }

                return EnsureMap().TryGetValue(declaration, out function);
            }

            public void Add(FunctionDeclaration declaration, FunctionPlan function)
            {
                EnsureMap().Add(declaration, function);
            }

            private Dictionary<FunctionDeclaration, FunctionPlan> EnsureMap()
            {
                if (_map != null)
                {
                    return _map;
                }

                var map = new Dictionary<FunctionDeclaration, FunctionPlan>(
                    Math.Max(4, _modulePlan.Functions.Count),
                    ReferenceEqualityComparer.Instance);
                for (var i = 0; i < _modulePlan.Functions.Count; i++)
                {
                    var function = _modulePlan.Functions[i];
                    if (function.Declaration != null)
                    {
                        map[function.Declaration] = function;
                    }
                }

                _map = map;
                return map;
            }
        }

        private sealed class ModuleInitializerFunctionCollector
        {
            private readonly CompileSession _session;
            private readonly ModulePlan _modulePlan;
            private readonly FunctionPlanRegistry _functions;

            public ModuleInitializerFunctionCollector(
                CompileSession session,
                ModulePlan modulePlan,
                FunctionPlanRegistry functions)
            {
                _session = session;
                _modulePlan = modulePlan;
                _functions = functions;
            }

            public void Visit(AstNode node)
            {
                if (node == null)
                {
                    return;
                }

                switch (node)
                {
                    case FunctionDeclaration function:
                        Register(function);
                        return;
                    case LambdaExpression lambda:
                        Register(lambda.Function);
                        return;
                }

                var visitor = new ModuleInitializerChildVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private void Register(FunctionDeclaration declaration)
            {
                if (declaration == null ||
                    declaration.Flags == FunctionFlags.Declare ||
                    _functions.TryGetValue(declaration, out _))
                {
                    return;
                }

                var functionId = _session.AllocateFunctionId();
                var functionScope = _session.Scopes.Add(new ScopeInfo(
                    _modulePlan.ModuleScope,
                    _modulePlan.Id,
                    functionId,
                    BackendScopeKind.Function));
                var plan = new FunctionPlan(functionId, _modulePlan.Id, functionScope, declaration, FunctionVisibility.InternalOnly, isModuleFunction: false);
                _modulePlan.AddFunction(plan);
                _functions.Add(declaration, plan);
            }
        }

        private sealed class FunctionBodyBinder
        {
            private readonly CompileSession _session;
            private readonly ModulePlan _modulePlan;
            private readonly FunctionPlan _function;
            private readonly FunctionPlanRegistry _functions;
            private LocalSlotBuilder _localSlots;
            private List<LocalScope> _localScopes;
            private Dictionary<AstNode, int> _localScopeByNode;
            private Stack<int> _scopeStack;
            private List<FunctionId> _nestedFunctions;

            public FunctionBodyBinder(
                CompileSession session,
                ModulePlan modulePlan,
                FunctionPlan function,
                FunctionPlanRegistry functions)
            {
                _session = session;
                _modulePlan = modulePlan;
                _function = function;
                _functions = functions;
                _localSlots = new LocalSlotBuilder(Math.Max(4, function.Declaration?.Parameters.Count ?? 0));
                _localScopes = new List<LocalScope>();
                _localScopeByNode = new Dictionary<AstNode, int>(ReferenceEqualityComparer.Instance);
                _scopeStack = new Stack<int>();
            }

            public void Bind()
            {
                var declaration = _function.Declaration;
                var rootScope = AddLocalScope(-1, declaration.Body ?? declaration);
                _scopeStack.Push(rootScope);
                try
                {
                    if (declaration.Name != null && declaration.Flags == FunctionFlags.Lambda)
                    {
                        DeclareLocal(declaration.Name.Value, BackendSymbolKind.Local, declaration.Access, declaration, false);
                    }

                    for (var i = 0; i < declaration.Parameters.Count; i++)
                    {
                        var parameter = declaration.Parameters[i];
                        if (parameter.Initializer != null)
                        {
                            _function.HasDefaultParameters = true;
                        }
                        if (parameter.Name != null)
                        {
                            DeclareLocal(parameter.Name.Value, BackendSymbolKind.Parameter, MemberAccess.Internal, parameter, true);
                        }
                    }

                    CollectDeclarations(declaration.Body);
                    DeclareUsedContexts(declaration.Body);
                    ValidateNativeContract(declaration);
                }
                finally
                {
                    _scopeStack.Pop();
                }

                _function.LocalScopes = _localScopes.ToArray();
                _function.LocalScopeByNode = _localScopeByNode;
                _function.LocalSlots = _localSlots.ToArray();
                if (_nestedFunctions != null)
                {
                    _function.NestedFunctions = _nestedFunctions.ToArray();
                }
                BindImportedNativeCalls(declaration.Body);
            }

            private void BindImportedNativeCalls(AstNode node)
            {
                if (node == null ||
                    node is FunctionDeclaration or LambdaExpression)
                {
                    return;
                }

                if (node is FunctionCallExpression call)
                {
                    TryBindImportedNativeCall(call);
                }
                if (node is GetPropertyExpression property)
                {
                    TryBindCompileTimeProperty(property);
                }

                var visitor = new ImportedNativeCallVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private void TryBindCompileTimeProperty(GetPropertyExpression property)
            {
                if (!_session.Options.Optimization.EnableModuleConstInlining)
                {
                    return;
                }
                var root = property.Object;
                while (root is GetPropertyExpression parent)
                {
                    root = parent.Object;
                }
                if (root is NameExpression owner &&
                    HasLocal(owner.Identifier?.Value))
                {
                    return;
                }

                if (ModuleConstInliningAnalyzer.TryResolvePropertyConstant(
                        _session,
                        _modulePlan,
                        property,
                        out var value))
                {
                    _function.CompileTimeProperties[property] = value;
                }
            }

            private void TryBindImportedNativeCall(
                FunctionCallExpression call)
            {
                if (call.Target is not GetPropertyExpression
                    {
                        Object: NameExpression owner,
                        Property: NameExpression member
                    } ||
                    HasSpreadArgument(call) ||
                    HasLocal(owner.Identifier.Value))
                {
                    return;
                }

                ImportDeclaration import = null;
                for (var i = 0; i < _modulePlan.Declaration.Imports.Count; i++)
                {
                    var candidate = _modulePlan.Declaration.Imports[i];
                    if (!candidate.Include &&
                        candidate.Name?.Value == owner.Identifier.Value)
                    {
                        import = candidate;
                        break;
                    }
                }
                if (import?.Module == null)
                {
                    return;
                }

                for (var moduleIndex = 0;
                    moduleIndex < _session.Modules.Length;
                    moduleIndex++)
                {
                    var module = _session.Modules[moduleIndex];
                    if (!ReferenceEquals(
                        module.Declaration,
                        import.Module))
                    {
                        continue;
                    }
                    for (var functionIndex = 0;
                        functionIndex < module.Functions.Count;
                        functionIndex++)
                    {
                        var target = module.Functions[functionIndex];
                        if (target.IsNativeDeclared &&
                            target.Visibility ==
                                FunctionVisibility.Exported &&
                            target.Name == member.Identifier.Value)
                        {
                            _function.ImportedNativeCalls[call] = target;
                            return;
                        }
                    }
                    return;
                }
            }

            private bool HasLocal(string name)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return false;
                }
                for (var i = 0; i < _function.LocalSlots.Length; i++)
                {
                    if (string.Equals(
                        _function.LocalSlots[i].Name,
                        name,
                        StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
                return false;
            }

            private static bool HasSpreadArgument(
                FunctionCallExpression call)
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

            private readonly struct ImportedNativeCallVisitor :
                IAstChildVisitor
            {
                private readonly FunctionBodyBinder _owner;

                public ImportedNativeCallVisitor(
                    FunctionBodyBinder owner)
                {
                    _owner = owner;
                }

                public void Visit(AstNode node)
                {
                    _owner.BindImportedNativeCalls(node);
                }
            }

            private void ValidateNativeContract(FunctionDeclaration declaration)
            {
                if (!declaration.IsNative)
                {
                    return;
                }

                if (declaration.ReturnType == null)
                {
                    throw new AuroraCompilationException(
                        AuroraCompilationStage.Binding,
                        declaration,
                        $"Native function '{declaration.Name.Value}' requires a declared return type.");
                }
                if (TypeReferenceFacts.IsVoid(declaration.ReturnType))
                {
                    NativeVoidReturnValidator.Validate(declaration);
                }
                ValidateNativeDefaults(declaration);
                for (var i = 0; i < declaration.Parameters.Count; i++)
                {
                    var parameter = declaration.Parameters[i];
                    if (parameter.IsSpreadOperator)
                    {
                        throw new AuroraCompilationException(
                            AuroraCompilationStage.Binding,
                            parameter,
                            $"Native function '{declaration.Name.Value}' cannot declare spread parameters.");
                    }
                }
            }

            private sealed class NativeVoidReturnValidator
            {
                private readonly FunctionDeclaration _function;

                private NativeVoidReturnValidator(FunctionDeclaration function)
                {
                    _function = function;
                }

                public static void Validate(FunctionDeclaration function)
                {
                    new NativeVoidReturnValidator(function).Visit(function.Body);
                }

                private void Visit(AstNode node)
                {
                    if (node == null ||
                        (node is FunctionDeclaration nested &&
                            !ReferenceEquals(nested, _function)) ||
                        node is LambdaExpression)
                    {
                        return;
                    }
                    if (node is ReturnStatement { Expression: not null } statement)
                    {
                        throw new AuroraCompilationException(
                            AuroraCompilationStage.Binding,
                            statement,
                            $"Native void function '{_function.Name.Value}' cannot return a value.");
                    }
                    var visitor = new ChildVisitor(this);
                    AstTraversal.VisitChildren(node, ref visitor);
                }

                private readonly struct ChildVisitor : IAstChildVisitor
                {
                    private readonly NativeVoidReturnValidator _owner;

                    public ChildVisitor(NativeVoidReturnValidator owner)
                    {
                        _owner = owner;
                    }

                    public void Visit(AstNode node)
                    {
                        _owner.Visit(node);
                    }
                }
            }

            private void ValidateNativeDefaults(
                FunctionDeclaration declaration)
            {
                var sawDefault = false;
                for (var i = 0; i < declaration.Parameters.Count; i++)
                {
                    var parameter = declaration.Parameters[i];
                    if (parameter.Initializer == null)
                    {
                        if (sawDefault)
                        {
                            throw new AuroraCompilationException(
                                AuroraCompilationStage.Binding,
                                parameter,
                                $"Native function '{declaration.Name.Value}' default parameters must be trailing.");
                        }
                        continue;
                    }

                    sawDefault = true;
                    if (!ModuleConstInliningAnalyzer.TryEvaluateConstant(
                            _session,
                            _modulePlan,
                            parameter.Initializer,
                            out var value) ||
                        value.Kind is not (
                            ValueKind.Null or
                            ValueKind.Boolean or
                            ValueKind.Number or
                            ValueKind.String))
                    {
                        throw new AuroraCompilationException(
                            AuroraCompilationStage.Binding,
                            parameter.Initializer,
                            $"Native function '{declaration.Name.Value}' default for parameter '{parameter.Name?.Value}' must be a compile-time primitive constant.");
                    }

                    if (!MatchesDeclaredDefaultType(
                            parameter.DeclaredType?.Name,
                            value.Kind))
                    {
                        throw new AuroraCompilationException(
                            AuroraCompilationStage.Binding,
                            parameter.Initializer,
                            $"Native function '{declaration.Name.Value}' default type '{GetDefaultTypeName(value.Kind)}' does not match declared type '{parameter.DeclaredType.Name}'.");
                    }

                    parameter.Initializer =
                        ModuleConstInliningAnalyzer.CreateLiteralExpression(
                            value,
                            parameter.Initializer.Range);
                    parameter.Initializer.Parent = parameter;
                }
            }

            private static bool MatchesDeclaredDefaultType(
                string declaredType,
                ValueKind valueKind)
            {
                if (declaredType == null)
                {
                    return true;
                }

                return (declaredType, valueKind) switch
                {
                    ("Null", ValueKind.Null) => true,
                    ("Boolean", ValueKind.Boolean) => true,
                    ("Number", ValueKind.Number) => true,
                    ("String", ValueKind.String) => true,
                    _ => false
                };
            }

            private static string GetDefaultTypeName(ValueKind valueKind)
            {
                return valueKind switch
                {
                    ValueKind.Null => "Null",
                    ValueKind.Boolean => "Boolean",
                    ValueKind.Number => "Number",
                    ValueKind.String => "String",
                    _ => valueKind.ToString()
                };
            }

            private int CurrentScopeId => _scopeStack.Peek();

            private int AddLocalScope(int parentId, AstNode owner)
            {
                var scopeId = _localScopes.Count;
                _localScopes.Add(new LocalScope(scopeId, parentId, owner));
                if (owner != null && !_localScopeByNode.ContainsKey(owner))
                {
                    _localScopeByNode.Add(owner, scopeId);
                }
                return scopeId;
            }

            private void EnterLocalScope(AstNode owner)
            {
                _scopeStack.Push(AddLocalScope(CurrentScopeId, owner));
            }

            private void ExitLocalScope()
            {
                _scopeStack.Pop();
            }

            private bool IsRootBody(BlockStatement block)
            {
                return ReferenceEquals(block, _function.Declaration?.Body);
            }

            private void CollectBlock(BlockStatement block, bool createScope)
            {
                if (createScope)
                {
                    EnterLocalScope(block);
                }

                try
                {
                    for (var i = 0; i < block.Functions.Count; i++)
                    {
                        CollectDeclarations(block.Functions[i]);
                    }
                    for (var i = 0; i < block.Statements.Count; i++)
                    {
                        CollectDeclarations(block.Statements[i]);
                    }
                }
                finally
                {
                    if (createScope)
                    {
                        ExitLocalScope();
                    }
                }
            }

            public void CollectDeclarations(AstNode node)
            {
                if (node == null)
                {
                    return;
                }

                switch (node)
                {
                    case BlockStatement block:
                        CollectBlock(block, createScope: !IsRootBody(block));
                        return;
                    case VariableDeclaration variable:
                        if (variable.IsDeclare)
                        {
                            return;
                        }
                        DeclarePattern(variable);
                        CollectDeclarations(variable.Initializer);
                        return;
                    case FunctionDeclaration nested when !ReferenceEquals(nested, _function.Declaration):
                        DeclareNestedFunction(nested, declareName: true);
                        return;
                    case LambdaExpression lambda:
                        DeclareNestedFunction(lambda.Function, declareName: false);
                        return;
                    case TryStatement tryStatement:
                        CollectDeclarations(tryStatement.Body);
                        CollectCatchBlock(tryStatement);
                        CollectDeclarations(tryStatement.FinallyBody);
                        return;
                    case ObjectDestructuringPattern:
                    case ArrayDestructuringPattern:
                        return;
                }

                var visitor = new DeclarationVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private void CollectCatchBlock(TryStatement statement)
            {
                if (statement.CatchBody == null)
                {
                    return;
                }

                if (string.IsNullOrEmpty(statement.CatchVariable))
                {
                    CollectDeclarations(statement.CatchBody);
                    return;
                }

                var owner = statement.CatchBody is BlockStatement catchBlock ? (AstNode)catchBlock : statement;
                EnterLocalScope(owner);
                try
                {
                    DeclareCatchVariable(statement);
                    if (statement.CatchBody is BlockStatement block)
                    {
                        CollectBlock(block, createScope: false);
                    }
                    else
                    {
                        CollectDeclarations(statement.CatchBody);
                    }
                }
                finally
                {
                    ExitLocalScope();
                }
            }

            private void DeclareCatchVariable(TryStatement statement)
            {
                if (!string.IsNullOrEmpty(statement.CatchVariable))
                {
                    DeclareLocal(statement.CatchVariable, BackendSymbolKind.Local, MemberAccess.Internal, statement, false);
                }
            }

            private void DeclareNestedFunction(FunctionDeclaration declaration, bool declareName)
            {
                if (declaration == null || declaration.Flags == FunctionFlags.Declare)
                {
                    return;
                }

                if (declareName && declaration.Name != null)
                {
                    DeclareLocal(declaration.Name.Value, BackendSymbolKind.Local, declaration.Access, declaration, false);
                }

                var nestedPlan = EnsureNestedFunction(declaration);
                nestedPlan.ParentLocalScopeId = CurrentScopeId;
                (_nestedFunctions ??= new List<FunctionId>()).Add(nestedPlan.Id);
            }

            private FunctionPlan EnsureNestedFunction(FunctionDeclaration declaration)
            {
                if (_functions.TryGetValue(declaration, out var existing))
                {
                    return existing;
                }

                var functionId = _session.AllocateFunctionId();
                var functionScope = _session.Scopes.Add(new ScopeInfo(
                    _function.Scope,
                    _modulePlan.Id,
                    functionId,
                    BackendScopeKind.Function));
                var plan = new FunctionPlan(functionId, _modulePlan.Id, functionScope, declaration, FunctionVisibility.InternalOnly, isModuleFunction: false);
                _modulePlan.AddFunction(plan);
                _functions.Add(declaration, plan);
                return plan;
            }

            private void DeclarePattern(VariableDeclaration variable)
            {
                if (variable.IsDeclare)
                {
                    return;
                }

                if (variable.Name != null)
                {
                    DeclareLocal(variable.Name.Value, BackendSymbolKind.Local, variable.Access, variable, false);
                    return;
                }

                DeclarePattern(variable.Pattern, variable);
            }

            private void DeclarePattern(Expression pattern, VariableDeclaration declaration)
            {
                switch (pattern)
                {
                    case NameExpression name:
                        DeclareLocal(name.Identifier.Value, BackendSymbolKind.Local, declaration.Access, declaration, false);
                        return;
                    case SpreadExpression { Expression: NameExpression spreadName }:
                        DeclareLocal(spreadName.Identifier.Value, BackendSymbolKind.Local, declaration.Access, declaration, false);
                        return;
                    case ObjectDestructuringPattern objectPattern:
                        for (var i = 0; i < objectPattern.Properties.Count; i++)
                        {
                            DeclareLocal(objectPattern.Properties[i].Value, BackendSymbolKind.Local, declaration.Access, declaration, false);
                        }
                        return;
                    case ArrayDestructuringPattern arrayPattern:
                        for (var i = 0; i < arrayPattern.Elements.Count; i++)
                        {
                            DeclarePattern(arrayPattern.Elements[i], declaration);
                        }
                        return;
                }
            }

            private void DeclareLocal(string name, BackendSymbolKind kind, MemberAccess access, AstNode declaration, bool isParameter)
            {
                if (string.IsNullOrEmpty(name))
                {
                    return;
                }

                if (TryGetConflictingLocal(name, CurrentScopeId, out var existing))
                {
                    ThrowDuplicateLocal(name, existing, declaration);
                }

                var slot = new LocalSlot(
                    new LocalSlotId(_localSlots.Count),
                    CurrentScopeId,
                    name,
                    kind,
                    isParameter ? BackendSymbolFlags.None : GetLocalFlags(declaration),
                    access,
                    declaration,
                    typeof(ScriptDatum),
                    isParameter);
                _localSlots.Add(slot);
            }

            private bool TryGetConflictingLocal(string name, int scopeId, out LocalSlot slot)
            {
                if (TryGetLocalInScope(name, scopeId, out slot))
                {
                    return true;
                }

                scopeId = GetParentScopeId(scopeId);
                while (scopeId >= 0)
                {
                    if (TryGetLocalInScope(name, scopeId, out slot) &&
                        (slot.Flags & BackendSymbolFlags.Const) != 0)
                    {
                        return true;
                    }

                    scopeId = GetParentScopeId(scopeId);
                }

                slot = default;
                return false;
            }

            private bool TryGetLocalInScope(string name, int scopeId, out LocalSlot slot)
            {
                for (var i = 0; i < _localSlots.Count; i++)
                {
                    if (_localSlots[i].ScopeId == scopeId &&
                        string.Equals(_localSlots[i].Name, name, StringComparison.Ordinal))
                    {
                        slot = _localSlots[i];
                        return true;
                    }
                }

                slot = default;
                return false;
            }

            private int GetParentScopeId(int scopeId)
            {
                return (uint)scopeId < (uint)_localScopes.Count ? _localScopes[scopeId].ParentId : -1;
            }

            private static void ThrowDuplicateLocal(string name, LocalSlot existing, AstNode declaration)
            {
                var existingLocation = FormatLocation(existing.Declaration?.Range ?? SourceSpan.None);
                var scopeName = existing.ScopeId == 0 ? "function scope" : "block scope";
                throw new AuroraCompilationException(AuroraCompilationStage.Binding, 
                    declaration ?? existing.Declaration,
                    $"Duplicate declaration '{name}' in {scopeName}. Previous declaration: {existingLocation}.");
            }

            private static string FormatLocation(SourceSpan range)
            {
                if (string.IsNullOrEmpty(range.FileName))
                {
                    return $"line:{range.StartLine}, column:{range.StartColumn}";
                }

                return $"{range.FileName} line:{range.StartLine}, column:{range.StartColumn}";
            }

            private void DeclareUsedContexts(AstNode node)
            {
                if (node == null ||
                    _modulePlan.Declaration.Contexts.Count == 0)
                {
                    return;
                }

                if (node is FunctionDeclaration nested &&
                    !ReferenceEquals(nested, _function.Declaration) ||
                    node is LambdaExpression)
                {
                    return;
                }

                if (node is NameExpression name)
                {
                    var value = name.Identifier?.Value;
                    if (!string.IsNullOrEmpty(value) &&
                        _modulePlan.Declaration.TryGetContext(value, out var context) &&
                        !HasDeclaredLocal(value))
                    {
                        DeclareLocal(
                            value,
                            BackendSymbolKind.Local,
                            MemberAccess.Internal,
                            context,
                            false);
                    }
                    return;
                }

                var visitor = new ContextUseVisitor(this);
                AstTraversal.VisitChildren(node, ref visitor);
            }

            private bool HasDeclaredLocal(string name)
            {
                for (var i = 0; i < _localSlots.Count; i++)
                {
                    if (StringComparer.Ordinal.Equals(_localSlots[i].Name, name))
                    {
                        return true;
                    }
                }
                return false;
            }

            private readonly struct ContextUseVisitor : IAstChildVisitor
            {
                private readonly FunctionBodyBinder _owner;

                public ContextUseVisitor(FunctionBodyBinder owner)
                {
                    _owner = owner;
                }

                public void Visit(AstNode node)
                {
                    _owner.DeclareUsedContexts(node);
                }
            }

            private static BackendSymbolFlags GetLocalFlags(AstNode declaration)
            {
                if (declaration is VariableDeclaration { IsConst: true } ||
                    declaration is ContextDeclaration)
                {
                    return BackendSymbolFlags.Const;
                }

                return BackendSymbolFlags.None;
            }
        }

        private struct LocalSlotBuilder
        {
            private LocalSlot[] _items;

            public LocalSlotBuilder(int capacity)
            {
                _items = capacity == 0 ? Array.Empty<LocalSlot>() : new LocalSlot[capacity];
                Count = 0;
            }

            public int Count { get; private set; }

            public LocalSlot this[int index] => _items[index];

            public void Add(LocalSlot slot)
            {
                if (Count == _items.Length)
                {
                    Grow();
                }

                _items[Count++] = slot;
            }

            public LocalSlot[] ToArray()
            {
                if (Count == 0)
                {
                    return Array.Empty<LocalSlot>();
                }

                if (Count == _items.Length)
                {
                    return _items;
                }

                var result = new LocalSlot[Count];
                Array.Copy(_items, result, Count);
                return result;
            }

            private void Grow()
            {
                var newSize = _items.Length == 0 ? 4 : _items.Length * 2;
                var replacement = new LocalSlot[newSize];
                if (Count != 0)
                {
                    Array.Copy(_items, replacement, Count);
                }

                _items = replacement;
            }
        }

        private readonly struct ModuleInitializerChildVisitor : IAstChildVisitor
        {
            private readonly ModuleInitializerFunctionCollector _collector;

            public ModuleInitializerChildVisitor(ModuleInitializerFunctionCollector collector)
            {
                _collector = collector;
            }

            public void Visit(AstNode node)
            {
                _collector.Visit(node);
            }
        }

        private readonly struct DeclarationVisitor : IAstChildVisitor
        {
            private readonly FunctionBodyBinder _binder;

            public DeclarationVisitor(FunctionBodyBinder binder)
            {
                _binder = binder;
            }

            public void Visit(AstNode node)
            {
                _binder.CollectDeclarations(node);
            }
        }

    }
}
