using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Statements;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Lowering;
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
            string name,
            BackendSymbolKind kind,
            BackendSymbolFlags flags,
            MemberAccess access,
            AstNode declaration,
            Type type,
            bool isParameter)
        {
            Id = id;
            Name = name;
            Kind = kind;
            Flags = flags;
            Access = access;
            Declaration = declaration;
            Type = type;
            IsParameter = isParameter;
        }

        public LocalSlotId Id { get; }
        public string Name { get; }
        public BackendSymbolKind Kind { get; }
        public BackendSymbolFlags Flags { get; }
        public MemberAccess Access { get; }
        public AstNode Declaration { get; }
        public Type Type { get; }
        public bool IsParameter { get; }
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
            UpvalueSlots = Array.Empty<UpvalueSlot>();
            CapturedLocalSlots = Array.Empty<UpvalueSlot>();
            NestedFunctions = Array.Empty<FunctionId>();
            ParameterDefaults = Array.Empty<LoweredExpression>();
            UnsupportedLoweredNodes = Array.Empty<LoweredUnsupportedNode>();
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
        public FieldInfo DirectClosureField { get; set; }
        public LocalSlot[] LocalSlots { get; set; }
        public UpvalueSlot[] UpvalueSlots { get; set; }
        public UpvalueSlot[] CapturedLocalSlots { get; set; }
        public FunctionId[] NestedFunctions { get; set; }
        public LoweredExpression[] ParameterDefaults { get; set; }
        public LoweredBlockStatement Body { get; set; }
        public int UnsupportedLoweredStatementCount { get; set; }
        public int UnsupportedLoweredExpressionCount { get; set; }
        public LoweredUnsupportedNode[] UnsupportedLoweredNodes { get; set; }
        public bool IsDirectCallCandidate { get; set; }
        public bool UsesArgumentsObject { get; set; }
        public bool HasDefaultParameters { get; set; }
        public bool RequiresClosureObject { get; set; } = true;
        public bool CanCacheClosureObject { get; set; }
        public bool IsLambda => Declaration?.Flags == FunctionFlags.Lambda;
    }

    internal sealed class ModulePlan
    {
        private readonly List<FunctionPlan> _functions = new();
        private readonly Dictionary<string, SymbolId> _symbolsByName = new(StringComparer.Ordinal);

        public ModulePlan(ModuleId id, ModuleDeclaration declaration)
        {
            Id = id;
            Declaration = declaration ?? throw new ArgumentNullException(nameof(declaration));
            Name = declaration.ModuleName;
            Path = declaration.ModulePath;
            FullPath = declaration.FullPath;
            PathHash = declaration.ModulePath?.GetHashCode() ?? 0;
            ModuleScope = ScopeId.Invalid;
        }

        public ModuleId Id { get; }
        public ModuleDeclaration Declaration { get; }
        public string Name { get; }
        public string Path { get; }
        public string FullPath { get; }
        public int PathHash { get; }
        public ScopeId ModuleScope { get; set; }
        public MethodInfo Initializer { get; set; }
        public IReadOnlyList<FunctionPlan> Functions => _functions;

        public bool TryDeclareSymbol(string name, SymbolId symbol)
        {
            return _symbolsByName.TryAdd(name, symbol);
        }

        public bool TryGetSymbol(string name, out SymbolId symbol)
        {
            return _symbolsByName.TryGetValue(name, out symbol);
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
