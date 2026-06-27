using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Diagnostics;
using AuroraScript.LanguageServices.Parsing;
using AuroraScript.LanguageServices.Semantics;
using System;
using System.Linq;
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
