using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.Completion;
using AuroraScript.LanguageServices.Text;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace AuroraScript.LanguageServices.Tests;

public sealed class BuiltinLanguageFeatureTests
{
    [Fact]
    public void HoverReturnsBuiltinMemberDocumentation()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                return Math.abs(-1);
            }
            """;
        var service = CreateService();

        var hover = service.GetHover("test.as", source, PositionOf(source, "abs"));

        Assert.NotNull(hover);
        Assert.Contains("declare type Math", hover!.Contents, StringComparison.Ordinal);
        Assert.Contains("```aurorascript", hover.Contents, StringComparison.Ordinal);
        Assert.Contains("static func abs(Number value) Number", hover.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void HoverReturnsBuiltinOwnerDocumentationForMemberAccess()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                console.log("x");
            }
            """;
        var service = CreateService();

        var hover = service.GetHover("test.as", source, PositionOf(source, "console"));

        Assert.NotNull(hover);
        Assert.Contains("```aurorascript", hover!.Contents, StringComparison.Ordinal);
        Assert.Contains("declare type console;", hover.Contents, StringComparison.Ordinal);
        Assert.Contains("Console logging and timing API.", hover.Contents, StringComparison.Ordinal);
        Assert.DoesNotContain("console.log", hover.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void HoverReturnsBuiltinMemberDeclarationDocumentation()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                console.log("x");
            }
            """;
        var service = CreateService();

        var hover = service.GetHover("test.as", source, PositionOf(source, "log"));

        Assert.NotNull(hover);
        Assert.Contains("```aurorascript", hover!.Contents, StringComparison.Ordinal);
        Assert.Contains("static func log(...Object values) void", hover.Contents, StringComparison.Ordinal);
        Assert.Contains("Writes values to standard output.", hover.Contents, StringComparison.Ordinal);
        Assert.DoesNotContain("readonly func", hover.Contents, StringComparison.Ordinal);
        Assert.DoesNotContain("any[]): null", hover.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void HoverReturnsLocalizedBuiltinDocumentation()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                return String.fromCharCode(65);
            }
            """;
        var english = CreateService("en-US");
        var chinese = CreateService("zh-CN");

        var englishHover = english.GetHover("test.as", source, PositionOf(source, "fromCharCode"));
        var chineseHover = chinese.GetHover("test.as", source, PositionOf(source, "fromCharCode"));

        Assert.NotNull(englishHover);
        Assert.Contains("Creates a one-character string", englishHover!.Contents, StringComparison.Ordinal);
        Assert.NotNull(chineseHover);
        Assert.Contains("根据字符编码创建单字符字符串", chineseHover!.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void HoverReturnsScriptFunctionLeadingComments()
    {
        const string source =
            """
            @module(TEST);
            // Adds two values.
            // Used by callers.
            export func add(left, right) {
                return left + right;
            }
            """;
        var service = CreateService();

        var hover = service.GetHover("test.as", source, PositionOf(source, "add"));

        Assert.NotNull(hover);
        Assert.Contains("func add(left, right)", hover!.Contents, StringComparison.Ordinal);
        Assert.Contains("Adds two values.", hover.Contents, StringComparison.Ordinal);
        Assert.Contains("Used by callers.", hover.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void HoverReturnsScriptFunctionCommentsAtCallSite()
    {
        const string source =
            """
            @module(TEST);
            // Adds two values.
            export func add(left, right) {
                return left + right;
            }
            export func run() {
                return add(1, 2);
            }
            """;
        var service = CreateService();

        var hover = service.GetHover("test.as", source, PositionOfLast(source, "add"));

        Assert.NotNull(hover);
        Assert.Contains("```aurorascript", hover!.Contents, StringComparison.Ordinal);
        Assert.Contains("func add(left, right)", hover.Contents, StringComparison.Ordinal);
        Assert.Contains("Adds two values.", hover.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void HoverReturnsImportedScriptFunctionCommentsAtCallSite()
    {
        var root = Path.Combine(Path.GetTempPath(), "aurora-hover-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var mainPath = Path.Combine(root, "main.as");
            var libPath = Path.Combine(root, "lib.as");
            var main =
                """
                @module(MAIN);
                import lib from './lib';
                export func run() {
                    return lib.compute(1);
                }
                """;
            var lib =
                """
                @module(LIB);
                // Computes a value.
                export func compute(value) {
                    return value;
                }
                """;
            var service = CreateService();
            service.OpenOrUpdateDocument(mainPath, main);
            service.OpenOrUpdateDocument(libPath, lib);

            var hover = service.GetHover(mainPath, PositionOf(main, "compute"));

            Assert.NotNull(hover);
            Assert.Contains("```aurorascript", hover!.Contents, StringComparison.Ordinal);
            Assert.Contains("export func compute(value)", hover.Contents, StringComparison.Ordinal);
            Assert.Contains("Computes a value.", hover.Contents, StringComparison.Ordinal);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void HoverReturnsObjectLiteralMemberCommentsAtCallSite()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                const timer = {
                    // Resets the timer.
                    reset: () => 0,
                    // Current tick count.
                    count: 1
                };
                timer.reset();
                return timer.count;
            }
            """;
        var service = CreateService();

        var resetHover = service.GetHover("test.as", source, PositionOfLast(source, "reset"));
        var countHover = service.GetHover("test.as", source, PositionOfLast(source, "count"));

        Assert.NotNull(resetHover);
        Assert.Contains("func reset()", resetHover!.Contents, StringComparison.Ordinal);
        Assert.Contains("Resets the timer.", resetHover.Contents, StringComparison.Ordinal);
        Assert.NotNull(countHover);
        Assert.Contains("property count", countHover!.Contents, StringComparison.Ordinal);
        Assert.Contains("Current tick count.", countHover.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void HoverReturnsAssignedObjectMemberCommentsAtCallSite()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                const timer = {};
                // Stops the timer.
                timer.stop = () => 0;
                return timer.stop();
            }
            """;
        var service = CreateService();

        var hover = service.GetHover("test.as", source, PositionOfLast(source, "stop"));

        Assert.NotNull(hover);
        Assert.Contains("func stop()", hover!.Contents, StringComparison.Ordinal);
        Assert.Contains("Stops the timer.", hover.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void HoverReturnsCommentsForNativeFunctions()
    {
        const string source =
            """
            @module(TEST);
            // Fast callable.
            native func helper(Number value) Number {
                return value;
            }
            """;
        var service = CreateService();

        var hover = service.GetHover("test.as", source, PositionOf(source, "helper"));

        Assert.NotNull(hover);
        Assert.Contains("Fast callable.", hover!.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void HoverReturnsModuleLeadingComments()
    {
        const string source =
            """
            // MD5 module.
            // Exports hash helpers.
            @module(MD5);
            export func run() {
                return 1;
            }
            """;
        var service = CreateService();

        var hover = service.GetHover("test.as", source, PositionOf(source, "@module"));

        Assert.NotNull(hover);
        Assert.Contains("MD5 module.", hover!.Contents, StringComparison.Ordinal);
        Assert.Contains("Exports hash helpers.", hover.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void HoverReturnsModuleAnnotationDocumentation()
    {
        const string source =
            """
            @module(TEST);
            func helper() {
                return 1;
            }
            """;
        var service = CreateService("zh-CN");

        var hover = service.GetHover("test.as", source, PositionOf(source, "@module"));

        Assert.NotNull(hover);
        Assert.Contains("显式查询名称", hover!.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void CompletionReturnsBuiltinGlobals()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                Ma
            }
            """;
        var service = CreateService();

        var completions = service.GetCompletions("test.as", source, PositionOf(source, "Ma"));

        Assert.Contains(completions.Items, item => item.Label == "Math" && item.Kind == CompletionItemKind.Type);
        Assert.Contains(completions.Items, item => item.Label == "Path" && item.Kind == CompletionItemKind.Constructor);
        Assert.Contains(completions.Items, item => item.Label == "console" && item.Kind == CompletionItemKind.Type);
        Assert.Contains(completions.Items, item => item.Label == "global" && item.Kind == CompletionItemKind.Object);
        Assert.DoesNotContain(completions.Items, item => item.Label is "fs" or "http");
    }

    [Fact]
    public void CompletionReturnsBuiltinMembers()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                Math.
            }
            """;
        var service = CreateService();

        var completions = service.GetCompletions("test.as", source, new TextPosition(2, 9));

        Assert.Contains(completions.Items, item => item.Label == "abs" && item.Kind == CompletionItemKind.Method);
        Assert.Contains(completions.Items, item => item.Label == "PI" && item.Kind == CompletionItemKind.Constant && item.ReadOnly);
    }

    [Fact]
    public void CompletionReturnsCompilerProvidedGlobalMembers()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                global.
            }
            """;
        var service = CreateService();

        var completions = service.GetCompletions("test.as", source, new TextPosition(2, 11));

        Assert.Contains(completions.Items, item => item.Label == "modules" && item.Kind == CompletionItemKind.Property && item.ReadOnly);
        Assert.Contains(completions.Items, item => item.Label == "getModule" && item.Kind == CompletionItemKind.Method && item.ReadOnly);
    }

    [Fact]
    public void CompletionReturnsBuiltinModuleMembersForImportAlias()
    {
        const string source =
            """
            @module(TEST);
            import files from 'fs';
            export func run() {
                files.
            }
            """;
        var service = CreateService();

        var completions = service.GetCompletions(sourceName: "test.as", source, new TextPosition(3, 10));

        Assert.Contains(completions.Items, item => item.Label == "readText" && item.Kind == CompletionItemKind.Method);
        Assert.Contains(completions.Items, item => item.Label == "appendBytes" && item.Kind == CompletionItemKind.Method);
        Assert.Contains(completions.Items, item => item.Label == "size" && item.Kind == CompletionItemKind.Method);
    }

    [Fact]
    public void HoverAndSignatureHelpResolveBuiltinModuleImportAlias()
    {
        const string source =
            """
            @module(TEST);
            import web from 'http';
            export func run() {
                return web.getAsync('https://example.test', (error, response) => {});
            }
            """;
        var service = CreateService();

        var hover = service.GetHover("test.as", source, PositionOf(source, "getAsync"));
        var signature = service.GetSignatureHelp("test.as", source, PositionOf(source, "response"));

        Assert.NotNull(hover);
        Assert.Contains("static func getAsync", hover!.Contents, StringComparison.Ordinal);
        Assert.Contains("callback(null, response)", hover.Contents, StringComparison.Ordinal);
        Assert.NotNull(signature);
        Assert.Equal(
            "static func getAsync(String url, HttpRequestOptions | Null options, Function callback) Boolean",
            Assert.Single(signature!.Signatures).Label);
    }

    [Fact]
    public void BuiltinModuleImportsDoNotProduceMissingImportDiagnostics()
    {
        const string source =
            """
            @module(TEST);
            import files from 'fs';
            import web from 'http';
            export func run() {
                return files.exist('.') && web != null;
            }
            """;
        var service = CreateService();

        var diagnostics = service.GetDiagnostics("test.as", source);

        Assert.DoesNotContain(diagnostics, diagnostic => diagnostic.Code == "AURORA-IMPORT-NOT-FOUND");
    }

    [Fact]
    public void DefinitionReturnsBuiltinModuleDeclarationDocument()
    {
        const string source =
            """
            @module(TEST);
            import files from 'fs';
            export func run(path) {
                return files.readText(path);
            }
            """;
        var service = CreateService();

        var definition = service.GetDefinition("test.as", source, PositionOf(source, "readText"));

        Assert.NotNull(definition);
        Assert.Equal("aurora-builtin:/fs.as", definition!.Path);
        var document = service.GetBuiltinDocument(definition.Path);
        Assert.NotNull(document);
        Assert.Contains("static func readText(String | Path path) String", document!.Text, StringComparison.Ordinal);
    }

    [Fact]
    public void CompletionReturnsCurrentScopeSymbols()
    {
        const string source =
            """
            @module(TEST);
            const moduleConst = 1;
            func helper() {
                return 1;
            }
            export func run(input) {
                var localValue = input;
                const localConst = 2;
                zz
            }
            """;
        var service = CreateService();

        var completions = service.GetCompletions("test.as", source, PositionAfter(source, "zz"));

        Assert.Contains(completions.Items, item => item.Label == "localValue" && item.Kind == CompletionItemKind.Variable);
        Assert.Contains(completions.Items, item => item.Label == "localConst" && item.Kind == CompletionItemKind.Constant && item.ReadOnly);
        Assert.Contains(completions.Items, item => item.Label == "input" && item.Kind == CompletionItemKind.Variable);
        Assert.Contains(completions.Items, item => item.Label == "helper" && item.Kind == CompletionItemKind.Function);
        Assert.Contains(completions.Items, item => item.Label == "moduleConst" && item.Kind == CompletionItemKind.Constant);
        Assert.Contains(completions.Items, item => item.Label == "Math" && item.Kind == CompletionItemKind.Type);
    }

    [Fact]
    public void CompletionReturnsImportAliases()
    {
        var root = Path.Combine(Path.GetTempPath(), "aurora-completion-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var mainPath = Path.Combine(root, "main.as");
            var libPath = Path.Combine(root, "lib.as");
            var main =
                """
                @module(MAIN);
                import lib from './lib';
                export func run() {
                    zz
                }
                """;
            var lib = "@module(LIB); export const value = 42;";
            var service = CreateService();
            service.OpenOrUpdateDocument(mainPath, main);
            service.OpenOrUpdateDocument(libPath, lib);

            var completions = service.GetCompletions(mainPath, PositionAfter(main, "zz"));

            Assert.Contains(completions.Items, item => item.Label == "lib" && item.Kind == CompletionItemKind.Module);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void CompletionReturnsImportedModuleMembers()
    {
        var root = Path.Combine(Path.GetTempPath(), "aurora-completion-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var mainPath = Path.Combine(root, "main.as");
            var libPath = Path.Combine(root, "lib.as");
            var main =
                """
                @module(MAIN);
                import lib from './lib';
                export func run() {
                    lib.
                }
                """;
            var lib =
                """
                @module(LIB);
                export const value = 42;
                export func compute(input) {
                    return input;
                }
                """;
            var service = CreateService();
            service.OpenOrUpdateDocument(mainPath, main);
            service.OpenOrUpdateDocument(libPath, lib);

            var completions = service.GetCompletions(mainPath, new TextPosition(3, 8));

            Assert.Contains(completions.Items, item => item.Label == "value" && item.Kind == CompletionItemKind.Constant && item.ReadOnly);
            Assert.Contains(completions.Items, item => item.Label == "compute" && item.Kind == CompletionItemKind.Method);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SignatureHelpReturnsBuiltinMethodSignature()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                return Math.pow(2, 3);
            }
            """;
        var service = CreateService();

        var signatureHelp = service.GetSignatureHelp("test.as", source, PositionOf(source, "3"));

        Assert.NotNull(signatureHelp);
        var signature = Assert.Single(signatureHelp!.Signatures);
        Assert.Equal("static func pow(Number x, Number y) Number", signature.Label);
        Assert.Equal(1, signatureHelp.ActiveParameter);
        Assert.Equal(2, signature.Parameters.Count);
    }

    [Fact]
    public void SignatureHelpReturnsBuiltinConstructorSignature()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                return new Path("mem://app", "scripts");
            }
            """;
        var service = CreateService();

        var signatureHelp = service.GetSignatureHelp("test.as", source, PositionOf(source, "scripts"));

        Assert.NotNull(signatureHelp);
        var signature = Assert.Single(signatureHelp!.Signatures);
        Assert.Equal("constructor(String | Path | Null root, ...String | Path segments)", signature.Label);
        Assert.Equal(1, signatureHelp.ActiveParameter);
        Assert.Equal(2, signature.Parameters.Count);
    }

    [Fact]
    public void SignatureHelpReturnsCallableConstructorSignature()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                return String(123);
            }
            """;
        var service = CreateService();

        var signatureHelp = service.GetSignatureHelp("test.as", source, PositionOf(source, "123"));

        Assert.NotNull(signatureHelp);
        var signature = Assert.Single(signatureHelp!.Signatures);
        Assert.Equal("constructor(Object | Null value)", signature.Label);
        Assert.Equal(0, signatureHelp.ActiveParameter);
    }

    [Fact]
    public void CompletionShowsBuiltinConstructorSignature()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                return Pa
            }
            """;
        var service = CreateService();

        var completions = service.GetCompletions("test.as", source, PositionOf(source, "Pa"));

        Assert.Contains(completions.Items, item =>
            item.Label == "Path" &&
            item.Detail == "constructor(String | Path | Null root, ...String | Path segments)");
    }

    [Fact]
    public void CompletionReturnsObjectLiteralMembers()
    {
        const string source =
            """
            @module(TEST);
            export func run() {
                const timer = {
                    reset: () => 0,
                    count: 1
                };
                timer.
            }
            """;
        var service = CreateService();

        var completions = service.GetCompletions("test.as", source, PositionAfter(source, "timer."));

        Assert.Contains(completions.Items, item => item.Label == "reset" && item.Kind == CompletionItemKind.Method);
        Assert.Contains(completions.Items, item => item.Label == "count" && item.Kind == CompletionItemKind.Property);
        Assert.DoesNotContain(completions.Items, item => item.Label == "Math");
    }

    [Fact]
    public void HoverReturnsFunctionSignatureWithoutComments()
    {
        const string source =
            """
            @module(TEST);
            export func add(Number left, Number right) Number {
                return left + right;
            }
            export func run() {
                return add(1, 2);
            }
            """;
        var service = CreateService();

        var hover = service.GetHover("test.as", source, PositionOfLast(source, "add"));

        Assert.NotNull(hover);
        Assert.Contains("func add(Number left, Number right) Number", hover!.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void HoverPreservesExportNativeKeywordOrder()
    {
        const string source =
            """
            @module(TEST);
            export native func createAStar(Number width) Number {
                return width;
            }
            """;
        var service = CreateService();

        var hover = service.GetHover("test.as", source, PositionOf(source, "createAStar"));

        Assert.NotNull(hover);
        Assert.Contains("export native func createAStar(Number width) Number", hover!.Contents, StringComparison.Ordinal);
        Assert.DoesNotContain("native export", hover.Contents, StringComparison.Ordinal);
    }

    [Fact]
    public void SignatureHelpReturnsScriptFunctionSignature()
    {
        const string source =
            """
            @module(TEST);
            export func add(Number left, Number right) Number {
                return left + right;
            }
            export func run() {
                return add(1, 2);
            }
            """;
        var service = CreateService();

        var signatureHelp = service.GetSignatureHelp("test.as", source, PositionOf(source, "2"));

        Assert.NotNull(signatureHelp);
        var signature = Assert.Single(signatureHelp!.Signatures);
        Assert.Equal("func add(Number left, Number right) Number", signature.Label);
        Assert.Equal(1, signatureHelp.ActiveParameter);
    }

    private static AuroraLanguageService CreateService(string? locale = null)
    {
        var catalog = BuiltinApiLoader.LoadFromFile(BuiltinApiCatalogTests.GetRuntimeApiPath());
        return new AuroraLanguageService(new AuroraLanguageServiceOptions(catalog)
        {
            DocumentationLocale = locale ?? "en"
        });
    }

    private static TextPosition PositionOf(string source, string needle)
    {
        var offset = source.IndexOf(needle, StringComparison.Ordinal);
        Assert.True(offset >= 0, $"Needle '{needle}' not found.");
        return PositionAtOffset(source, offset);
    }

    private static TextPosition PositionAfter(string source, string needle)
    {
        var offset = source.IndexOf(needle, StringComparison.Ordinal);
        Assert.True(offset >= 0, $"Needle '{needle}' not found.");
        return PositionAtOffset(source, offset + needle.Length);
    }

    private static TextPosition PositionOfLast(string source, string needle)
    {
        var offset = source.LastIndexOf(needle, StringComparison.Ordinal);
        Assert.True(offset >= 0, $"Needle '{needle}' not found.");
        return PositionAtOffset(source, offset);
    }

    private static TextPosition PositionAtOffset(string source, int offset)
    {
        var line = 0;
        var character = 0;
        for (var i = 0; i < offset; i++)
        {
            if (source[i] == '\r')
            {
                if (i + 1 < offset && source[i + 1] == '\n')
                {
                    i++;
                }
                line++;
                character = 0;
            }
            else if (source[i] == '\n')
            {
                line++;
                character = 0;
            }
            else
            {
                character++;
            }
        }

        return new TextPosition(line, character);
    }
}
