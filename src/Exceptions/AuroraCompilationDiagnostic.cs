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
            Exception exception = null)
        {
            Stage = stage;
            Message = message ?? string.Empty;
            Location = location;
            OriginalException = exception;
        }

        internal AuroraCompilationStage Stage { get; }

        internal Exception OriginalException { get; }

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
            return $"{Message}{location}";
        }
    }
}
