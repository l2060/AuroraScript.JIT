using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Backend.Binding;
using AuroraScript.Compiler.Backend.Plans;
using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading;

namespace AuroraScript.Compiler.Backend
{
    internal sealed class CompileSession
    {
        private int _nextFunctionId;
        private readonly ConcurrentDictionary<
            (string File, int Offset, string Message),
            AuroraCompilationDiagnostic> _warnings = new();

        public CompileSession(EngineOptions options, CancellationToken cancellationToken = default)
            : this(options, CompilationModeCapabilities.FromOptions(options), cancellationToken)
        {
        }

        public CompileSession(EngineOptions options, CompilationModeCapabilities capabilities, CancellationToken cancellationToken = default)
        {
            Options = options ?? throw new ArgumentNullException(nameof(options));
            CancellationToken = cancellationToken;
            Capabilities = capabilities;
            HostExports = new HostExportCatalog(
                options.Compiler.NativeTypes,
                options.Packages,
                options.Runtime);
            ClrTypes = HostExports.ClrTypes;
            Scopes = new ScopeTable();
            Symbols = new SymbolTable();
            Modules = Array.Empty<ModulePlan>();
        }

        public EngineOptions Options { get; }
        public CancellationToken CancellationToken { get; }
        public CompilationModeCapabilities Capabilities { get; }
        public HostExportCatalog HostExports { get; }
        public ClrTypeCatalog ClrTypes { get; }
        public ScopeTable Scopes { get; }
        public SymbolTable Symbols { get; }
        public ModulePlan[] Modules { get; set; }

        public void ReportWarning(AstNode node, string message)
        {
            if (node == null || string.IsNullOrEmpty(message))
            {
                return;
            }
            var range = node.Range;
            _warnings.TryAdd(
                (range.FileName ?? string.Empty, range.Offset, message),
                new AuroraCompilationDiagnostic(
                    AuroraCompilationStage.Emission,
                    message,
                    range,
                    severity: AuroraCompilationDiagnosticSeverity.Warning));
        }

        public AuroraCompilationDiagnostic[] GetWarnings()
        {
            return _warnings.Values
                .OrderBy(warning => warning.FileName, StringComparer.Ordinal)
                .ThenBy(warning => warning.LineNumber)
                .ThenBy(warning => warning.ColumnNumber)
                .ToArray();
        }

        public FunctionId AllocateFunctionId()
        {
            return new FunctionId(_nextFunctionId++);
        }
    }
}
