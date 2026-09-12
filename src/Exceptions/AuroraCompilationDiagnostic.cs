using AuroraScript.Compiler;
using System;

namespace AuroraScript
{
    /// <summary>
    /// Represents one script compilation diagnostic.
    /// </summary>
    public sealed class AuroraCompilationDiagnostic
    {
        internal AuroraCompilationDiagnostic(
            AuroraCompilationStage stage,
            string message,
            SourceSpan location,
            Exception exception = null,
            AuroraCompilationDiagnosticSeverity severity =
                AuroraCompilationDiagnosticSeverity.Error)
        {
            Stage = stage;
            Message = message ?? string.Empty;
            Location = location;
            OriginalException = exception;
            Severity = severity;
        }

        internal AuroraCompilationStage Stage { get; }

        internal Exception OriginalException { get; }

        /// <summary>Gets whether this diagnostic is an error or a warning.</summary>
        public AuroraCompilationDiagnosticSeverity Severity { get; }

        /// <summary>Human-readable diagnostic message.</summary>
        public string Message { get; }

        /// <summary>Source location associated with the diagnostic, or <see cref="SourceSpan.None"/>.</summary>
        public SourceSpan Location { get; }

        /// <summary>Source file path associated with the diagnostic.</summary>
        public string FileName => Location.FileName;

        /// <summary>1-based line number, or -1 when no source location is available.</summary>
        public int LineNumber => Location.StartLine;

        /// <summary>1-based column number, or 0 when no source location is available.</summary>
        public int ColumnNumber => Location.StartColumn;

        /// <summary>Returns a formatted diagnostic line.</summary>
        public override string ToString()
        {
            var location = Location.StartLine > 0
                ? $" Location: {Location.FileName} line:{Location.StartLine}, column:{Location.StartColumn}"
                : string.Empty;
            var prefix = Severity == AuroraCompilationDiagnosticSeverity.Warning
                ? "warning: "
                : string.Empty;
            return $"{prefix}{Message}{location}";
        }
    }
}
