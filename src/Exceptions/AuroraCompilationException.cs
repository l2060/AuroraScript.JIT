using AuroraScript.Compiler;
using AuroraScript.Compiler.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AuroraScript
{
    /// <summary>
    /// Represents one or more script compilation diagnostics.
    /// </summary>
    public sealed class AuroraCompilationException : AuroraException
    {
        /// <summary>Compilation diagnostics that caused the failure.</summary>
        public IReadOnlyList<AuroraCompilationDiagnostic> Diagnostics { get; }

        /// <summary>Number of diagnostics in this exception.</summary>
        public int Count => Diagnostics.Count;

        /// <summary>First diagnostic in this exception.</summary>
        public AuroraCompilationDiagnostic FirstDiagnostic => Diagnostics[0];

        /// <summary>Source file path of the first diagnostic.</summary>
        public string FileName => FirstDiagnostic.FileName;

        /// <summary>1-based start line of the first diagnostic, or -1 when unavailable.</summary>
        public int LineNumber => FirstDiagnostic.LineNumber;

        /// <summary>1-based start column of the first diagnostic, or 0 when unavailable.</summary>
        public int ColumnNumber => FirstDiagnostic.ColumnNumber;

        /// <summary>
        /// Initializes a new compilation exception from diagnostics.
        /// </summary>
        public AuroraCompilationException(IEnumerable<AuroraCompilationDiagnostic> diagnostics)
            : this(MaterializeDiagnostics(diagnostics))
        {
        }

        internal AuroraCompilationException(AuroraCompilationStage stage, string fileName, int lineNumber, int columnNumber, string message)
            : this(new[] { CreateDiagnostic(stage, fileName, lineNumber, columnNumber, message) })
        {
        }

        internal AuroraCompilationException(AuroraCompilationStage stage, string fileName, Token token, string message)
            : this(new[] { CreateDiagnostic(stage, fileName, token, message) })
        {
        }

        internal AuroraCompilationException(AuroraCompilationStage stage, string fileName, SourceSpan location, string message)
            : this(new[] { CreateDiagnostic(stage, WithFileName(location, fileName), message) })
        {
        }

        internal AuroraCompilationException(AuroraCompilationStage stage, Token token, string message)
            : this(new[] { CreateDiagnostic(stage, token?.Range ?? SourceSpan.None, message) })
        {
        }

        internal AuroraCompilationException(AuroraCompilationStage stage, AstNode node, string message)
            : this(new[] { CreateDiagnostic(stage, node?.Range ?? SourceSpan.None, message) })
        {
        }

        internal AuroraCompilationException(AuroraCompilationStage stage, SourceSpan location, string message)
            : this(new[] { CreateDiagnostic(stage, location, message) })
        {
        }

        private AuroraCompilationException(AuroraCompilationDiagnostic[] diagnostics)
            : base(CreateMessage(diagnostics), diagnostics.Length == 1 ? diagnostics[0].OriginalException : null)
        {
            Diagnostics = diagnostics;
        }

        internal static AuroraCompilationException FromException(Exception exception, AuroraCompilationStage fallbackStage)
        {
            return new AuroraCompilationException(CollectDiagnostics(exception, fallbackStage).ToArray());
        }

        internal static AuroraCompilationDiagnostic[] CollectDiagnostics(Exception exception, AuroraCompilationStage fallbackStage)
        {
            if (exception is AuroraCompilationException compilation)
            {
                return compilation.Diagnostics.ToArray();
            }

            if (exception is AggregateException aggregate)
            {
                var inner = aggregate.Flatten().InnerExceptions;
                var diagnostics = new List<AuroraCompilationDiagnostic>(inner.Count);
                for (var i = 0; i < inner.Count; i++)
                {
                    diagnostics.AddRange(CollectDiagnostics(inner[i], fallbackStage));
                }
                return diagnostics.ToArray();
            }

            return new[]
            {
                new AuroraCompilationDiagnostic(fallbackStage, exception?.Message ?? string.Empty, SourceSpan.None, exception)
            };
        }

        internal static AuroraCompilationDiagnostic ToDiagnostic(Exception exception, AuroraCompilationStage fallbackStage)
        {
            return CollectDiagnostics(exception, fallbackStage)[0];
        }

        /// <summary>Returns a formatted compilation failure report.</summary>
        public override string ToString()
        {
            var builder = new StringBuilder(Message);
            for (var i = 0; i < Diagnostics.Count; i++)
            {
                builder.AppendLine();
                builder.Append("  [").Append(i + 1).Append("] ").Append(Diagnostics[i]);
            }
            return builder.ToString();
        }

        private static AuroraCompilationDiagnostic[] MaterializeDiagnostics(IEnumerable<AuroraCompilationDiagnostic> diagnostics)
        {
            ArgumentNullException.ThrowIfNull(diagnostics);
            var result = diagnostics.ToArray();
            if (result.Length == 0)
            {
                throw new ArgumentException("At least one diagnostic is required.", nameof(diagnostics));
            }
            return result;
        }

        private static AuroraCompilationDiagnostic CreateDiagnostic(
            AuroraCompilationStage stage,
            string fileName,
            int lineNumber,
            int columnNumber,
            string message)
        {
            return CreateDiagnostic(
                stage,
                new SourceSpan
                {
                    FileName = fileName,
                    StartLine = lineNumber,
                    StartColumn = columnNumber,
                    EndLine = lineNumber,
                    EndColumn = columnNumber
                },
                message);
        }

        private static AuroraCompilationDiagnostic CreateDiagnostic(
            AuroraCompilationStage stage,
            string fileName,
            Token token,
            string message)
        {
            return token == null
                ? CreateDiagnostic(stage, SourceSpan.None, message)
                : CreateDiagnostic(stage, WithFileName(token.Range, fileName), message);
        }

        private static AuroraCompilationDiagnostic CreateDiagnostic(
            AuroraCompilationStage stage,
            SourceSpan location,
            string message)
        {
            return new AuroraCompilationDiagnostic(stage, message, location);
        }

        private static SourceSpan WithFileName(SourceSpan location, string fileName)
        {
            if (string.IsNullOrEmpty(location.FileName))
            {
                location.FileName = fileName;
            }
            return location;
        }

        private static string CreateMessage(IReadOnlyList<AuroraCompilationDiagnostic> diagnostics)
        {
            return diagnostics.Count == 1
                ? diagnostics[0].Message
                : $"Compilation failed with {diagnostics.Count} diagnostics";
        }
    }
}
