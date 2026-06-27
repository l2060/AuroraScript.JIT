using AuroraScript.Compiler.Analyzer;
using AuroraScript.Core;
using AuroraScript.LanguageServices.Diagnostics;
using AuroraScript.LanguageServices.Text;
using AuroraScript.LanguageServices.Workspace;
using System;
using System.Collections.Generic;
using System.IO;

namespace AuroraScript.LanguageServices.Parsing;

public sealed class AuroraParseService
{
    private readonly EngineOptions _options;

    public AuroraParseService()
        : this(EngineOptions.Default)
    {
    }

    internal AuroraParseService(EngineOptions options)
    {
        _options = options ?? EngineOptions.Default;
    }

    internal IScriptSourceResolver SourceResolver => _options.Compiler.SourceResolver;

    public AuroraParseResult ParseText(string sourceName, string sourceText, string? baseDirectory = null)
    {
        return ParseText(sourceName, sourceText, baseDirectory, null);
    }

    internal AuroraParseResult ParseText(
        string sourceName,
        string sourceText,
        string? baseDirectory,
        AuroraWorkspaceSnapshot? workspaceSnapshot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sourceName);
        ArgumentNullException.ThrowIfNull(sourceText);

        baseDirectory ??= workspaceSnapshot?.BaseDirectory ?? _options.Compiler.BaseDirectory;
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            baseDirectory = Directory.GetCurrentDirectory();
        }

        var fullPath = ScriptPath.GetFullPath(baseDirectory, sourceName);
        var source = new MemoryScriptSource(baseDirectory, fullPath, sourceText);
        var options = workspaceSnapshot == null
            ? _options
            : _options.WithCompiler(compiler => compiler.WithSourceResolver(
                new WorkspaceScriptSourceResolver(workspaceSnapshot, _options.Compiler.SourceResolver)));

        try
        {
            using var lexer = new AuroraLexer(baseDirectory, source);
            var parser = new AuroraParser(lexer, options);
            return new AuroraParseResult(parser.Parse(), Array.Empty<LanguageDiagnostic>());
        }
        catch (AuroraCompilationException ex)
        {
            return new AuroraParseResult(null, ConvertDiagnostics(ex));
        }
    }

    private static IReadOnlyList<LanguageDiagnostic> ConvertDiagnostics(AuroraCompilationException exception)
    {
        if (exception.Diagnostics.Count == 0)
        {
            return new[]
            {
                new LanguageDiagnostic("AURORA0000", exception.Message, default, LanguageDiagnosticSeverity.Error)
            };
        }

        var diagnostics = new List<LanguageDiagnostic>(exception.Diagnostics.Count);
        for (var i = 0; i < exception.Diagnostics.Count; i++)
        {
            var diagnostic = exception.Diagnostics[i];
            diagnostics.Add(new LanguageDiagnostic(
                "AURORA-COMPILE",
                diagnostic.Message,
                TextRange.FromSourceSpan(diagnostic.Location),
                LanguageDiagnosticSeverity.Error));
        }

        return diagnostics;
    }
}
