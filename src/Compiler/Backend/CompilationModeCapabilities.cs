using System;

namespace AuroraScript.Compiler.Backend
{
    internal readonly struct CompilationModeCapabilities
    {
        public CompilationModeCapabilities(
            bool canUseModuleDirectCall,
            bool canInferAutoModuleDirectCall)
        {
            CanUseModuleDirectCall = canUseModuleDirectCall;
            CanInferAutoModuleDirectCall = canInferAutoModuleDirectCall;
        }

        public bool CanUseModuleDirectCall { get; }
        public bool CanInferAutoModuleDirectCall { get; }

        public static CompilationModeCapabilities FromOptions(EngineOptions options)
        {
            ArgumentNullException.ThrowIfNull(options);

            _ = options.Compiler.Mode switch
            {
                CompilationMode.Dynamic or CompilationMode.OnlyRun or CompilationMode.Persistence => true,
                _ => throw new ArgumentOutOfRangeException(
                    nameof(options.Compiler.Mode),
                    options.Compiler.Mode,
                    "Unsupported compilation mode.")
            };

            return new CompilationModeCapabilities(
                canUseModuleDirectCall: true,
                canInferAutoModuleDirectCall: options.Optimization.EnableAutoModuleDirectCall);
        }

        public CompilationModeCapabilities WithoutModuleDirectCall()
        {
            return CanUseModuleDirectCall
                ? new CompilationModeCapabilities(
                    canUseModuleDirectCall: false,
                    canInferAutoModuleDirectCall: false)
                : this;
        }
    }
}
