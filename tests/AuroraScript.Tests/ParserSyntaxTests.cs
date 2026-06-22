using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Core;
using AuroraScript.Tests.Infrastructure;
using System;
using System.IO;
using Xunit;

namespace AuroraScript.Tests;

public sealed class ParserSyntaxTests
{
    [Theory]
    [InlineData("var value = 1; const fixedValue = 2;")]
    [InlineData("func add(a, b = 2) { return a + b; } function empty() { return; }")]
    [InlineData("enum Color { Red, Green = 4, Blue }")]
    [InlineData("if (true) { var x = 1; } else if (false) { var x = 2; } else { var x = 3; }")]
    [InlineData("for (var i = 0; i < 3; i++) { if (i == 1) continue; }")]
    [InlineData("for (var item in [1, 2, 3]) { if (item == 2) break; }")]
    [InlineData("while (false) { break; }")]
    [InlineData("try { throw new Error('failure'); } catch (error) { var message = error.message; } finally { var done = true; }")]
    [InlineData("var array = [1, , 2, ...[3, 4]]; var map = { a: 1, b: 2, key, ...{ c: 3 } };")]
    [InlineData("var { a, b } = { a: 1, b: 2 }; var [first, ...rest, last] = [1, 2, 3];")]
    [InlineData("var expression = (a, b) => a + b; var block = () => { return 1; };")]
    [InlineData("func mutate(obj) { delete obj.value; debugger; yield; }")]
    [InlineData("func values() { return `value=${1 + 2}`; }")]
    [InlineData("func regex() { return /a[\\/]b+/gi; }")]
    public void ParsesSupportedGrammarBranches(string body)
    {
        var module = Parse("@module(TEST);\n" + body);
        Assert.Equal("TEST", module.ModuleName);
    }

    [Fact]
    public void ParsesImportIncludeAndExportDeclarations()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("dependency.as", "@module(DEPENDENCY); export var value = 1;");
        workspace.WriteSource("included.as", "export const INCLUDED = 2;");
        var source = "@module(TEST);\n" +
            "import dependency from 'dependency';\n" +
            "include 'included';\n" +
            "export var value = dependency.value;\n" +
            "export func run() { return value + INCLUDED; }\n" +
            "export enum Mode { One, Two }\n" +
            "export declare func HOST(value);";

        var module = Parse(source, workspace.Root);
        Assert.Equal(2, module.Imports.Count);
        Assert.Contains(module.Imports, import => import.Include);
        Assert.Contains(module.Imports, import => !import.Include);
    }

    [Theory]
    [InlineData("var value = ;")]
    [InlineData("if (true) {")]
    [InlineData("enum Broken { Value = 'text' }")]
    [InlineData("declare var value;")]
    [InlineData("export if (true) { }")]
    [InlineData("func broken( { }")]
    [InlineData("var { key } =;")]
    [InlineData("var [first] =;")]
    [InlineData("var value = 1 +;")]
    [InlineData("var value = array[];")]
    [InlineData("func call() { target(,); }")]
    [InlineData("func value() { return `missing=${1 + 2`; }")]
    [InlineData("if () { var value = 1; }")]
    [InlineData("while () { break; }")]
    [InlineData("for (var i = 0; ; ) ;")]
    [InlineData("try { var value = 1; }")]
    [InlineData("throw;")]
    [InlineData("delete;")]
    [InlineData("var value = { key: };")]
    [InlineData("var value = [...];")]
    [InlineData("func duplicate(value, value) { }")]
    public void RejectsInvalidSyntax(string body)
    {
        var exception = Record.Exception(() => Parse("@module(TEST);\n" + body));
        Assert.NotNull(exception);
        Assert.IsAssignableFrom<AuroraException>(exception);
    }

    [Fact]
    public void RejectsImportAfterExecutableStatement()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("dependency.as", "@module(DEPENDENCY);");
        var exception = Record.Exception(() => Parse(
            "@module(TEST); var value = 1; import dependency from 'dependency';",
            workspace.Root));

        var parse = Assert.IsType<AuroraParseException>(exception);
        Assert.Contains("top of the module", parse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsMissingImportFile()
    {
        using var workspace = new TestWorkspace();
        var exception = Record.Exception(() => Parse(
            "@module(TEST); import missing from 'does-not-exist';",
            workspace.Root));

        Assert.IsType<AuroraEmitException>(exception);
    }

    [Theory]
    [InlineData("@module();")]
    [InlineData("@module(TEST)")]
    [InlineData("@module(TEST, EXTRA);")]
    [InlineData("@;")]
    public void RejectsMalformedModuleMetadata(string source)
    {
        var error = Record.Exception(() => Parse(source));
        Assert.NotNull(error);
        Assert.IsAssignableFrom<AuroraException>(error);
    }

    [Fact]
    public void DiagnosticReportsVirtualSourceCoordinates()
    {
        var root = Path.GetTempPath();
        var error = Assert.Throws<AuroraParseException>(() => Parse(
            "@module(TEST);\nfunc run() {\n  return 1 +;\n}",
            root));

        Assert.Contains("parser-test.as", error.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, error.LineNumber);
        Assert.True(error.ColumnNumber > 0);
    }

    private static ModuleDeclaration Parse(string source, string? root = null)
    {
        root ??= Path.GetTempPath();
        using var lexer = new AuroraLexer(root, new TextSource(root, Path.Combine(root, "parser-test.as"), source));
        var parser = new AuroraParser(lexer, EngineOptions.Default.WithBaseDirectory(root));
        return parser.Parse();
    }
}
