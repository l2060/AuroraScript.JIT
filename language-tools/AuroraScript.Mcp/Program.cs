using AuroraScript;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

var server = new AuroraMcpServer();
await server.RunAsync();

internal sealed class AuroraMcpServer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly Dictionary<string, LanguageResource> _resources;
    private readonly string _languagePackRoot;

    public AuroraMcpServer()
    {
        _languagePackRoot = ResolveLanguagePackRoot();
        _resources = CreateResources(_languagePackRoot);
    }

    public async Task RunAsync()
    {
        using var input = Console.OpenStandardInput();
        using var output = Console.OpenStandardOutput();
        var reader = new StreamReader(input, Encoding.UTF8, detectEncodingFromByteOrderMarks: false);
        var writer = new StreamWriter(output, new UTF8Encoding(false)) { AutoFlush = true };

        while (await reader.ReadLineAsync().ConfigureAwait(false) is { } line)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            JsonObject? request;
            try
            {
                request = JsonNode.Parse(line) as JsonObject;
            }
            catch (JsonException)
            {
                continue;
            }

            if (request == null)
            {
                continue;
            }

            var response = await HandleRequestAsync(request).ConfigureAwait(false);
            if (response != null)
            {
                await writer.WriteLineAsync(response.ToJsonString(JsonOptions)).ConfigureAwait(false);
            }
        }
    }

    private async Task<JsonObject?> HandleRequestAsync(JsonObject request)
    {
        var id = request["id"]?.DeepClone();
        var method = request["method"]?.GetValue<string>();
        var parameters = request["params"] as JsonObject;

        try
        {
            return method switch
            {
                "initialize" => Response(id, InitializeResult(parameters)),
                "notifications/initialized" => null,
                "ping" => Response(id, new JsonObject()),
                "resources/list" => Response(id, ListResources()),
                "resources/read" => Response(id, ReadResource(parameters)),
                "tools/list" => Response(id, ListTools()),
                "tools/call" => Response(id, await CallToolAsync(parameters).ConfigureAwait(false)),
                _ => Error(id, -32601, $"Method not found: {method}")
            };
        }
        catch (Exception ex)
        {
            return Error(id, -32603, ex.Message);
        }
    }

    private static JsonObject InitializeResult(JsonObject? parameters)
    {
        var protocolVersion = parameters?["protocolVersion"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(protocolVersion))
        {
            protocolVersion = "2025-06-18";
        }

        return new JsonObject
        {
            ["protocolVersion"] = protocolVersion,
            ["serverInfo"] = new JsonObject
            {
                ["name"] = "AuroraScript.Mcp",
                ["version"] = "2.1.1"
            },
            ["capabilities"] = new JsonObject
            {
                ["resources"] = new JsonObject(),
                ["tools"] = new JsonObject()
            }
        };
    }

    private JsonObject ListResources()
    {
        var resources = new JsonArray();
        foreach (var resource in _resources.Values)
        {
            resources.Add(new JsonObject
            {
                ["uri"] = resource.Uri,
                ["name"] = resource.Name,
                ["description"] = resource.Description,
                ["mimeType"] = resource.MimeType
            });
        }

        return new JsonObject { ["resources"] = resources };
    }

    private JsonObject ReadResource(JsonObject? parameters)
    {
        var uri = parameters?["uri"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(uri) || !_resources.TryGetValue(uri, out var resource))
        {
            throw new InvalidOperationException($"Unknown resource: {uri}");
        }

        var text = File.ReadAllText(resource.Path, Encoding.UTF8);
        return new JsonObject
        {
            ["contents"] = new JsonArray
            {
                new JsonObject
                {
                    ["uri"] = resource.Uri,
                    ["mimeType"] = resource.MimeType,
                    ["text"] = text
                }
            }
        };
    }

    private static JsonObject ListTools()
    {
        return new JsonObject
        {
            ["tools"] = new JsonArray
            {
                Tool(
                    "aurora_get_document",
                    "Read an AuroraScript language-pack document by id.",
                    new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["id"] = new JsonObject
                            {
                                ["type"] = "string",
                                ["enum"] = new JsonArray("ai", "language", "performance", "compiler-runtime-map", "ebnf", "features", "runtime-api", "examples")
                            }
                        },
                        ["required"] = new JsonArray("id")
                    }),
                Tool(
                    "aurora_list_features",
                    "Return structured AuroraScript feature metadata.",
                    new JsonObject { ["type"] = "object", ["properties"] = new JsonObject() }),
                Tool(
                    "aurora_check_script",
                    "Compile-check AuroraScript source as a full module or a CompileBlock body.",
                    new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["source"] = new JsonObject { ["type"] = "string" },
                            ["mode"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("module", "block") },
                            ["sourceName"] = new JsonObject { ["type"] = "string" },
                            ["baseDirectory"] = new JsonObject { ["type"] = "string" },
                            ["parameters"] = new JsonObject
                            {
                                ["type"] = "array",
                                ["items"] = new JsonObject { ["type"] = "string" }
                            }
                        },
                        ["required"] = new JsonArray("source")
                    }),
                Tool(
                    "aurora_explain_diagnostic",
                    "Explain common AuroraScript compiler diagnostics and likely fixes.",
                    new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["message"] = new JsonObject { ["type"] = "string" }
                        },
                        ["required"] = new JsonArray("message")
                    })
            }
        };
    }

    private async Task<JsonObject> CallToolAsync(JsonObject? parameters)
    {
        var name = parameters?["name"]?.GetValue<string>();
        var arguments = parameters?["arguments"] as JsonObject ?? new JsonObject();

        return name switch
        {
            "aurora_get_document" => ToolText(ReadDocument(arguments)),
            "aurora_list_features" => ToolText(File.ReadAllText(Path.Combine(_languagePackRoot, "schema", "language-features.json"), Encoding.UTF8), "application/json"),
            "aurora_check_script" => ToolJson(await CheckScriptAsync(arguments).ConfigureAwait(false)),
            "aurora_explain_diagnostic" => ToolText(ExplainDiagnostic(arguments["message"]?.GetValue<string>() ?? string.Empty)),
            _ => throw new InvalidOperationException($"Unknown tool: {name}")
        };
    }

    private string ReadDocument(JsonObject arguments)
    {
        var id = arguments["id"]?.GetValue<string>() ?? "ai";
        var uri = id switch
        {
            "ai" => "aurora://docs/ai",
            "language" => "aurora://docs/language",
            "performance" => "aurora://docs/performance",
            "compiler-runtime-map" => "aurora://docs/compiler-runtime-map",
            "ebnf" => "aurora://schema/ebnf",
            "features" => "aurora://schema/features",
            "runtime-api" => "aurora://schema/runtime-api",
            "examples" => "aurora://examples/manifest",
            _ => throw new InvalidOperationException($"Unknown document id: {id}")
        };

        return File.ReadAllText(_resources[uri].Path, Encoding.UTF8);
    }

    private static async Task<JsonObject> CheckScriptAsync(JsonObject arguments)
    {
        var source = arguments["source"]?.GetValue<string>() ?? string.Empty;
        var mode = arguments["mode"]?.GetValue<string>() ?? "module";
        var sourceName = arguments["sourceName"]?.GetValue<string>() ?? (mode == "block" ? "__mcp_block__.as" : "__mcp_module__.as");
        var baseDirectory = arguments["baseDirectory"]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            baseDirectory = Path.Combine(Path.GetTempPath(), "AuroraScript.Mcp");
        }
        Directory.CreateDirectory(baseDirectory);

        var options = EngineOptions.Default
            .WithCompiler(compiler =>
            {
                compiler.Directory = baseDirectory;
                compiler.Mode = CompilationMode.Dynamic;
            })
            .WithRuntime(runtime => runtime.HotReload = false)
            .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release);

        var engine = new AuroraEngine(options);

        try
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(mode, "block"))
            {
                var parameters = ReadStringArray(arguments["parameters"] as JsonArray);
                using var block = engine.CompileBlock(source, new CompileBlockOptions
                {
                    SourceName = sourceName,
                    Parameters = parameters
                });
            }
            else
            {
                await engine.BuildAsync(engine.MemorySource(sourceName, source)).ConfigureAwait(false);
            }

            return new JsonObject
            {
                ["valid"] = true,
                ["diagnostics"] = new JsonArray()
            };
        }
        catch (AuroraCompilationException ex)
        {
            var diagnostics = new JsonArray();
            foreach (var diagnostic in ex.Diagnostics)
            {
                diagnostics.Add(new JsonObject
                {
                    ["message"] = diagnostic.Message,
                    ["fileName"] = diagnostic.FileName,
                    ["lineNumber"] = diagnostic.LineNumber,
                    ["columnNumber"] = diagnostic.ColumnNumber,
                    ["endLineNumber"] = diagnostic.Location.EndLine,
                    ["endColumnNumber"] = diagnostic.Location.EndColumn,
                    ["offset"] = diagnostic.Location.Offset,
                    ["length"] = diagnostic.Location.Length
                });
            }

            return new JsonObject
            {
                ["valid"] = false,
                ["diagnostics"] = diagnostics
            };
        }
        catch (Exception ex)
        {
            return new JsonObject
            {
                ["valid"] = false,
                ["diagnostics"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["message"] = ex.Message,
                        ["fileName"] = sourceName,
                        ["lineNumber"] = -1,
                        ["columnNumber"] = 0
                    }
                }
            };
        }
    }

    private static IReadOnlyList<string> ReadStringArray(JsonArray? array)
    {
        if (array == null || array.Count == 0)
        {
            return Array.Empty<string>();
        }

        var result = new string[array.Count];
        for (var i = 0; i < array.Count; i++)
        {
            result[i] = array[i]?.GetValue<string>() ?? string.Empty;
        }

        return result;
    }

    private static string ExplainDiagnostic(string message)
    {
        if (message.Contains("Duplicate declaration", StringComparison.OrdinalIgnoreCase))
        {
            return "A name was declared where AuroraScript does not allow it. Same-scope duplicates are rejected. A child block may shadow an outer var, but it may not redeclare a visible outer const. Rename the inner variable or remove the duplicate declaration.";
        }

        if (message.Contains("Cannot assign to constant", StringComparison.OrdinalIgnoreCase))
        {
            return "A const binding was used as a mutation target. Replace const with var if mutation is intended, or create a new variable instead of assigning to the const.";
        }

        if (message.Contains("must be placed at the top of the module", StringComparison.OrdinalIgnoreCase))
        {
            return "Module metadata, import, and include statements must appear before ordinary module statements.";
        }

        if (message.Contains("CompileBlock does not support module-level statement", StringComparison.OrdinalIgnoreCase))
        {
            return "CompileBlock accepts a function body, not a complete module. Remove @module/import/export/declare or validate the script as mode=module.";
        }

        if (message.Contains("break statement must be inside a loop", StringComparison.OrdinalIgnoreCase) ||
            message.Contains("continue statement must be inside a loop", StringComparison.OrdinalIgnoreCase))
        {
            return "break and continue are only valid inside for, for-in, or while loops.";
        }

        return "No specialized explanation is available. Check aurora-script-ai.md and run aurora_check_script for structured diagnostics.";
    }

    private static JsonObject Tool(string name, string description, JsonObject inputSchema)
    {
        return new JsonObject
        {
            ["name"] = name,
            ["description"] = description,
            ["inputSchema"] = inputSchema
        };
    }

    private static JsonObject ToolText(string text, string mimeType = "text/markdown")
    {
        return new JsonObject
        {
            ["content"] = new JsonArray
            {
                new JsonObject
                {
                    ["type"] = "text",
                    ["mimeType"] = mimeType,
                    ["text"] = text
                }
            }
        };
    }

    private static JsonObject ToolJson(JsonObject value)
    {
        return ToolText(value.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web) { WriteIndented = true }), "application/json");
    }

    private static JsonObject Response(JsonNode? id, JsonObject result)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["result"] = result
        };
    }

    private static JsonObject Error(JsonNode? id, int code, string message)
    {
        return new JsonObject
        {
            ["jsonrpc"] = "2.0",
            ["id"] = id,
            ["error"] = new JsonObject
            {
                ["code"] = code,
                ["message"] = message
            }
        };
    }

    private static Dictionary<string, LanguageResource> CreateResources(string root)
    {
        var resources = new[]
        {
            new LanguageResource("aurora://docs/ai", "AuroraScript AI reference", "Primary AI reference for syntax, semantics, runtime APIs, and pitfalls.", "text/markdown", Path.Combine(root, "docs", "aurora-script-ai.md")),
            new LanguageResource("aurora://docs/language", "AuroraScript language reference", "Human-readable language reference.", "text/markdown", Path.Combine(root, "docs", "language-reference.md")),
            new LanguageResource("aurora://docs/performance", "AuroraScript performance best practices", "Performance guidance for compiler and runtime usage.", "text/markdown", Path.Combine(root, "docs", "performance-best-practices.md")),
            new LanguageResource("aurora://docs/compiler-runtime-map", "AuroraScript compiler runtime map", "Source map for maintainers and AI agents.", "text/markdown", Path.Combine(root, "docs", "compiler-runtime-map.md")),
            new LanguageResource("aurora://schema/ebnf", "AuroraScript EBNF grammar", "Documentation grammar summary.", "text/plain", Path.Combine(root, "schema", "aurora-script.ebnf")),
            new LanguageResource("aurora://schema/features", "AuroraScript language features", "Structured feature index.", "application/json", Path.Combine(root, "schema", "language-features.json")),
            new LanguageResource("aurora://schema/runtime-api", "AuroraScript runtime API", "Structured runtime API metadata.", "application/json", Path.Combine(root, "schema", "runtime-api.json")),
            new LanguageResource("aurora://examples/manifest", "AuroraScript examples manifest", "Valid and invalid example metadata.", "application/json", Path.Combine(root, "examples", "examples.manifest.json"))
        };

        return resources.ToDictionary(resource => resource.Uri, StringComparer.Ordinal);
    }

    private static string ResolveLanguagePackRoot()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, ".language"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", ".language")),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", ".language")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), ".language"))
        };

        for (var i = 0; i < candidates.Length; i++)
        {
            if (File.Exists(Path.Combine(candidates[i], "docs", "aurora-script-ai.md")))
            {
                return candidates[i];
            }
        }

        throw new DirectoryNotFoundException("Could not locate .language directory.");
    }

    private sealed record LanguageResource(
        string Uri,
        string Name,
        string Description,
        string MimeType,
        string Path);
}
