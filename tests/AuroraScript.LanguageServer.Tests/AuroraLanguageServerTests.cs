using AuroraScript.LanguageServer;
using AuroraScript.LanguageServices.Builtins;
using AuroraScript.LanguageServices.Features.SemanticTokens;
using System;
using System.IO;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using Xunit;

namespace AuroraScript.LanguageServer.Tests;

public sealed class AuroraLanguageServerTests
{
    [Fact]
    public void EmbeddedRuntimeApiMetadataLoads()
    {
        using var stream = typeof(AuroraLanguageServerFactory).Assembly.GetManifestResourceStream(
            AuroraLanguageServerFactory.RuntimeApiResourceName);
        Assert.NotNull(stream);

        var catalog = BuiltinApiLoader.Load(stream);

        Assert.True(catalog.TryGetGlobal("Math", out var math));
        Assert.True(math.TryGetMember("abs", out var abs));
        Assert.Equal("number", abs.ReturnType);
        Assert.True(catalog.TryGetModule("fs", out var fileSystem));
        Assert.True(fileSystem.TryGetMember("readText", out _));
        Assert.True(catalog.TryGetModule("http", out var http));
        Assert.True(http.TryGetMember("getAsync", out _));
    }

    [Fact]
    public void DefaultBuiltinCatalogFallsBackToEmbeddedRuntimeApiMetadata()
    {
        var catalog = AuroraLanguageServerFactory.LoadDefaultBuiltinCatalog(() => null);

        Assert.True(catalog.TryGetGlobal("Math", out var math));
        Assert.True(math.TryGetMember("abs", out var abs));
        Assert.Equal("number", abs.ReturnType);
    }

    [Fact]
    public async Task InitializeReturnsCoreCapabilities()
    {
        var server = CreateServer();
        var result = await server.HandleAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = 1,
            ["method"] = "initialize",
            ["params"] = new JsonObject()
        });

        Assert.NotNull(result.Response);
        var capabilities = result.Response!.Result!.AsObject()["capabilities"]!.AsObject();
        Assert.True(capabilities["hoverProvider"]!.GetValue<bool>());
        Assert.NotNull(capabilities["completionProvider"]);
        Assert.NotNull(capabilities["signatureHelpProvider"]);
        Assert.True(capabilities["renameProvider"]!.GetValue<bool>());
        Assert.True(capabilities["documentFormattingProvider"]!.GetValue<bool>());
        Assert.NotNull(capabilities["semanticTokensProvider"]);
    }

    [Fact]
    public async Task DidOpenPublishesReadonlyBuiltinDiagnostics()
    {
        var server = CreateServer();
        var result = await DidOpen(server,
            """
            @module(TEST);
            export func run() {
                Math.PI = 1;
            }
            """);

        var notification = Assert.Single(result.Notifications);
        Assert.Equal("textDocument/publishDiagnostics", notification.Method);
        var diagnostics = notification.Parameters["diagnostics"]!.AsArray();
        var diagnosticNode = Assert.Single(diagnostics);
        Assert.NotNull(diagnosticNode);
        var diagnostic = diagnosticNode!.AsObject();
        Assert.Equal("AURORA-BUILTIN-READONLY", diagnostic["code"]!.GetValue<string>());
        Assert.Contains("Math.PI", diagnostic["message"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    [Fact]
    public async Task DidOpenRecognizesBuiltinFileSystemImport()
    {
        var server = CreateServer();
        const string source =
            """
            @module(TEST);
            import fs from 'fs';
            export func run(path) {
                return fs.readText(path);
            }
            """;

        var result = await DidOpen(server, source);

        Assert.Empty(PublishedDiagnostics(result, TestUri));
    }

    [Fact]
    public async Task DependencyDocumentChangesRefreshReferencingDiagnostics()
    {
        var server = CreateServer();
        var root = Path.Combine(Path.GetTempPath(), "aurora-lsp-diagnostics-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var mainPath = Path.Combine(root, "main.as");
            var libPath = Path.Combine(root, "lib.as");
            var mainUri = new Uri(mainPath).AbsoluteUri;
            var libUri = new Uri(libPath).AbsoluteUri;
            const string main =
                """
                @module(MAIN);
                import lib from './lib';
                export func run() {
                    return lib.value;
                }
                """;
            const string lib = "@module(LIB); export const value = 42;";

            var missingResult = await DidOpen(server, mainUri, main);
            var missingDiagnostics = PublishedDiagnostics(missingResult, mainUri);
            var missingDiagnostic = Assert.Single(missingDiagnostics);
            Assert.Contains("Import file not found", missingDiagnostic!.AsObject()["message"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);

            var resolvedResult = await DidOpen(server, libUri, lib);
            Assert.Empty(PublishedDiagnostics(resolvedResult, mainUri));
            Assert.Empty(PublishedDiagnostics(resolvedResult, libUri));

            var missingAgainResult = await DidClose(server, libUri);
            Assert.Empty(PublishedDiagnostics(missingAgainResult, libUri));
            var missingAgainDiagnostics = PublishedDiagnostics(missingAgainResult, mainUri);
            var missingAgainDiagnostic = Assert.Single(missingAgainDiagnostics);
            Assert.Contains("Import file not found", missingAgainDiagnostic!.AsObject()["message"]!.GetValue<string>(), StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task HoverReturnsBuiltinMemberMarkdown()
    {
        var server = CreateServer();
        const string source =
            """
            @module(TEST);
            export func run() {
                return Math.abs(-1);
            }
            """;
        await DidOpen(server, source);

        var result = await Request(server, 2, "textDocument/hover", new JsonObject
        {
            ["textDocument"] = TextDocument(),
            ["position"] = Position(source, "abs")
        });

        var hover = result.Response!.Result!.AsObject();
        var contents = hover["contents"]!.AsObject();
        Assert.Equal("markdown", contents["kind"]!.GetValue<string>());
        var value = contents["value"]!.GetValue<string>();
        Assert.Contains("```aurorascript", value, StringComparison.Ordinal);
        Assert.Contains("Math.abs(value: Number): Number;", value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HoverReturnsBuiltinOwnerAndMemberDeclarationMarkdown()
    {
        var server = CreateServer();
        const string source =
            """
            @module(TEST);
            export func run() {
                console.log("x");
            }
            """;
        await DidOpen(server, source);

        var ownerResult = await Request(server, 23, "textDocument/hover", new JsonObject
        {
            ["textDocument"] = TextDocument(),
            ["position"] = Position(source, "console")
        });
        var ownerValue = ownerResult.Response!.Result!.AsObject()["contents"]!.AsObject()["value"]!.GetValue<string>();
        Assert.Contains("console;", ownerValue, StringComparison.Ordinal);
        Assert.DoesNotContain("console.log", ownerValue, StringComparison.Ordinal);

        var memberResult = await Request(server, 24, "textDocument/hover", new JsonObject
        {
            ["textDocument"] = TextDocument(),
            ["position"] = Position(source, "log")
        });
        var memberValue = memberResult.Response!.Result!.AsObject()["contents"]!.AsObject()["value"]!.GetValue<string>();
        Assert.Contains("console.log(...values: Object[]): void;", memberValue, StringComparison.Ordinal);
        Assert.DoesNotContain("readonly func", memberValue, StringComparison.Ordinal);
    }

    [Fact]
    public async Task HoverReturnsScriptFunctionCommentsAtCallSite()
    {
        var server = CreateServer();
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
        await DidOpen(server, source);

        var result = await Request(server, 25, "textDocument/hover", new JsonObject
        {
            ["textDocument"] = TextDocument(),
            ["position"] = PositionOfLast(source, "add")
        });

        var contents = result.Response!.Result!.AsObject()["contents"]!.AsObject();
        Assert.Equal("markdown", contents["kind"]!.GetValue<string>());
        var value = contents["value"]!.GetValue<string>();
        Assert.Contains("```aurorascript", value, StringComparison.Ordinal);
        Assert.Contains("func add(left, right)", value, StringComparison.Ordinal);
        Assert.Contains("Adds two values.", value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InitializeLocaleControlsBuiltinHoverLanguage()
    {
        var server = CreateServer();
        await Request(server, 21, "initialize", new JsonObject
        {
            ["locale"] = "zh-CN"
        });
        const string source =
            """
            @module(TEST);
            export func run() {
                return String.fromCharCode(65);
            }
            """;
        await DidOpen(server, source);

        var result = await Request(server, 22, "textDocument/hover", new JsonObject
        {
            ["textDocument"] = TextDocument(),
            ["position"] = Position(source, "fromCharCode")
        });

        var hover = result.Response!.Result!.AsObject();
        var value = hover["contents"]!.AsObject()["value"]!.GetValue<string>();
        Assert.Contains("根据字符编码创建单字符字符串", value, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CompletionReturnsBuiltinMembers()
    {
        var server = CreateServer();
        const string source =
            """
            @module(TEST);
            export func run() {
                Math.
            }
            """;
        await DidOpen(server, source);

        var result = await Request(server, 3, "textDocument/completion", new JsonObject
        {
            ["textDocument"] = TextDocument(),
            ["position"] = new JsonObject { ["line"] = 2, ["character"] = 9 }
        });

        var items = result.Response!.Result!.AsArray();
        Assert.Contains(items, node => node!.AsObject()["label"]!.GetValue<string>() == "abs");
        Assert.Contains(items, node => node!.AsObject()["label"]!.GetValue<string>() == "PI");
    }

    [Fact]
    public async Task SignatureHelpReturnsActiveParameter()
    {
        var server = CreateServer();
        const string source =
            """
            @module(TEST);
            export func run() {
                return Math.pow(2, 3);
            }
            """;
        await DidOpen(server, source);

        var result = await Request(server, 4, "textDocument/signatureHelp", new JsonObject
        {
            ["textDocument"] = TextDocument(),
            ["position"] = Position(source, "3")
        });

        var signatureHelp = result.Response!.Result!.AsObject();
        Assert.Equal(1, signatureHelp["activeParameter"]!.GetValue<int>());
        var signatureNode = Assert.Single(signatureHelp["signatures"]!.AsArray());
        Assert.NotNull(signatureNode);
        var signature = signatureNode!.AsObject();
        Assert.Equal("Math.pow(x: Number, y: Number): Number", signature["label"]!.GetValue<string>());
    }

    [Fact]
    public async Task DefinitionReturnsImportedModuleMemberLocation()
    {
        var server = CreateServer();
        var root = Path.Combine(Path.GetTempPath(), "aurora-lsp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var mainPath = Path.Combine(root, "main.as");
            var libPath = Path.Combine(root, "lib.as");
            var mainUri = new Uri(mainPath).AbsoluteUri;
            var libUri = new Uri(libPath).AbsoluteUri;
            const string main =
                """
                @module(MAIN);
                import lib from './lib';
                export func run() {
                    return lib.value;
                }
                """;
            const string lib = "@module(LIB); export const value = 42;";
            File.WriteAllText(libPath, lib);
            await DidOpen(server, libUri, lib);
            await DidOpen(server, mainUri, main);

            var result = await Request(server, 5, "textDocument/definition", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = mainUri },
                ["position"] = Position(main, "value")
            });

            var location = result.Response!.Result!.AsObject();
            Assert.Equal(libUri, location["uri"]!.GetValue<string>());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DefinitionReturnsBuiltinVirtualDocumentLocation()
    {
        var server = CreateServer();
        const string source =
            """
            @module(TEST);
            export func run() {
                return Math.abs(-1);
            }
            """;
        await DidOpen(server, source);

        var definitionResult = await Request(server, 53, "textDocument/definition", new JsonObject
        {
            ["textDocument"] = TextDocument(),
            ["position"] = Position(source, "abs")
        });

        var location = definitionResult.Response!.Result!.AsObject();
        var uri = location["uri"]!.GetValue<string>();
        Assert.Equal("aurora-builtin:/Math.as", uri);

        var documentResult = await Request(server, 54, "aurora/builtinDocument", new JsonObject
        {
            ["uri"] = uri
        });

        var document = documentResult.Response!.Result!.AsObject();
        Assert.Equal("aurora", document["languageId"]!.GetValue<string>());
        var text = document["text"]!.GetValue<string>();
        Assert.Contains("Math.abs(value: Number): Number;", text, StringComparison.Ordinal);
        Assert.Contains("Math.PI: Number;", text, StringComparison.Ordinal);
    }

    [Fact]
    public async Task DefinitionReturnsBuiltinTypeReferenceLocationInsideVirtualDocument()
    {
        var server = CreateServer();
        var documentResult = await Request(server, 55, "aurora/builtinDocument", new JsonObject
        {
            ["uri"] = "aurora-builtin:/Math.as"
        });
        var text = documentResult.Response!.Result!.AsObject()["text"]!.GetValue<string>();

        var definitionResult = await Request(server, 56, "textDocument/definition", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = "aurora-builtin:/Math.as" },
            ["position"] = Position(text, "Number")
        });

        var location = definitionResult.Response!.Result!.AsObject();
        Assert.Equal("aurora-builtin:/Number.as", location["uri"]!.GetValue<string>());
    }

    [Fact]
    public async Task DefinitionUsesOpenWorkspaceDocumentsWithoutDiskFile()
    {
        var server = CreateServer();
        var root = Path.Combine(Path.GetTempPath(), "aurora-lsp-memory-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var mainPath = Path.Combine(root, "main.as");
            var libPath = Path.Combine(root, "lib.as");
            var mainUri = new Uri(mainPath).AbsoluteUri;
            var libUri = new Uri(libPath).AbsoluteUri;
            const string main =
                """
                @module(MAIN);
                import lib from './lib';
                export func run() {
                    return lib.value;
                }
                """;
            const string lib = "@module(LIB); export const value = 42;";
            await DidOpen(server, libUri, lib);
            await DidOpen(server, mainUri, main);

            var result = await Request(server, 50, "textDocument/definition", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = mainUri },
                ["position"] = Position(main, "value")
            });

            var location = result.Response!.Result!.AsObject();
            Assert.Equal(libUri, location["uri"]!.GetValue<string>());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task DidChangeInvalidatesImportedDefinitionLocation()
    {
        var server = CreateServer();
        var root = Path.Combine(Path.GetTempPath(), "aurora-lsp-change-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var mainPath = Path.Combine(root, "main.as");
            var libPath = Path.Combine(root, "lib.as");
            var mainUri = new Uri(mainPath).AbsoluteUri;
            var libUri = new Uri(libPath).AbsoluteUri;
            const string main =
                """
                @module(MAIN);
                import lib from './lib';
                export func run() {
                    return lib.value;
                }
                """;
            const string firstLib = "@module(LIB); export const value = 1;";
            const string secondLib =
                """
                @module(LIB);
                export const other = 1;
                export const value = 2;
                """;
            await DidOpen(server, libUri, firstLib);
            await DidOpen(server, mainUri, main);

            var firstResult = await Request(server, 51, "textDocument/definition", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = mainUri },
                ["position"] = Position(main, "value")
            });

            await DidChange(server, libUri, secondLib, version: 2);
            var secondResult = await Request(server, 52, "textDocument/definition", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = mainUri },
                ["position"] = Position(main, "value")
            });

            Assert.Equal(0, LocationStartLine(firstResult.Response!.Result!));
            Assert.Equal(2, LocationStartLine(secondResult.Response!.Result!));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ReferencesReturnsImportedExportLocations()
    {
        var server = CreateServer();
        var root = Path.Combine(Path.GetTempPath(), "aurora-lsp-ref-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var mainPath = Path.Combine(root, "main.as");
            var libPath = Path.Combine(root, "lib.as");
            var otherPath = Path.Combine(root, "other.as");
            var mainUri = new Uri(mainPath).AbsoluteUri;
            var libUri = new Uri(libPath).AbsoluteUri;
            var otherUri = new Uri(otherPath).AbsoluteUri;
            const string main =
                """
                @module(MAIN);
                import lib from './lib';
                export func run() { return lib.value; }
                """;
            const string lib = "@module(LIB); export const value = 42;";
            const string other =
                """
                @module(OTHER);
                import lib from './lib';
                export func run() { return lib.value; }
                """;
            File.WriteAllText(libPath, lib);
            await DidOpen(server, libUri, lib);
            await DidOpen(server, mainUri, main);
            await DidOpen(server, otherUri, other);

            var result = await Request(server, 6, "textDocument/references", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = mainUri },
                ["position"] = Position(main, "value"),
                ["context"] = new JsonObject { ["includeDeclaration"] = true }
            });

            var locations = result.Response!.Result!.AsArray();
            Assert.Equal(3, locations.Count);
            Assert.Contains(locations, node => node!.AsObject()["uri"]!.GetValue<string>() == libUri);
            Assert.Contains(locations, node => node!.AsObject()["uri"]!.GetValue<string>() == mainUri);
            Assert.Contains(locations, node => node!.AsObject()["uri"]!.GetValue<string>() == otherUri);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task ReferencesUseInitializeWorkspaceRootForDiskFiles()
    {
        var server = CreateServer();
        var root = Path.Combine(Path.GetTempPath(), "aurora-lsp-root-ref-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var mainPath = Path.Combine(root, "main.as");
            var libPath = Path.Combine(root, "lib.as");
            var otherPath = Path.Combine(root, "other.as");
            var mainUri = new Uri(mainPath).AbsoluteUri;
            var libUri = new Uri(libPath).AbsoluteUri;
            var otherUri = new Uri(otherPath).AbsoluteUri;
            const string lib = "@module(LIB); export const value = 42;";
            const string main =
                """
                @module(MAIN);
                import lib from './lib';
                export func run() { return lib.value; }
                """;
            const string other =
                """
                @module(OTHER);
                import shared from './lib';
                export func run() { return shared.value; }
                """;
            File.WriteAllText(libPath, lib);
            File.WriteAllText(mainPath, main);
            File.WriteAllText(otherPath, other);

            await Request(server, 61, "initialize", new JsonObject
            {
                ["rootUri"] = new Uri(root).AbsoluteUri
            });
            await DidOpen(server, mainUri, main);

            var result = await Request(server, 62, "textDocument/references", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = mainUri },
                ["position"] = Position(main, "value"),
                ["context"] = new JsonObject { ["includeDeclaration"] = true }
            });

            var locations = result.Response!.Result!.AsArray();
            Assert.Equal(3, locations.Count);
            Assert.Contains(locations, node => node!.AsObject()["uri"]!.GetValue<string>() == libUri);
            Assert.Contains(locations, node => node!.AsObject()["uri"]!.GetValue<string>() == mainUri);
            Assert.Contains(locations, node => node!.AsObject()["uri"]!.GetValue<string>() == otherUri);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task RenameReturnsWorkspaceEditForImportedExport()
    {
        var server = CreateServer();
        var root = Path.Combine(Path.GetTempPath(), "aurora-lsp-rename-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var mainPath = Path.Combine(root, "main.as");
            var libPath = Path.Combine(root, "lib.as");
            var otherPath = Path.Combine(root, "other.as");
            var mainUri = new Uri(mainPath).AbsoluteUri;
            var libUri = new Uri(libPath).AbsoluteUri;
            var otherUri = new Uri(otherPath).AbsoluteUri;
            const string main =
                """
                @module(MAIN);
                import lib from './lib';
                export func run() { return lib.value; }
                """;
            const string lib = "@module(LIB); export const value = 42;";
            const string other =
                """
                @module(OTHER);
                import lib from './lib';
                export func run() { return lib.value; }
                """;
            File.WriteAllText(libPath, lib);
            await DidOpen(server, libUri, lib);
            await DidOpen(server, mainUri, main);
            await DidOpen(server, otherUri, other);

            var result = await Request(server, 7, "textDocument/rename", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = mainUri },
                ["position"] = Position(main, "value"),
                ["newName"] = "total"
            });

            var edit = result.Response!.Result!.AsObject();
            var changes = edit["changes"]!.AsObject();
            Assert.True(changes.ContainsKey(libUri));
            Assert.True(changes.ContainsKey(mainUri));
            Assert.True(changes.ContainsKey(otherUri));
            Assert.Equal("total", changes[mainUri]!.AsArray()[0]!.AsObject()["newText"]!.GetValue<string>());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task SemanticTokensReturnsEncodedTokenData()
    {
        var server = CreateServer();
        const string source =
            """
            @module(TEST);
            export func run(value) {
                const total = value + 10;
                return `total: ${total}`;
            }
            """;
        await DidOpen(server, source);

        var result = await Request(server, 8, "textDocument/semanticTokens/full", new JsonObject
        {
            ["textDocument"] = TextDocument()
        });

        var semanticTokens = result.Response!.Result!.AsObject();
        var data = semanticTokens["data"]!.AsArray();
        Assert.NotEmpty(data);
        Assert.Equal(0, data.Count % 5);
    }

    [Fact]
    public async Task SemanticTokensSupportStandaloneTDocDocuments()
    {
        var server = CreateServer();
        const string uri = "file:///D:/workspace/config.tdoc";
        const string source = "Object { readonly String id \"UX01\", count -1, }";
        await DidOpen(server, uri, source);

        var result = await Request(server, 81, "textDocument/semanticTokens/full", new JsonObject
        {
            ["textDocument"] = new JsonObject { ["uri"] = uri }
        });

        var tokenTypes = SemanticTokenTypes(result.Response!.Result!.AsObject()["data"]!.AsArray());
        Assert.Contains(AuroraSemanticTokenTypes.Type, tokenTypes);
        Assert.Contains(AuroraSemanticTokenTypes.MapKey, tokenTypes);
        Assert.Contains(AuroraSemanticTokenTypes.Keyword, tokenTypes);
        Assert.Contains(AuroraSemanticTokenTypes.Number, tokenTypes);
    }

    [Fact]
    public async Task SemanticTokensUseStartupWorkspaceDeclareDeclarations()
    {
        var server = CreateServer();
        var root = Path.Combine(Path.GetTempPath(), "aurora-lsp-declare-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var mainPath = Path.Combine(root, "main.as");
            var globalsPath = Path.Combine(root, "globals.as");
            var mainUri = new Uri(mainPath).AbsoluteUri;
            const string main =
                """
                @module(MAIN);
                export func run() {
                    INPUT_NUMBER("title", "label", "number", null);
                    return APP_VERSION;
                }
                """;
            const string globals =
                """
                @global();
                declare const APP_VERSION;
                declare func INPUT_NUMBER(title, label, type, callback);
                """;
            File.WriteAllText(mainPath, main);
            File.WriteAllText(globalsPath, globals);

            await Request(server, 91, "initialize", new JsonObject
            {
                ["rootUri"] = new Uri(root).AbsoluteUri
            });

            var result = await Request(server, 92, "textDocument/semanticTokens/full", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = mainUri }
            });

            var tokenTypes = SemanticTokenTypes(result.Response!.Result!.AsObject()["data"]!.AsArray());
            Assert.Contains(AuroraSemanticTokenTypes.DeclaredGlobalFunction, tokenTypes);
            Assert.Contains(AuroraSemanticTokenTypes.DeclaredGlobal, tokenTypes);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task StartupWorkspaceDeclareScanSkipsBinDirectory()
    {
        var server = CreateServer();
        var root = Path.Combine(Path.GetTempPath(), "aurora-lsp-declare-skip-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var binDirectory = Path.Combine(root, "bin");
            Directory.CreateDirectory(binDirectory);
            var mainPath = Path.Combine(root, "main.as");
            var binGlobalsPath = Path.Combine(binDirectory, "globals.as");
            var mainUri = new Uri(mainPath).AbsoluteUri;
            const string main =
                """
                @module(MAIN);
                export func run() {
                    return BIN_ONLY_VERSION;
                }
                """;
            const string binGlobals =
                """
                @global();
                declare const BIN_ONLY_VERSION;
                """;
            File.WriteAllText(mainPath, main);
            File.WriteAllText(binGlobalsPath, binGlobals);

            await Request(server, 93, "initialize", new JsonObject
            {
                ["rootUri"] = new Uri(root).AbsoluteUri
            });

            var result = await Request(server, 94, "textDocument/semanticTokens/full", new JsonObject
            {
                ["textDocument"] = new JsonObject { ["uri"] = mainUri }
            });

            var tokenTypes = SemanticTokenTypes(result.Response!.Result!.AsObject()["data"]!.AsArray());
            Assert.DoesNotContain(AuroraSemanticTokenTypes.DeclaredGlobal, tokenTypes);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task FormattingReturnsTextEdits()
    {
        var server = CreateServer();
        const string source = "@module(TEST);\nexport func run() {\nreturn 1;   \n}\n";
        await DidOpen(server, source);

        var result = await Request(server, 9, "textDocument/formatting", new JsonObject
        {
            ["textDocument"] = TextDocument(),
            ["options"] = new JsonObject
            {
                ["tabSize"] = 2,
                ["insertSpaces"] = true
            }
        });

        var edits = result.Response!.Result!.AsArray();
        var edit = Assert.Single(edits);
        Assert.NotNull(edit);
        var text = edit!.AsObject()["newText"]!.GetValue<string>();
        Assert.Contains("  return 1;", text, StringComparison.Ordinal);
        Assert.DoesNotContain("1;   ", text, StringComparison.Ordinal);
    }

    private static AuroraLanguageServer CreateServer()
    {
        return AuroraLanguageServerFactory.CreateDefault();
    }

    private static Task<AuroraScript.LanguageServer.Protocol.LspResult> DidOpen(AuroraLanguageServer server, string source)
    {
        return DidOpen(server, TestUri, source);
    }

    private static Task<AuroraScript.LanguageServer.Protocol.LspResult> DidOpen(AuroraLanguageServer server, string uri, string source)
    {
        return server.HandleAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = "textDocument/didOpen",
            ["params"] = new JsonObject
            {
                ["textDocument"] = new JsonObject
                {
                    ["uri"] = uri,
                    ["languageId"] = "aurora",
                    ["version"] = 1,
                    ["text"] = source
                }
            }
        });
    }

    private static Task<AuroraScript.LanguageServer.Protocol.LspResult> DidChange(
        AuroraLanguageServer server,
        string uri,
        string source,
        int version)
    {
        return server.HandleAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = "textDocument/didChange",
            ["params"] = new JsonObject
            {
                ["textDocument"] = new JsonObject
                {
                    ["uri"] = uri,
                    ["version"] = version
                },
                ["contentChanges"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["text"] = source
                    }
                }
            }
        });
    }

    private static Task<AuroraScript.LanguageServer.Protocol.LspResult> DidClose(AuroraLanguageServer server, string uri)
    {
        return server.HandleAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["method"] = "textDocument/didClose",
            ["params"] = new JsonObject
            {
                ["textDocument"] = new JsonObject
                {
                    ["uri"] = uri
                }
            }
        });
    }

    private static Task<AuroraScript.LanguageServer.Protocol.LspResult> Request(
        AuroraLanguageServer server,
        int id,
        string method,
        JsonObject parameters)
    {
        return server.HandleAsync(new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["method"] = method,
            ["params"] = parameters
        });
    }

    private static JsonObject TextDocument()
    {
        return new JsonObject { ["uri"] = TestUri };
    }

    private static JsonObject Position(string source, string needle)
    {
        var offset = source.IndexOf(needle, StringComparison.Ordinal);
        Assert.True(offset >= 0, $"Needle '{needle}' not found.");
        return PositionAtOffset(source, offset);
    }

    private static JsonObject PositionOfLast(string source, string needle)
    {
        var offset = source.LastIndexOf(needle, StringComparison.Ordinal);
        Assert.True(offset >= 0, $"Needle '{needle}' not found.");
        return PositionAtOffset(source, offset);
    }

    private static JsonObject PositionAtOffset(string source, int offset)
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

        return new JsonObject { ["line"] = line, ["character"] = character };
    }

    private static int LocationStartLine(JsonNode location)
    {
        return location
            .AsObject()["range"]!
            .AsObject()["start"]!
            .AsObject()["line"]!
            .GetValue<int>();
    }

    private static int[] SemanticTokenTypes(JsonArray data)
    {
        Assert.Equal(0, data.Count % 5);
        var types = new int[data.Count / 5];
        for (var i = 0; i < data.Count; i += 5)
        {
            types[i / 5] = data[i + 3]!.GetValue<int>();
        }

        return types;
    }

    private static JsonArray PublishedDiagnostics(
        AuroraScript.LanguageServer.Protocol.LspResult result,
        string uri)
    {
        foreach (var notification in result.Notifications)
        {
            if (notification.Method == "textDocument/publishDiagnostics" &&
                notification.Parameters["uri"]!.GetValue<string>() == uri)
            {
                return notification.Parameters["diagnostics"]!.AsArray();
            }
        }

        Assert.Fail($"No publishDiagnostics notification for {uri}.");
        return new JsonArray();
    }

    private const string TestUri = "file:///D:/workspace/test.as";
}
