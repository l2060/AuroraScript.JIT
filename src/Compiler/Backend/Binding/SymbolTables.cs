using AuroraScript.Compiler.Ast;
using AuroraScript.Core;
using System;
using System.Collections.Generic;

namespace AuroraScript.Compiler.Backend.Binding
{
    internal enum BackendScopeKind
    {
        Module,
        Function,
        Block,
        Catch,
        CompileBlock
    }

    internal enum BackendSymbolKind
    {
        ModuleProperty,
        ImportAlias,
        Local,
        Parameter,
        Function,
        Enum
    }

    [Flags]
    internal enum BackendSymbolFlags
    {
        None = 0,
        Const = 1 << 0,
        Exported = 1 << 1,
        Imported = 1 << 2,
        Assigned = 1 << 3,
        Captured = 1 << 4,
        Escaped = 1 << 5,
        ModuleVisible = 1 << 6,
        InternalOnly = 1 << 7,
        DeclaredOnly = 1 << 8
    }

    internal readonly struct ScopeInfo
    {
        public ScopeInfo(ScopeId parent, ModuleId module, FunctionId function, BackendScopeKind kind)
        {
            Parent = parent;
            Module = module;
            Function = function;
            Kind = kind;
            FirstSymbol = SymbolId.Invalid;
            SymbolCount = 0;
        }

        private ScopeInfo(
            ScopeId parent,
            ModuleId module,
            FunctionId function,
            BackendScopeKind kind,
            SymbolId firstSymbol,
            int symbolCount)
        {
            Parent = parent;
            Module = module;
            Function = function;
            Kind = kind;
            FirstSymbol = firstSymbol;
            SymbolCount = symbolCount;
        }

        public ScopeId Parent { get; }
        public ModuleId Module { get; }
        public FunctionId Function { get; }
        public BackendScopeKind Kind { get; }
        public SymbolId FirstSymbol { get; }
        public int SymbolCount { get; }

        public ScopeInfo WithSymbolRange(SymbolId firstSymbol, int symbolCount)
        {
            return new ScopeInfo(Parent, Module, Function, Kind, firstSymbol, symbolCount);
        }
    }

    internal readonly struct SymbolInfo
    {
        public SymbolInfo(
            string name,
            BackendSymbolKind kind,
            BackendSymbolFlags flags,
            ScopeId scope,
            ModuleId module,
            FunctionId function,
            MemberAccess access,
            AstNode declaration)
        {
            Name = name;
            Kind = kind;
            Flags = flags;
            Scope = scope;
            Module = module;
            Function = function;
            Access = access;
            Declaration = declaration;
        }

        public string Name { get; }
        public BackendSymbolKind Kind { get; }
        public BackendSymbolFlags Flags { get; }
        public ScopeId Scope { get; }
        public ModuleId Module { get; }
        public FunctionId Function { get; }
        public MemberAccess Access { get; }
        public AstNode Declaration { get; }

        public bool HasFlag(BackendSymbolFlags flag) => (Flags & flag) != 0;

        public SymbolInfo WithFlags(BackendSymbolFlags flags)
        {
            return new SymbolInfo(Name, Kind, flags, Scope, Module, Function, Access, Declaration);
        }
    }

    internal sealed class ScopeTable
    {
        private readonly List<ScopeInfo> _scopes = new();

        public int Count => _scopes.Count;

        public ScopeInfo this[ScopeId id]
        {
            get => _scopes[id.Value];
            set => _scopes[id.Value] = value;
        }

        public ScopeId Add(ScopeInfo scope)
        {
            var id = new ScopeId(_scopes.Count);
            _scopes.Add(scope);
            return id;
        }

        public ScopeInfo[] ToArray() => _scopes.ToArray();
    }

    internal sealed class SymbolTable
    {
        private readonly List<SymbolInfo> _symbols = new();

        public int Count => _symbols.Count;

        public SymbolInfo this[SymbolId id]
        {
            get => _symbols[id.Value];
            set => _symbols[id.Value] = value;
        }

        public SymbolId Add(SymbolInfo symbol)
        {
            var id = new SymbolId(_symbols.Count);
            _symbols.Add(symbol);
            return id;
        }

        public SymbolInfo[] ToArray() => _symbols.ToArray();
    }
}
