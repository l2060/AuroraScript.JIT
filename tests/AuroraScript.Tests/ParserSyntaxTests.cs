using AuroraScript.Compiler.Analyzer;
using AuroraScript.Compiler.Ast;
using AuroraScript.Compiler.Ast.Expressions;
using AuroraScript.Source;
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
    [InlineData("try { var value = 1; }")]
    [InlineData("var array = [1, , 2, ...[3, 4]]; var map = { a: 1, b: 2, key, ...{ c: 3 } };")]
    [InlineData("var { a, b } = { a: 1, b: 2 }; var [first, ...rest, last] = [1, 2, 3];")]
    [InlineData("var expression = (a, b) => a + b; var block = () => { return 1; };")]
    [InlineData("func mutate(obj) { delete obj.value; debugger; }")]
    [InlineData("func values() { return `value=${1 + 2}`; }")]
    [InlineData("func values() { return `outer=${`inner=${1 + 2}`}`; }")]
    [InlineData("func values() { return `literal=\\${value}`; }")]
    [InlineData("func values() { return `object=${{ value: 1 }.value}`; }")]
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
            "export enum Mode { One, Two }\n";

        var module = Parse(source, workspace.Root);
        Assert.Equal(2, module.Imports.Count);
        Assert.Contains(module.Imports, import => import.Include);
        Assert.Contains(module.Imports, import => !import.Include);
    }

    [Fact]
    public void AllowsMultipleImportsAndIncludesAtModuleTop()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("first.as", "@module(FIRST);");
        workspace.WriteSource("second.as", "@module(SECOND);");
        workspace.WriteSource("included.as", "export const VALUE = 1;");

        var module = Parse(
            """
            @module(TEST);
            import first from 'first';
            include 'included';
            import second from 'second';
            export func run() { return VALUE; }
            """,
            workspace.Root);

        Assert.Equal(3, module.Imports.Count);
        Assert.Empty(module.Statements);
        Assert.Single(module.Functions);
    }

    [Theory]
    [InlineData("var value = ;")]
    [InlineData("if (true) {")]
    [InlineData("enum Broken { Value = 'text' }")]
    [InlineData("declare var value = 1;")]
    [InlineData("declare const value = 1;")]
    [InlineData("declare var { value };")]
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
    [InlineData("throw;")]
    [InlineData("delete;")]
    [InlineData("var value = { key: };")]
    [InlineData("var value = [...];")]
    [InlineData("func duplicate(value, value) { }")]
    [InlineData("func value() { return `empty=${}`; }")]
    [InlineData("func value() { return `extra=${1 2}`; }")]
    [InlineData("1 = value;")]
    [InlineData("(a + b) = 1;")]
    [InlineData("func f() { } f() = 1;")]
    [InlineData("a + b += 1;")]
    [InlineData("++1;")]
    [InlineData("target(1,,2);")]
    [InlineData("func f(a = ) { }")]
    [InlineData("func f(...rest, next) { }")]
    [InlineData("func f(...rest = 1) { }")]
    [InlineData("var value = {,};")]
    [InlineData("var value = { a: 1,, b: 2 };")]
    [InlineData("var value = { ... };")]
    [InlineData("if (true)")]
    [InlineData("else { var value = 1; }")]
    [InlineData("while (true)")]
    [InlineData("try { } catch")]
    [InlineData("try { } finally")]
    [InlineData("try { } catch () { }")]
    [InlineData("try { } catch (123) { }")]
    [InlineData("for (var i = 0 i < 3; i++) { }")]
    [InlineData("for (var item in) { }")]
    [InlineData("for (var item in [1, 2])")]
    [InlineData("enum E { A,,B }")]
    [InlineData("enum E { A = 1.5 }")]
    [InlineData("enum E { A = 2147483648 }")]
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

        var parse = Assert.IsType<AuroraCompilationException>(exception);
        Assert.Contains("top of the module", parse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ParsesGlobalDeclarationFiles()
    {
        var module = Parse(
            """
            // comments may precede the header
            @global();

            declare var HOST_VALUE;
            declare const HOST_CONST;
            declare func HOST_ADD(left, right);
            """);

        Assert.True(module.IsGlobalDeclarationFile);
        Assert.Null(module.ModuleName);
        Assert.Equal(2, module.Statements.Count);
        Assert.Single(module.Functions);
        var variable = Assert.IsType<VariableDeclaration>(module.Statements[0]);
        Assert.Equal("HOST_VALUE", variable.Name.Value);
        Assert.True(variable.IsDeclare);
        Assert.False(variable.IsConst);
        Assert.Equal(MemberAccess.Internal, variable.Access);
        Assert.Null(variable.Initializer);
        Assert.Null(variable.Pattern);
        var constant = Assert.IsType<VariableDeclaration>(module.Statements[1]);
        Assert.Equal("HOST_CONST", constant.Name.Value);
        Assert.True(constant.IsDeclare);
        Assert.True(constant.IsConst);
        var function = Assert.Single(module.Functions);
        Assert.Equal("HOST_ADD", function.Name.Value);
        Assert.Equal(FunctionFlags.Declare, function.Flags);
    }

    [Theory]
    [InlineData("@module(TEST);\ndeclare var HOST_VALUE;", "declare is only allowed")]
    [InlineData("@module(TEST);\nexport declare var HOST_VALUE;", "export declare is not supported")]
    [InlineData("@global();\nexport declare var HOST_VALUE;", "export declare is not supported")]
    [InlineData("@global();\nvar value = 1;", "only allow declare")]
    [InlineData("@global();\n@module(TEST);", "cannot also declare @module")]
    [InlineData("@module(TEST);\n@global();", "must be the first")]
    public void RejectsInvalidGlobalDeclarationUsage(string source, string message)
    {
        var parse = Assert.Throws<AuroraCompilationException>(() => Parse(source));
        Assert.Contains(message, parse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void RejectsIncludeAfterExecutableStatement()
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("dependency.as", "@module(DEPENDENCY);");
        var exception = Record.Exception(() => Parse(
            "@module(TEST); var value = 1; include 'dependency';",
            workspace.Root));

        var parse = Assert.IsType<AuroraCompilationException>(exception);
        Assert.Contains("top of the module", parse.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("@module(TEST); import from 'dependency';")]
    [InlineData("@module(TEST); import dependency 'dependency';")]
    [InlineData("@module(TEST); include dependency;")]
    public void RejectsMalformedImportOrInclude(string source)
    {
        using var workspace = new TestWorkspace();
        workspace.WriteSource("dependency.as", "@module(DEPENDENCY);");

        var exception = Record.Exception(() => Parse(source, workspace.Root));

        Assert.NotNull(exception);
        Assert.IsAssignableFrom<AuroraException>(exception);
    }

    [Fact]
    public void ParserKeepsMissingImportAsRawPath()
    {
        using var workspace = new TestWorkspace();
        var module = Parse(
            "@module(TEST); import missing from 'does-not-exist';",
            workspace.Root);

        var import = Assert.Single(module.Imports);
        Assert.Equal("does-not-exist", import.File.Value);
        Assert.Null(import.FullPath);
    }

    [Theory]
    [InlineData("@module();")]
    [InlineData("@module(TEST)")]
    [InlineData("@module(TEST, EXTRA);")]
    [InlineData("@;")]
    [InlineData("@module('TEST');")]
    public void RejectsMalformedModuleMetadata(string source)
    {
        var error = Record.Exception(() => Parse(source));
        Assert.NotNull(error);
        Assert.IsAssignableFrom<AuroraException>(error);
    }

    [Fact]
    public void ParsesNonModuleMetadata()
    {
        var module = Parse("@module(TEST);\n@author(TEST);");

        Assert.Equal("TEST", module.MetaInfos["author"]);
        Assert.Equal("TEST", module.ModuleName);
    }

    [Theory]
    [InlineData("@author(TEST);\n@module(TEST);")]
    [InlineData("var value = 1;\n@module(TEST);")]
    [InlineData(";\n@module(TEST);")]
    [InlineData("@directCall()\nfunc run() { return 1; }\n@module(TEST);")]
    public void RejectsModuleMetadataAfterOtherSyntax(string source)
    {
        var error = Record.Exception(() => Parse(source));

        Assert.NotNull(error);
        Assert.IsAssignableFrom<AuroraException>(error);
    }

    [Fact]
    public void ParsesFunctionAnnotations()
    {
        var module = Parse(
            """
            @module(TEST);
            @directCall
            export func run() { return 42; }
            """);

        var function = Assert.Single(module.Functions);
        Assert.Equal("run", function.Name.Value);
        var annotation = Assert.Single(function.Annotations);
        Assert.Equal("directCall", annotation.Name.Value);
        Assert.Empty(annotation.Arguments);
    }

    [Fact]
    public void ParsesDirectCallAnnotationArgument()
    {
        var module = Parse(
            """
            @module(TEST);
            @directCall(false)
            func run() { return 42; }
            """);

        var function = Assert.Single(module.Functions);
        var annotation = Assert.Single(function.Annotations);
        Assert.Equal("directCall", annotation.Name.Value);
        var argument = Assert.IsType<AuroraScript.Tokens.BooleanToken>(annotation.Arguments[0]);
        Assert.False(argument.BoolValue);
    }

    [Fact]
    public void DiagnosticReportsVirtualSourceCoordinates()
    {
        var root = Path.GetTempPath();
        var error = Assert.Throws<AuroraCompilationException>(() => Parse(
            "@module(TEST);\nfunc run() {\n  return 1 +;\n}",
            root));

        Assert.Contains("parser-test.as", error.FileName, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(3, error.LineNumber);
        Assert.True(error.ColumnNumber > 0);
    }

    private static ModuleDeclaration Parse(string source, string? root = null)
    {
        root ??= Path.GetTempPath();
        using var lexer = new AuroraLexer(root, new MemorySource(root, Path.Combine(root, "parser-test.as"), source));
        var parser = new AuroraParser(lexer, EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = AuroraScript.Core.ScriptSources.FileSystem(root)));
        return parser.Parse();
    }
}
