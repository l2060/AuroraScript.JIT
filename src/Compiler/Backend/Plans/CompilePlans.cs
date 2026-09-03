using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Core;
using AuroraScript.Runtime;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace AuroraScript.Compiler.Backend.Plans
{
    internal enum FunctionVisibility
    {
        Exported,
        ModuleVisible,
        InternalOnly
    }

    internal enum FunctionCallConvention
    {
        Span,
        Fast0,
        Fast1,
        Fast2,
        Fast3,
        Fast4,
        Fast5,
        Fast6,
        Fast7
    }

    internal readonly struct LocalSlot
    {
        public LocalSlot(
            LocalSlotId id,
            int scopeId,
            string name,
            BackendSymbolKind kind,
            BackendSymbolFlags flags,
            MemberAccess access,
            AstNode declaration,
            Type type,
            bool isParameter)
        {
            Id = id;
            ScopeId = scopeId;
            Name = name;
            Kind = kind;
            Flags = flags;
            Access = access;
            Declaration = declaration;
            Type = type;
            IsParameter = isParameter;
        }

        public LocalSlotId Id { get; }
        public int ScopeId { get; }
        public string Name { get; }
        public BackendSymbolKind Kind { get; }
        public BackendSymbolFlags Flags { get; }
        public MemberAccess Access { get; }
        public AstNode Declaration { get; }
        public Type Type { get; }
        public bool IsParameter { get; }
    }

    internal readonly struct LocalScope
    {
        public LocalScope(int id, int parentId, AstNode owner)
        {
            Id = id;
            ParentId = parentId;
            Owner = owner;
        }

        public int Id { get; }
        public int ParentId { get; }
        public AstNode Owner { get; }
    }

    internal readonly struct UpvalueSlot
    {
        public UpvalueSlot(
            UpvalueSlotId id,
            string name,
            FunctionId sourceFunction,
            LocalSlotId sourceLocal,
            UpvalueSlotId sourceUpvalue,
            bool isInherited)
        {
            Id = id;
            Name = name;
            SourceFunction = sourceFunction;
            SourceLocal = sourceLocal;
            SourceUpvalue = sourceUpvalue;
            IsInherited = isInherited;
        }

        public UpvalueSlotId Id { get; }
        public string Name { get; }
        public FunctionId SourceFunction { get; }
        public LocalSlotId SourceLocal { get; }
        public UpvalueSlotId SourceUpvalue { get; }
        public bool IsInherited { get; }
    }

    internal sealed class FunctionPlan
    {
        public FunctionPlan(
            FunctionId id,
            ModuleId module,
            ScopeId scope,
            FunctionDeclaration declaration,
            FunctionVisibility visibility,
            bool isModuleFunction)
        {
            Id = id;
            Module = module;
            Scope = scope;
            Declaration = declaration;
            Visibility = visibility;
            IsModuleFunction = isModuleFunction;
            Name = declaration?.Name?.Value;
            CallConvention = FunctionCallConvention.Span;
            LocalSlots = Array.Empty<LocalSlot>();
            LocalScopes = Array.Empty<LocalScope>();
            UpvalueSlots = Array.Empty<UpvalueSlot>();
            CapturedLocalSlots = Array.Empty<UpvalueSlot>();
            NestedFunctions = Array.Empty<FunctionId>();
            ParentLocalScopeId = -1;
            ImportedNativeCalls =
                new Dictionary<FunctionCallExpression, FunctionPlan>(
                    ReferenceEqualityComparer.Instance);
            CompileTimeProperties =
                new Dictionary<GetPropertyExpression, ScriptDatum>(
                    ReferenceEqualityComparer.Instance);
        }

        public FunctionId Id { get; }
        public ModuleId Module { get; }
        public ScopeId Scope { get; }
        public FunctionDeclaration Declaration { get; }
        public string Name { get; }
        public bool IsModuleFunction { get; }
        public FunctionVisibility Visibility { get; set; }
        public FunctionCallConvention CallConvention { get; set; }
        public MethodInfo Method { get; set; }
        public int DynamicDelegateId { get; set; }
        public LocalSlot[] LocalSlots { get; set; }
        public LocalScope[] LocalScopes { get; set; }
        public Dictionary<AstNode, int> LocalScopeByNode { get; set; }
        public UpvalueSlot[] UpvalueSlots { get; set; }
        public UpvalueSlot[] CapturedLocalSlots { get; set; }
        public FunctionId[] NestedFunctions { get; set; }
        public bool IsDirectCallCandidate { get; set; }
        public bool HasDefaultParameters { get; set; }
        public bool RequiresClosureObject { get; set; } = true;
        public bool CanCacheClosureObject { get; set; }
        public int ParentLocalScopeId { get; set; }
        public Dictionary<FunctionCallExpression, FunctionPlan>
            ImportedNativeCalls { get; }
        public Dictionary<GetPropertyExpression, ScriptDatum>
            CompileTimeProperties { get; }
        public bool IsLambda => Declaration?.Flags == FunctionFlags.Lambda;
        public bool IsNativeDeclared => Declaration?.IsNative == true;
        public MethodInfo NativeEntryMethod { get; set; }

    }

    internal sealed class ModulePlan
    {
        private readonly List<FunctionPlan> _functions;
        private readonly Dictionary<string, SymbolId> _symbolsByName;
        private Dictionary<SymbolId, ScriptDatum> _inlineConstants;
        private HashSet<string> _declaredOnlyNames;

        public ModulePlan(ModuleId id, ModuleDeclaration declaration)
        {
            Id = id;
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
            _functions = new List<FunctionPlan>(Math.Max(4, declaration.Functions.Count));
            _symbolsByName = new Dictionary<string, SymbolId>(
                Math.Max(4, declaration.Imports.Count + declaration.Statements.Count + declaration.Functions.Count),
                StringComparer.Ordinal);
            Name = declaration.ModuleName;
            Source = declaration.Source;
            PathHash = Source.FullPath.GetHashCode();
            ModuleScope = ScopeId.Invalid;
        }

        public ModuleId Id { get; }
        public ModuleDeclaration Declaration { get; }
        public string Name { get; }
        public ScriptSourceReference Source { get; }
        public int PathHash { get; }
        public ScopeId ModuleScope { get; set; }
        public MethodInfo Initializer { get; set; }
        public List<FunctionPlan> Functions => _functions;
        public bool HasInlineConstants => _inlineConstants != null && _inlineConstants.Count != 0;

        public bool TryDeclareSymbol(string name, SymbolId symbol)
        {
            return _symbolsByName.TryAdd(name, symbol);
        }

        public bool TryGetSymbol(string name, out SymbolId symbol)
        {
            return _symbolsByName.TryGetValue(name, out symbol);
        }

        public void MarkDeclaredOnly(string name)
        {
            _declaredOnlyNames ??= new HashSet<string>(StringComparer.Ordinal);
            _declaredOnlyNames.Add(name);
        }

        public bool IsDeclaredOnly(string name)
        {
            return !string.IsNullOrEmpty(name) &&
                _declaredOnlyNames != null &&
                _declaredOnlyNames.Contains(name);
        }

        public void SetInlineConstant(SymbolId symbol, ScriptDatum value)
        {
            _inlineConstants ??= new Dictionary<SymbolId, ScriptDatum>();
            _inlineConstants[symbol] = value;
        }

        public bool TryGetInlineConstant(SymbolId symbol, out ScriptDatum value)
        {
            if (_inlineConstants != null)
            {
                return _inlineConstants.TryGetValue(symbol, out value);
            }

            value = default;
            return false;
        }

        public void AddFunction(FunctionPlan function)
        {
            if (!function.Module.Equals(Id))
            {
                throw new ArgumentException("Function belongs to a different module.", nameof(function));
            }
            _functions.Add(function);
        }
    }

    internal sealed class CompileBlockPlan
    {
        public CompileBlockPlan(BlockStatement body, IReadOnlyList<string> parameters, string sourceName)
        {
            Body = body ?? throw new ArgumentNullException(nameof(body));
            Parameters = parameters ?? Array.Empty<string>();
            SourceName = sourceName;
        }

        public BlockStatement Body { get; }
        public IReadOnlyList<string> Parameters { get; }
        public string SourceName { get; }
        public CompileSession Session { get; set; }
        public ModulePlan Module { get; set; }
        public FunctionPlan Function { get; set; }
    }
}
