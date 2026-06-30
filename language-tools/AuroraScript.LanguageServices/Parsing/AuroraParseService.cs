using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.GlobalDeclarations;
using AuroraScript.Core;
using AuroraScript.LanguageServices.Diagnostics;
using AuroraScript.LanguageServices.Text;
using AuroraScript.LanguageServices.Workspace;
using AuroraScript.Source;
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

        baseDirectory ??= workspaceSnapshot?.BaseDirectory;
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            baseDirectory = Directory.GetCurrentDirectory();
        }

        var fullPath = ScriptPath.GetFullPath(baseDirectory, sourceName);
        var source = new MemorySource(baseDirectory, fullPath, sourceText);
        var options = workspaceSnapshot == null
            ? _options
            : _options.WithCompiler(compiler => compiler.WithSourceResolver(
                new WorkspaceScriptSourceResolver(workspaceSnapshot, _options.Compiler.SourceResolver)));

        try
        {
            using var lexer = new AuroraLexer(baseDirectory, source);
            var parser = new AuroraParser(lexer, options);
            var module = parser.Parse();
            var diagnostics = ResolveImports(module, source, options);
            return new AuroraParseResult(module, diagnostics);
        }
        catch (AuroraCompilationException ex)
        {
            return new AuroraParseResult(null, ConvertDiagnostics(ex));
        }
    }

    private static IReadOnlyList<LanguageDiagnostic> ResolveImports(
        ModuleDeclaration module,
        ScriptSource source,
        EngineOptions options)
    {
        if (module.Imports.Count == 0)
        {
            return Array.Empty<LanguageDiagnostic>();
        }

        var diagnostics = new List<LanguageDiagnostic>();
        var importer = new ScriptSourceReference(source.BaseDirectory, source.FullPath, source.SourcePath);
        var context = new ScriptResolveContext(options.Compiler.ExtName);
        for (var i = 0; i < module.Imports.Count; i++)
        {
            var import = module.Imports[i];
            var requestedPath = import.File?.Value;
            if (string.IsNullOrWhiteSpace(requestedPath))
            {
                continue;
            }

            var resolved = options.Compiler.SourceResolver
                .ResolveAsync(importer, requestedPath, context)
                .AsTask()
                .GetAwaiter()
                .GetResult();
            if (resolved == null)
            {
                var message = import.Include
                    ? $"include file not found: {requestedPath}"
                    : $"Import file not found: {requestedPath}";
                diagnostics.Add(new LanguageDiagnostic(
                    "AURORA-IMPORT-NOT-FOUND",
                    message,
                    TextRange.FromSourceSpan(import.File != null ? import.File.Range : import.Range),
                    LanguageDiagnosticSeverity.Error));
                continue;
            }

            import.FullPath = resolved.Value.FullPath;
            import.ModulePath = resolved.Value.ModulePath;
            import.Reference = resolved.Value;

            try
            {
                var resolvedSource = options.Compiler.SourceResolver
                    .GetSourceAsync(resolved.Value)
                    .AsTask()
                    .GetAwaiter()
                    .GetResult();
                if (GlobalDeclarationScanner.IsGlobalFile(resolvedSource.ReadSource()))
                {
                    diagnostics.Add(new LanguageDiagnostic(
                        "AURORA-GLOBAL-IMPORT",
                        import.Include
                            ? "@global() declaration files cannot be included."
                            : "@global() declaration files cannot be imported.",
                        TextRange.FromSourceSpan(import.File != null ? import.File.Range : import.Range),
                        LanguageDiagnosticSeverity.Error));
                }
            }
            catch (Exception ex) when (IsSourceReadFailure(ex))
            {
            }
        }

        return diagnostics;
    }

    private static bool IsSourceReadFailure(Exception exception)
    {
        return exception is FileNotFoundException
            or DirectoryNotFoundException
            or IOException
            or UnauthorizedAccessException
            or ArgumentException
            or NotSupportedException
            or KeyNotFoundException;
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
