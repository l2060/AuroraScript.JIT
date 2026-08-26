using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Plans;
using System;
using System.Threading;

namespace AuroraScript.Compiler.Backend
{
    internal sealed class CompileSession
    {
        private int _nextFunctionId;

        public CompileSession(EngineOptions options, CancellationToken cancellationToken = default)
            : this(options, CompilationModeCapabilities.FromOptions(options), cancellationToken)
        {
        }

        public CompileSession(EngineOptions options, CompilationModeCapabilities capabilities, CancellationToken cancellationToken = default)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            CancellationToken = cancellationToken;
            Capabilities = capabilities;
            HostExports = new HostExportCatalog(options.HostExportAssemblies);
            Scopes = new ScopeTable();
            Symbols = new SymbolTable();
            Modules = Array.Empty<ModulePlan>();
        }

        public EngineOptions Options { get; }
        public CancellationToken CancellationToken { get; }
        public CompilationModeCapabilities Capabilities { get; }
        public HostExportCatalog HostExports { get; }
        public ScopeTable Scopes { get; }
        public SymbolTable Symbols { get; }
        public ModulePlan[] Modules { get; set; }

        public FunctionId AllocateFunctionId()
        {
            return new FunctionId(_nextFunctionId++);
        }
    }
}
