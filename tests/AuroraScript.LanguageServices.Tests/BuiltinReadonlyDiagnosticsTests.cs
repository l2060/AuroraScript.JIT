using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Diagnostics;
using AuroraScript.LanguageServices.Parsing;
using AuroraScript.LanguageServices.Semantics;
using System;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class BuiltinReadonlyDiagnosticsTests
{
    [Fact]
    public void ReportsReadonlyBuiltinMemberAssignment()
    {
        var analysis = Analyze(
            """
            @module(TEST);
            export func run() {
                Math.PI = 1;
            }
            """);

        var diagnostic = Assert.Single(analysis.Diagnostics);
        Assert.Equal("AURORA-BUILTIN-READONLY", diagnostic.Code);
        Assert.Equal(LanguageDiagnosticSeverity.Error, diagnostic.Severity);
        Assert.Contains("Math.PI", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void ReportsReadonlyBuiltinMemberDelete()
    {
        var analysis = Analyze(
            """
            @module(TEST);
            export func run() {
                delete Math.PI;
            }
            """);

        var diagnostic = Assert.Single(analysis.Diagnostics);
        Assert.Equal("AURORA-BUILTIN-READONLY", diagnostic.Code);
        Assert.Contains("Cannot delete", diagnostic.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void DoesNotReportLocalObjectWithSameNameAsBuiltin()
    {
        var analysis = Analyze(
            """
            @module(TEST);
            export func run() {
                const Math = { PI: 1 };
                Math.PI = 2;
            }
            """);

        Assert.DoesNotContain(analysis.Diagnostics, diagnostic => diagnostic.Code == "AURORA-BUILTIN-READONLY");
    }

    [Fact]
    public void ReportsReadonlyBuiltinModuleMemberAssignment()
    {
        var catalog = BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath());
        var service = new AuroraLanguageService(catalog);
        const string source =
            """
            @module(TEST);
            import files from 'fs';
            export func run() {
                files.readText = null;
            }
            """;

        var diagnostics = service.GetDiagnostics("test.as", source);

        var diagnostic = Assert.Single(diagnostics, item => item.Code == "AURORA-BUILTIN-READONLY");
        Assert.Contains("fs.readText", diagnostic.Message, StringComparison.Ordinal);
        Assert.DoesNotContain(diagnostics, item => item.Code == "AURORA-IMPORT-NOT-FOUND");
    }

    [Fact]
    public void ImportedProjectAliasSuppressesSameNamedBuiltinGlobalDiagnostic()
    {
        var analysis = Analyze(
            """
            @module(TEST);
            import Math from './math';
            export func run() {
                Math.PI = 1;
            }
            """);

        Assert.DoesNotContain(analysis.Diagnostics, diagnostic => diagnostic.Code == "AURORA-BUILTIN-READONLY");
    }

    [Fact]
    public void ResolvesBuiltinMemberMetadata()
    {
        var catalog = BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath());
        var analyzer = new AuroraSemanticAnalyzer(catalog);

        Assert.True(analyzer.TryResolveBuiltinMember("console", "log", out var symbol));
        Assert.Equal(SemanticSymbolKind.BuiltinMember, symbol.Kind);
        Assert.Equal("console.log", symbol.Name);
        Assert.True(symbol.BuiltinMember!.Parameters[0].Variadic);
    }

    private static AuroraSemanticAnalysis Analyze(string source)
    {
        var catalog = BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath());
        var parseService = new AuroraParseService();
        var parseResult = parseService.ParseText("test.as", source, AppContext.BaseDirectory);
        var analyzer = new AuroraSemanticAnalyzer(catalog);
        return analyzer.Analyze(parseResult);
    }
}
