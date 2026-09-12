namespace AuroraScript
{
    /// <summary>
    /// Identifies whether a compilation diagnostic stops the build.
    /// </summary>
    public enum AuroraCompilationDiagnosticSeverity : byte
    {
        /// <summary>The diagnostic prevents successful compilation.</summary>
        Error,

        /// <summary>The diagnostic reports suspicious code without stopping compilation.</summary>
        Warning
    }
}
