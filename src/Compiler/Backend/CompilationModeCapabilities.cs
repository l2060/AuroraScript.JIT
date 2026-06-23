using System;

namespace AuroraScript.Compiler.Backend
{
    internal readonly struct CompilationModeCapabilities
    {
        public CompilationModeCapabilities(
            CompilationMode mode,
            bool canAnalyzeModulesInParallel,
            bool canLowerModulesInParallel,
            bool canEmitModulesInParallel,
            bool requiresSerializedMetadata,
            bool requiresDeterministicPdb,
            bool canUseModuleDirectCall)
        {
            Mode = mode;
            CanAnalyzeModulesInParallel = canAnalyzeModulesInParallel;
            CanLowerModulesInParallel = canLowerModulesInParallel;
            CanEmitModulesInParallel = canEmitModulesInParallel;
            RequiresSerializedMetadata = requiresSerializedMetadata;
            RequiresDeterministicPdb = requiresDeterministicPdb;
            CanUseModuleDirectCall = canUseModuleDirectCall;
        }

        public CompilationMode Mode { get; }
        public bool CanAnalyzeModulesInParallel { get; }
        public bool CanLowerModulesInParallel { get; }
        public bool CanEmitModulesInParallel { get; }
        public bool RequiresSerializedMetadata { get; }
        public bool RequiresDeterministicPdb { get; }
        public bool CanUseModuleDirectCall { get; }

        public static CompilationModeCapabilities FromOptions(EngineOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            var canUseModuleDirectCall =
                !options.EnableHotReload &&
                options.EnableModuleDirectCall;

            return options.CompilationMode switch
            {
                CompilationMode.Dynamic => new CompilationModeCapabilities(
                    options.CompilationMode,
                    canAnalyzeModulesInParallel: true,
                    canLowerModulesInParallel: true,
                    canEmitModulesInParallel: false,
                    requiresSerializedMetadata: false,
                    requiresDeterministicPdb: false,
                    canUseModuleDirectCall),

                CompilationMode.OnlyRun => new CompilationModeCapabilities(
                    options.CompilationMode,
                    canAnalyzeModulesInParallel: true,
                    canLowerModulesInParallel: true,
                    canEmitModulesInParallel: false,
                    requiresSerializedMetadata: true,
                    requiresDeterministicPdb: false,
                    canUseModuleDirectCall),

                CompilationMode.Persistence => new CompilationModeCapabilities(
                    options.CompilationMode,
                    canAnalyzeModulesInParallel: true,
                    canLowerModulesInParallel: true,
                    canEmitModulesInParallel: false,
                    requiresSerializedMetadata: true,
                    requiresDeterministicPdb: options.OptimizeOption == OptimizeOptions.Debug,
                    canUseModuleDirectCall),

                _ => throw new NotImplementedException($"Unsupported compilation mode '{options.CompilationMode}'.")
            };
        }
    }
}
