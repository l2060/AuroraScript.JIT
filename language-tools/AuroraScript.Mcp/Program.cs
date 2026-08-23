using AuroraScript;
using AuroraScript.Core;
using AuroraScript.Runtime;
using AuroraScript.Runtime.Interop;
using AuroraScript.Runtime.Types;
using AuroraScript.Source;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

var server = new AuroraMcpServer();
await server.RunAsync();

internal sealed class AuroraMcpServer
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    private readonly Dictionary<string, LanguageResource> _resources;

    public AuroraMcpServer()
    {
        _resources = CreateResources();
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
                ["version"] = "4.0.0"
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

        var text = resource.ReadText();
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
                                ["description"] = "Document id or resource-relative path. Use aurora_list_documents to discover available ids.",
                                ["enum"] = new JsonArray(
                                    "ai",
                                    "script-best-practices",
                                    "language",
                                    "performance",
                                    "compiler-runtime-map",
                                    "host-integration",
                                    "ebnf",
                                    "features",
                                    "runtime-api",
                                    "host-api",
                                    "runtime-api-schema",
                                    "host-api-schema",
                                    "ast-schema",
                                    "diagnostics-schema",
                                    "examples-schema",
                                    "examples",
                                    "llms",
                                    "readme")
                            }
                        },
                        ["required"] = new JsonArray("id")
                    }),
                Tool(
                    "aurora_list_documents",
                    "List all embedded AuroraScript documents and schemas available through the MCP server.",
                    new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["prefix"] = new JsonObject
                            {
                                ["type"] = "string",
                                ["description"] = "Optional relative path prefix such as docs/, schema/, or examples/."
                            }
                        }
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
                            ["parameters"] = new JsonObject
                            {
                                ["type"] = "array",
                                ["items"] = new JsonObject { ["type"] = "string" }
                            },
                            ["sources"] = new JsonObject
                            {
                                ["type"] = "object",
                                ["description"] = "Additional in-memory source files keyed by path, for import/include dependencies."
                            }
                        },
                        ["required"] = new JsonArray("source")
                    }),
                Tool(
                    "aurora_run_script",
                    "Compile and run AuroraScript source, returning result, stdout, stderr, diagnostics, and runtime errors.",
                    new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["source"] = new JsonObject { ["type"] = "string" },
                            ["mode"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("module", "block") },
                            ["sourceName"] = new JsonObject { ["type"] = "string" },
                            ["moduleName"] = new JsonObject { ["type"] = "string", ["description"] = "Module name for module mode. Defaults to TEST." },
                            ["methodName"] = new JsonObject { ["type"] = "string", ["description"] = "Exported function to invoke for module mode. Defaults to run." },
                            ["parameters"] = new JsonObject
                            {
                                ["type"] = "array",
                                ["items"] = new JsonObject { ["type"] = "string" },
                                ["description"] = "CompileBlock parameter names for block mode."
                            },
                            ["arguments"] = new JsonObject
                            {
                                ["type"] = "array",
                                ["description"] = "JSON values converted to AuroraScript arguments."
                            },
                            ["sources"] = new JsonObject
                            {
                                ["type"] = "object",
                                ["description"] = "Additional in-memory source files keyed by path, for import/include dependencies."
                            }
                        },
                        ["required"] = new JsonArray("source")
                    }),
                Tool(
                    "aurora_check_file",
                    "Compile-check a file-system AuroraScript entry file and its import/include graph.",
                    new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["rootDirectory"] = new JsonObject { ["type"] = "string", ["description"] = "File-system resolver root." },
                            ["entryPath"] = new JsonObject { ["type"] = "string", ["description"] = "Entry .as path relative to rootDirectory, or an absolute path under rootDirectory." },
                            ["extName"] = new JsonObject { ["type"] = "string", ["description"] = "Script extension. Defaults to .as." },
                            ["sources"] = new JsonObject
                            {
                                ["type"] = "object",
                                ["description"] = "Optional in-memory overlay sources keyed by rootDirectory-relative path. Use '/' separators. Overlay wins when the resolved target is under rootDirectory."
                            }
                        },
                        ["required"] = new JsonArray("rootDirectory", "entryPath")
                    }),
                Tool(
                    "aurora_run_file",
                    "Compile and run a file-system AuroraScript entry file and its import/include graph.",
                    new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["rootDirectory"] = new JsonObject { ["type"] = "string", ["description"] = "File-system resolver root." },
                            ["entryPath"] = new JsonObject { ["type"] = "string", ["description"] = "Entry .as path relative to rootDirectory, or an absolute path under rootDirectory." },
                            ["moduleName"] = new JsonObject { ["type"] = "string", ["description"] = "Exported module to execute." },
                            ["methodName"] = new JsonObject { ["type"] = "string", ["description"] = "Exported function to invoke. Defaults to run." },
                            ["arguments"] = new JsonObject { ["type"] = "array", ["description"] = "JSON values converted to AuroraScript arguments." },
                            ["extName"] = new JsonObject { ["type"] = "string", ["description"] = "Script extension. Defaults to .as." },
                            ["sources"] = new JsonObject
                            {
                                ["type"] = "object",
                                ["description"] = "Optional in-memory overlay sources keyed by rootDirectory-relative path. Use '/' separators. Overlay wins when the resolved target is under rootDirectory."
                            }
                        },
                        ["required"] = new JsonArray("rootDirectory", "entryPath", "moduleName")
                    }),
                Tool(
                    "aurora_build_workspace",
                    "Compile all AuroraScript files visible under a file-system root through GetAllSourcesAsync.",
                    new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["rootDirectory"] = new JsonObject { ["type"] = "string", ["description"] = "File-system resolver root." },
                            ["extName"] = new JsonObject { ["type"] = "string", ["description"] = "Script extension. Defaults to .as." },
                            ["sources"] = new JsonObject
                            {
                                ["type"] = "object",
                                ["description"] = "Optional in-memory overlay sources keyed by rootDirectory-relative path. Use '/' separators. Overlay wins when the resolved target is under rootDirectory."
                            }
                        },
                        ["required"] = new JsonArray("rootDirectory")
                    }),
                Tool(
                    "aurora_search_runtime_api",
                    "Search the structured script-side runtime API index.",
                    new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["query"] = new JsonObject { ["type"] = "string", ["description"] = "Name or text to search, such as String.trim, fs.readText, http.get, or appendLine." },
                            ["limit"] = new JsonObject { ["type"] = "integer", ["description"] = "Maximum results. Defaults to 20." }
                        },
                        ["required"] = new JsonArray("query")
                    }),
                Tool(
                    "aurora_get_runtime_api",
                    "Get a structured runtime API entry by path, such as Math, Array.push, fs.readText, or http.getAsync.",
                    new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Runtime API path." }
                        },
                        ["required"] = new JsonArray("path")
                    }),
                Tool(
                    "aurora_list_examples",
                    "List valid or invalid AuroraScript examples from the language pack manifest.",
                    new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["kind"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("valid", "invalid", "all") }
                        }
                    }),
                Tool(
                    "aurora_get_example",
                    "Read an AuroraScript example by manifest path.",
                    new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["path"] = new JsonObject { ["type"] = "string", ["description"] = "Example path such as valid/templates.as or invalid/const-assignment.as." }
                        },
                        ["required"] = new JsonArray("path")
                    }),
                Tool(
                    "aurora_validate_best_practices",
                    "Check AuroraScript source for AI-authoring best-practice warnings.",
                    new JsonObject
                    {
                        ["type"] = "object",
                        ["properties"] = new JsonObject
                        {
                            ["source"] = new JsonObject { ["type"] = "string" },
                            ["mode"] = new JsonObject { ["type"] = "string", ["enum"] = new JsonArray("module", "block") }
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
            "aurora_get_document" => ToolResource(ReadDocument(arguments)),
            "aurora_list_documents" => ToolJson(ListDocuments(arguments)),
            "aurora_list_features" => ToolText(_resources["aurora://schema/features"].ReadText(), "application/json"),
            "aurora_check_script" => ToolJson(await CheckScriptAsync(arguments).ConfigureAwait(false)),
            "aurora_run_script" => ToolJson(await RunScriptAsync(arguments).ConfigureAwait(false)),
            "aurora_check_file" => ToolJson(await CheckFileAsync(arguments).ConfigureAwait(false)),
            "aurora_run_file" => ToolJson(await RunFileAsync(arguments).ConfigureAwait(false)),
            "aurora_build_workspace" => ToolJson(await BuildWorkspaceAsync(arguments).ConfigureAwait(false)),
            "aurora_search_runtime_api" => ToolJson(SearchRuntimeApi(arguments)),
            "aurora_get_runtime_api" => ToolJson(GetRuntimeApi(arguments)),
            "aurora_list_examples" => ToolJson(ListExamples(arguments)),
            "aurora_get_example" => ToolResource(GetExample(arguments)),
            "aurora_validate_best_practices" => ToolJson(ValidateBestPractices(arguments)),
            "aurora_explain_diagnostic" => ToolText(ExplainDiagnostic(arguments["message"]?.GetValue<string>() ?? string.Empty)),
            _ => throw new InvalidOperationException($"Unknown tool: {name}")
        };
    }

    private LanguageResource ReadDocument(JsonObject arguments)
    {
        var id = arguments["id"]?.GetValue<string>() ?? "ai";
        var uri = ResolveDocumentUri(id);

        return _resources[uri];
    }

    private JsonObject ListDocuments(JsonObject arguments)
    {
        var prefix = NormalizeResourcePath(arguments["prefix"]?.GetValue<string>() ?? string.Empty);
        var documents = new JsonArray();
        foreach (var resource in _resources.Values.OrderBy(resource => resource.RelativePath, StringComparer.Ordinal))
        {
            if (!string.IsNullOrEmpty(prefix) && !resource.RelativePath.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            documents.Add(new JsonObject
            {
                ["id"] = resource.Id,
                ["uri"] = resource.Uri,
                ["path"] = resource.RelativePath,
                ["name"] = resource.Name,
                ["description"] = resource.Description,
                ["mimeType"] = resource.MimeType
            });
        }

        return new JsonObject { ["documents"] = documents };
    }

    private string ResolveDocumentUri(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            id = "ai";
        }

        if (_resources.ContainsKey(id))
        {
            return id;
        }

        id = id.Trim();
        var alias = id.ToLowerInvariant() switch
        {
            "ai" => "docs/aurora-script-ai.md",
            "script-best-practices" => "docs/script-authoring-best-practices.md",
            "authoring" => "docs/script-authoring-best-practices.md",
            "best-practices" => "docs/script-authoring-best-practices.md",
            "language" => "docs/language-reference.md",
            "performance" => "docs/performance-best-practices.md",
            "compiler-runtime-map" => "docs/compiler-runtime-map.md",
            "host-integration" => "docs/host-integration.md",
            "ebnf" => "schema/aurora-script.ebnf",
            "features" => "schema/language-features.json",
            "runtime-api" => "schema/runtime-api.json",
            "host-api" => "schema/host-api.json",
            "runtime-api-schema" => "schema/runtime-api.schema.json",
            "host-api-schema" => "schema/host-api.schema.json",
            "ast-schema" => "schema/ast.schema.json",
            "diagnostics-schema" => "schema/diagnostics.schema.json",
            "examples-schema" => "schema/examples.schema.json",
            "examples" => "examples/examples.manifest.json",
            "llms" => "llms.txt",
            "readme" => "README.md",
            _ => id
        };

        var relativePath = NormalizeResourcePath(alias);
        foreach (var resource in _resources.Values)
        {
            if (string.Equals(resource.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(resource.Id, relativePath, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(resource.Id, id, StringComparison.OrdinalIgnoreCase))
            {
                return resource.Uri;
            }
        }

        throw new InvalidOperationException($"Unknown document id: {id}");
    }

    private static async Task<JsonObject> CheckScriptAsync(JsonObject arguments)
    {
        var source = arguments["source"]?.GetValue<string>() ?? string.Empty;
        var mode = arguments["mode"]?.GetValue<string>() ?? "module";
        var sourceName = arguments["sourceName"]?.GetValue<string>() ?? (mode == "block" ? "__mcp_block__.as" : "main.as");
        var memory = ScriptSources.Memory("mem://mcp-check/");
        memory.Add(sourceName, source);
        AddAdditionalSources(memory, arguments["sources"] as JsonObject);

        var options = EngineOptions.Default
            .WithCompiler(compiler =>
            {
                compiler.SourceResolver = memory;
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
                await engine.BuildAsync(sourceName).ConfigureAwait(false);
            }

            return new JsonObject
            {
                ["valid"] = true,
                ["diagnostics"] = new JsonArray()
            };
        }
        catch (AuroraCompilationException ex)
        {
            return new JsonObject
            {
                ["valid"] = false,
                ["diagnostics"] = ToDiagnosticsJson(ex)
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

    private static async Task<JsonObject> RunScriptAsync(JsonObject arguments)
    {
        var source = arguments["source"]?.GetValue<string>() ?? string.Empty;
        var mode = arguments["mode"]?.GetValue<string>() ?? "module";
        var sourceName = arguments["sourceName"]?.GetValue<string>() ?? (mode == "block" ? "__mcp_block__.as" : "main.as");
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var memory = ScriptSources.Memory("mem://mcp/");
        memory.Add(sourceName, source);
        AddAdditionalSources(memory, arguments["sources"] as JsonObject);

        var options = EngineOptions.Default
            .WithCompiler(compiler =>
            {
                compiler.SourceResolver = memory;
                compiler.Mode = CompilationMode.Dynamic;
            })
            .WithRuntime(runtime =>
            {
                runtime.HotReload = false;
                runtime.ConsoleStdOut = stdout;
                runtime.ConsoleErrorOut = stderr;
            })
            .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release);

        var engine = new AuroraEngine(options);
        var scriptArguments = ReadArgumentArray(arguments["arguments"] as JsonArray);

        try
        {
            ScriptDatum result;
            if (StringComparer.OrdinalIgnoreCase.Equals(mode, "block"))
            {
                var parameters = ReadStringArray(arguments["parameters"] as JsonArray);
                using var block = engine.CompileBlock(source, new CompileBlockOptions
                {
                    SourceName = sourceName,
                    Parameters = parameters
                });
                result = block.Invoke(scriptArguments);
            }
            else
            {
                var moduleName = arguments["moduleName"]?.GetValue<string>() ?? "TEST";
                var methodName = arguments["methodName"]?.GetValue<string>() ?? "run";
                await engine.BuildAsync(sourceName).ConfigureAwait(false);
                using var domain = engine.CreateDomain();
                result = domain.Execute(moduleName, methodName, ScriptObject.Null, scriptArguments);
            }

            return new JsonObject
            {
                ["success"] = true,
                ["result"] = ToJsonNode(result, options),
                ["resultText"] = ScriptDatum.ToString(result),
                ["stdout"] = stdout.ToString(),
                ["stderr"] = stderr.ToString(),
                ["diagnostics"] = new JsonArray()
            };
        }
        catch (AuroraCompilationException ex)
        {
            return new JsonObject
            {
                ["success"] = false,
                ["stage"] = "compile",
                ["message"] = ex.Message,
                ["stdout"] = stdout.ToString(),
                ["stderr"] = stderr.ToString(),
                ["diagnostics"] = ToDiagnosticsJson(ex)
            };
        }
        catch (AuroraRuntimeException ex)
        {
            return new JsonObject
            {
                ["success"] = false,
                ["stage"] = "runtime",
                ["message"] = ex.Message,
                ["stdout"] = stdout.ToString(),
                ["stderr"] = stderr.ToString(),
                ["stackTrace"] = ToStackTraceJson(ex)
            };
        }
        catch (Exception ex)
        {
            return new JsonObject
            {
                ["success"] = false,
                ["stage"] = "host",
                ["message"] = ex.Message,
                ["stdout"] = stdout.ToString(),
                ["stderr"] = stderr.ToString()
            };
        }
    }

    private static async Task<JsonObject> CheckFileAsync(JsonObject arguments)
    {
        var rootDirectory = ReadRequiredString(arguments, "rootDirectory");
        var entryPath = NormalizeEntryPath(rootDirectory, ReadRequiredString(arguments, "entryPath"));
        var options = CreateFileEngineOptions(arguments, new StringWriter(), new StringWriter());
        var engine = new AuroraEngine(options);

        try
        {
            await engine.BuildAsync(entryPath).ConfigureAwait(false);
            return new JsonObject
            {
                ["valid"] = true,
                ["rootDirectory"] = Path.GetFullPath(rootDirectory),
                ["entryPath"] = entryPath,
                ["diagnostics"] = new JsonArray()
            };
        }
        catch (AuroraCompilationException ex)
        {
            return new JsonObject
            {
                ["valid"] = false,
                ["rootDirectory"] = Path.GetFullPath(rootDirectory),
                ["entryPath"] = entryPath,
                ["diagnostics"] = ToDiagnosticsJson(ex)
            };
        }
        catch (Exception ex)
        {
            return new JsonObject
            {
                ["valid"] = false,
                ["rootDirectory"] = Path.GetFullPath(rootDirectory),
                ["entryPath"] = entryPath,
                ["diagnostics"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["message"] = ex.Message,
                        ["fileName"] = entryPath,
                        ["lineNumber"] = -1,
                        ["columnNumber"] = 0
                    }
                }
            };
        }
    }

    private static async Task<JsonObject> RunFileAsync(JsonObject arguments)
    {
        var rootDirectory = ReadRequiredString(arguments, "rootDirectory");
        var entryPath = NormalizeEntryPath(rootDirectory, ReadRequiredString(arguments, "entryPath"));
        var moduleName = ReadRequiredString(arguments, "moduleName");
        var methodName = arguments["methodName"]?.GetValue<string>() ?? "run";
        var stdout = new StringWriter();
        var stderr = new StringWriter();
        var options = CreateFileEngineOptions(arguments, stdout, stderr);
        var engine = new AuroraEngine(options);
        var scriptArguments = ReadArgumentArray(arguments["arguments"] as JsonArray);

        try
        {
            await engine.BuildAsync(entryPath).ConfigureAwait(false);
            using var domain = engine.CreateDomain();
            var result = domain.Execute(moduleName, methodName, ScriptObject.Null, scriptArguments);
            return new JsonObject
            {
                ["success"] = true,
                ["rootDirectory"] = Path.GetFullPath(rootDirectory),
                ["entryPath"] = entryPath,
                ["moduleName"] = moduleName,
                ["methodName"] = methodName,
                ["result"] = ToJsonNode(result, options),
                ["resultText"] = ScriptDatum.ToString(result),
                ["stdout"] = stdout.ToString(),
                ["stderr"] = stderr.ToString(),
                ["diagnostics"] = new JsonArray()
            };
        }
        catch (AuroraCompilationException ex)
        {
            return new JsonObject
            {
                ["success"] = false,
                ["stage"] = "compile",
                ["message"] = ex.Message,
                ["rootDirectory"] = Path.GetFullPath(rootDirectory),
                ["entryPath"] = entryPath,
                ["stdout"] = stdout.ToString(),
                ["stderr"] = stderr.ToString(),
                ["diagnostics"] = ToDiagnosticsJson(ex)
            };
        }
        catch (AuroraRuntimeException ex)
        {
            return new JsonObject
            {
                ["success"] = false,
                ["stage"] = "runtime",
                ["message"] = ex.Message,
                ["rootDirectory"] = Path.GetFullPath(rootDirectory),
                ["entryPath"] = entryPath,
                ["stdout"] = stdout.ToString(),
                ["stderr"] = stderr.ToString(),
                ["stackTrace"] = ToStackTraceJson(ex)
            };
        }
        catch (Exception ex)
        {
            return new JsonObject
            {
                ["success"] = false,
                ["stage"] = "host",
                ["message"] = ex.Message,
                ["rootDirectory"] = Path.GetFullPath(rootDirectory),
                ["entryPath"] = entryPath,
                ["stdout"] = stdout.ToString(),
                ["stderr"] = stderr.ToString()
            };
        }
    }

    private static async Task<JsonObject> BuildWorkspaceAsync(JsonObject arguments)
    {
        var rootDirectory = ReadRequiredString(arguments, "rootDirectory");
        var options = CreateFileEngineOptions(arguments, new StringWriter(), new StringWriter());
        var engine = new AuroraEngine(options);

        try
        {
            await engine.BuildAsync().ConfigureAwait(false);
            return new JsonObject
            {
                ["success"] = true,
                ["rootDirectory"] = Path.GetFullPath(rootDirectory),
                ["diagnostics"] = new JsonArray()
            };
        }
        catch (AuroraCompilationException ex)
        {
            return new JsonObject
            {
                ["success"] = false,
                ["stage"] = "compile",
                ["message"] = ex.Message,
                ["rootDirectory"] = Path.GetFullPath(rootDirectory),
                ["diagnostics"] = ToDiagnosticsJson(ex)
            };
        }
        catch (Exception ex)
        {
            return new JsonObject
            {
                ["success"] = false,
                ["stage"] = "host",
                ["message"] = ex.Message,
                ["rootDirectory"] = Path.GetFullPath(rootDirectory)
            };
        }
    }

    private JsonObject SearchRuntimeApi(JsonObject arguments)
    {
        var query = ReadRequiredString(arguments, "query");
        var limit = Math.Clamp(arguments["limit"]?.GetValue<int>() ?? 20, 1, 100);
        var api = JsonNode.Parse(_resources["aurora://schema/runtime-api"].ReadText()) as JsonObject
            ?? throw new InvalidOperationException("runtime-api.json is not an object.");
        var results = new JsonArray();

        foreach (var match in EnumerateRuntimeApiEntries(api)
            .Where(entry => entry.Matches(query))
            .Take(limit))
        {
            results.Add(match.ToJson());
        }

        return new JsonObject
        {
            ["query"] = query,
            ["results"] = results
        };
    }

    private JsonObject GetRuntimeApi(JsonObject arguments)
    {
        var path = ReadRequiredString(arguments, "path");
        var api = JsonNode.Parse(_resources["aurora://schema/runtime-api"].ReadText()) as JsonObject
            ?? throw new InvalidOperationException("runtime-api.json is not an object.");
        foreach (var entry in EnumerateRuntimeApiEntries(api))
        {
            if (string.Equals(entry.Path, path, StringComparison.OrdinalIgnoreCase))
            {
                return entry.ToJson();
            }
        }

        throw new InvalidOperationException($"Runtime API entry not found: {path}");
    }

    private JsonObject ListExamples(JsonObject arguments)
    {
        var kind = arguments["kind"]?.GetValue<string>() ?? "all";
        var manifest = JsonNode.Parse(_resources["aurora://examples/manifest"].ReadText()) as JsonObject
            ?? throw new InvalidOperationException("examples manifest is not an object.");
        var result = new JsonObject();
        if (StringComparer.OrdinalIgnoreCase.Equals(kind, "valid") || StringComparer.OrdinalIgnoreCase.Equals(kind, "all"))
        {
            result["valid"] = manifest["valid"]?.DeepClone() ?? new JsonArray();
        }

        if (StringComparer.OrdinalIgnoreCase.Equals(kind, "invalid") || StringComparer.OrdinalIgnoreCase.Equals(kind, "all"))
        {
            result["invalid"] = manifest["invalid"]?.DeepClone() ?? new JsonArray();
        }

        return result;
    }

    private LanguageResource GetExample(JsonObject arguments)
    {
        var path = NormalizeResourcePath(ReadRequiredString(arguments, "path"));
        if (!path.StartsWith("examples/", StringComparison.OrdinalIgnoreCase))
        {
            path = "examples/" + path;
        }

        foreach (var resource in _resources.Values)
        {
            if (string.Equals(resource.RelativePath, path, StringComparison.OrdinalIgnoreCase))
            {
                return resource;
            }
        }

        throw new InvalidOperationException($"Example not found: {path}");
    }

    private static JsonObject ValidateBestPractices(JsonObject arguments)
    {
        var source = arguments["source"]?.GetValue<string>() ?? string.Empty;
        var mode = arguments["mode"]?.GetValue<string>() ?? "module";
        var warnings = new JsonArray();

        AddPatternWarnings(
            warnings,
            source,
            @"for\s*\([^;]*;[^;]*<[^;]*\.[A-Za-z_][A-Za-z0-9_]*\s*;[^)]*\)",
            "loop.dynamic-bound",
            "Cache dynamic loop bounds such as items.length in a local variable before the loop.");
        AddPatternWarnings(
            warnings,
            source,
            @"\b(let|class|undefined)\b|===|!==",
            "syntax.javascript",
            "Avoid JavaScript-only syntax or assumptions; use AuroraScript syntax and null checks.");
        AddPatternWarnings(
            warnings,
            source,
            @"\bvar\s+[A-Za-z_$\u4e00-\u9fbb][\w$\u4e00-\u9fbb]*\s*=.*?,\s*[A-Za-z_$\u4e00-\u9fbb]",
            "declaration.multi-binding",
            "Declare one name per var or const statement.");

        if (StringComparer.OrdinalIgnoreCase.Equals(mode, "block"))
        {
            AddPatternWarnings(
                warnings,
                source,
                @"@(module|global)\b|\b(import|include|export|declare)\b",
                "block.module-syntax",
                "CompileBlock accepts a function body only; do not use file or module syntax.");
        }

        return new JsonObject
        {
            ["valid"] = warnings.Count == 0,
            ["warnings"] = warnings
        };
    }

    private static EngineOptions CreateFileEngineOptions(JsonObject arguments, TextWriter stdout, TextWriter stderr)
    {
        var rootDirectory = ReadRequiredString(arguments, "rootDirectory");
        var extName = arguments["extName"]?.GetValue<string>() ?? ".as";
        var resolver = CreateFileResolver(rootDirectory, arguments["sources"] as JsonObject);
        return EngineOptions.Default
            .WithCompiler(compiler =>
            {
                compiler.SourceResolver = resolver;
                compiler.Mode = CompilationMode.Dynamic;
                compiler.ExtName = extName;
            })
            .WithRuntime(runtime =>
            {
                runtime.HotReload = false;
                runtime.ConsoleStdOut = stdout;
                runtime.ConsoleErrorOut = stderr;
            })
            .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release);
    }

    private static IScriptSourceResolver CreateFileResolver(string rootDirectory, JsonObject? overlaySources)
    {
        var root = Path.GetFullPath(rootDirectory);
        if (!Directory.Exists(root))
        {
            throw new DirectoryNotFoundException($"Script root directory not found: {root}");
        }

        var fileSystem = ScriptSources.FileSystem(root, Encoding.UTF8);
        if (overlaySources == null || overlaySources.Count == 0)
        {
            return fileSystem;
        }

        var memory = ScriptSources.Memory(root);
        AddAdditionalSources(memory, overlaySources);
        return ScriptSources.Composite(memory, fileSystem);
    }

    private static string NormalizeEntryPath(string rootDirectory, string entryPath)
    {
        if (!Path.IsPathRooted(entryPath))
        {
            return NormalizeResourcePath(entryPath);
        }

        var root = Path.GetFullPath(rootDirectory);
        var fullPath = Path.GetFullPath(entryPath);
        var relative = Path.GetRelativePath(root, fullPath);
        if (relative.StartsWith("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
        {
            throw new ArgumentException("entryPath must be inside rootDirectory.", nameof(entryPath));
        }

        return NormalizeResourcePath(relative);
    }

    private static void AddAdditionalSources(MemorySourceResolver resolver, JsonObject? sources)
    {
        if (sources == null)
        {
            return;
        }

        foreach (var pair in sources)
        {
            resolver.Add(pair.Key, pair.Value?.GetValue<string>() ?? string.Empty);
        }
    }

    private static IEnumerable<RuntimeApiEntry> EnumerateRuntimeApiEntries(JsonObject api)
    {
        if (api["modules"] is JsonObject modules)
        {
            foreach (var module in modules)
            {
                if (module.Value is not JsonObject node)
                {
                    continue;
                }

                yield return RuntimeApiEntry.FromJson(module.Key, "module", node);
                if (node["members"] is not JsonObject members)
                {
                    continue;
                }

                foreach (var member in members)
                {
                    if (member.Value is JsonObject memberNode)
                    {
                        yield return RuntimeApiEntry.FromJson(
                            $"{module.Key}.{member.Key}",
                            "module-member",
                            memberNode);
                    }
                }
            }
        }

        if (api["globals"] is JsonObject globals)
        {
            foreach (var global in globals)
            {
                var node = global.Value as JsonObject;
                if (node == null)
                {
                    continue;
                }

                yield return RuntimeApiEntry.FromJson(global.Key, "global", node);
                if (node["constructors"] is JsonArray constructors)
                {
                    foreach (var constructor in constructors)
                    {
                        if (constructor is JsonObject constructorNode)
                        {
                            yield return RuntimeApiEntry.FromJson($"new {global.Key}", "constructor", constructorNode, global.Key);
                        }
                    }
                }

                if (node["members"] is JsonObject members)
                {
                    foreach (var member in members)
                    {
                        if (member.Value is JsonObject memberNode)
                        {
                            yield return RuntimeApiEntry.FromJson($"{global.Key}.{member.Key}", "global-member", memberNode);
                        }
                    }
                }
            }
        }

        if (api["prototypes"] is JsonObject prototypes)
        {
            foreach (var prototype in prototypes)
            {
                if (prototype.Value is not JsonObject members)
                {
                    continue;
                }

                foreach (var member in members)
                {
                    if (member.Value is JsonObject memberNode)
                    {
                        yield return RuntimeApiEntry.FromJson($"{prototype.Key}.{member.Key}", "prototype-member", memberNode);
                    }
                }
            }
        }
    }

    private static void AddPatternWarnings(
        JsonArray warnings,
        string source,
        string pattern,
        string code,
        string message)
    {
        foreach (Match match in Regex.Matches(source, pattern, RegexOptions.Multiline))
        {
            var (line, column) = GetLineColumn(source, match.Index);
            warnings.Add(new JsonObject
            {
                ["code"] = code,
                ["message"] = message,
                ["lineNumber"] = line,
                ["columnNumber"] = column,
                ["text"] = match.Value
            });
        }
    }

    private static (int Line, int Column) GetLineColumn(string source, int offset)
    {
        var line = 1;
        var column = 1;
        for (var i = 0; i < offset && i < source.Length; i++)
        {
            if (source[i] == '\n')
            {
                line++;
                column = 1;
            }
            else
            {
                column++;
            }
        }

        return (line, column);
    }

    private static string ReadRequiredString(JsonObject arguments, string name)
    {
        var value = arguments[name]?.GetValue<string>();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException($"'{name}' is required.", name);
        }

        return value;
    }

    private static ScriptDatum[] ReadArgumentArray(JsonArray? array)
    {
        if (array == null || array.Count == 0)
        {
            return Array.Empty<ScriptDatum>();
        }

        var result = new ScriptDatum[array.Count];
        for (var i = 0; i < array.Count; i++)
        {
            result[i] = ToScriptDatum(array[i]);
        }

        return result;
    }

    private static ScriptDatum ToScriptDatum(JsonNode? node)
    {
        if (node == null)
        {
            return ScriptDatum.Null;
        }

        var json = node.ToJsonString(JsonOptions);
        var scriptObject = EngineOptions.Default.Runtime.JsonSerializer.Deserialize(json, EngineOptions.Default);
        return ScriptDatum.FromObject(scriptObject);
    }

    private static JsonNode? ToJsonNode(ScriptDatum result, EngineOptions options)
    {
        var json = options.Runtime.JsonSerializer.Serialize(result, options, false);
        return JsonNode.Parse(json);
    }

    private static JsonArray ToDiagnosticsJson(AuroraCompilationException ex)
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

        return diagnostics;
    }

    private static JsonArray ToStackTraceJson(AuroraRuntimeException ex)
    {
        var frames = new JsonArray();
        if (ex.StackTrace == null)
        {
            return frames;
        }

        foreach (var frame in ex.StackTrace)
        {
            frames.Add(frame.ToString());
        }

        return frames;
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
            return "CompileBlock accepts a function body, not a complete file or module. Remove @module/@global/import/export/declare or validate the script as mode=module.";
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

    private static JsonObject ToolResource(LanguageResource resource)
    {
        return ToolText(resource.ReadText(), resource.MimeType);
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

    private static Dictionary<string, LanguageResource> CreateResources()
    {
        var resources = new Dictionary<string, LanguageResource>(StringComparer.Ordinal);
        foreach (var relativePath in DiscoverDocumentPaths())
        {
            var resource = CreateResource(relativePath);
            resources[resource.Uri] = resource;
        }

        return resources;
    }

    private static IEnumerable<string> DiscoverDocumentPaths()
    {
        var paths = new SortedSet<string>(StringComparer.OrdinalIgnoreCase);
        var assembly = Assembly.GetExecutingAssembly();
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            var relativePath = LanguageResource.TryGetRelativePath(resourceName);
            if (relativePath != null)
            {
                paths.Add(relativePath);
            }
        }

        foreach (var root in LanguageResource.GetDocumentRootCandidates())
        {
            if (!Directory.Exists(root))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(root, "*.*", SearchOption.AllDirectories))
            {
                paths.Add(NormalizeResourcePath(Path.GetRelativePath(root, file)));
            }
        }

        return paths;
    }

    private static LanguageResource CreateResource(string relativePath)
    {
        var normalized = NormalizeResourcePath(relativePath);
        var id = CreateDocumentId(normalized);
        var uri = GetKnownUri(normalized) ?? "aurora://documents/" + normalized;
        var name = GetKnownName(normalized) ?? Path.GetFileName(normalized);
        var description = GetKnownDescription(normalized) ?? $"AuroraScript document: {normalized}";
        return new LanguageResource(id, uri, name, description, GetMimeType(normalized), normalized);
    }

    private static string CreateDocumentId(string relativePath)
    {
        return relativePath
            .Replace('\\', '/')
            .Replace('/', ':')
            .Replace(".md", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".json", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".schema", "-schema", StringComparison.OrdinalIgnoreCase)
            .Replace(".ebnf", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace(".as", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private static string? GetKnownUri(string relativePath)
    {
        return relativePath switch
        {
            "docs/aurora-script-ai.md" => "aurora://docs/ai",
            "docs/script-authoring-best-practices.md" => "aurora://docs/script-best-practices",
            "docs/language-reference.md" => "aurora://docs/language",
            "docs/performance-best-practices.md" => "aurora://docs/performance",
            "docs/compiler-runtime-map.md" => "aurora://docs/compiler-runtime-map",
            "docs/host-integration.md" => "aurora://docs/host-integration",
            "schema/aurora-script.ebnf" => "aurora://schema/ebnf",
            "schema/language-features.json" => "aurora://schema/features",
            "schema/runtime-api.json" => "aurora://schema/runtime-api",
            "schema/host-api.json" => "aurora://schema/host-api",
            "examples/examples.manifest.json" => "aurora://examples/manifest",
            _ => null
        };
    }

    private static string? GetKnownName(string relativePath)
    {
        return relativePath switch
        {
            "README.md" => "AuroraScript language pack README",
            "llms.txt" => "AuroraScript LLM entrypoint",
            "docs/aurora-script-ai.md" => "AuroraScript AI reference",
            "docs/script-authoring-best-practices.md" => "AuroraScript script authoring best practices",
            "docs/language-reference.md" => "AuroraScript language reference",
            "docs/performance-best-practices.md" => "AuroraScript performance best practices",
            "docs/compiler-runtime-map.md" => "AuroraScript compiler runtime map",
            "docs/host-integration.md" => "AuroraScript host integration guide",
            "schema/aurora-script.ebnf" => "AuroraScript EBNF grammar",
            "schema/language-features.json" => "AuroraScript language features",
            "schema/runtime-api.json" => "AuroraScript runtime API",
            "schema/runtime-api.schema.json" => "AuroraScript runtime API schema",
            "schema/host-api.json" => "AuroraScript host API",
            "schema/host-api.schema.json" => "AuroraScript host API schema",
            "schema/ast.schema.json" => "AuroraScript AST schema",
            "schema/diagnostics.schema.json" => "AuroraScript diagnostics schema",
            "schema/examples.schema.json" => "AuroraScript examples schema",
            "examples/examples.manifest.json" => "AuroraScript examples manifest",
            _ => null
        };
    }

    private static string? GetKnownDescription(string relativePath)
    {
        return relativePath switch
        {
            "docs/aurora-script-ai.md" => "Primary AI reference for syntax, semantics, runtime APIs, and pitfalls.",
            "docs/script-authoring-best-practices.md" => "Recommended defaults for AI-generated AuroraScript modules, blocks, imports, data shapes, and validation.",
            "docs/language-reference.md" => "Human-readable language reference.",
            "docs/performance-best-practices.md" => "Performance guidance for compiler and runtime usage.",
            "docs/compiler-runtime-map.md" => "Source map for maintainers and AI agents.",
            "docs/host-integration.md" => "Host-side .NET API guide, including resolver, execution, CLR interop, and advanced source loading patterns.",
            "schema/aurora-script.ebnf" => "Documentation grammar summary.",
            "schema/language-features.json" => "Structured feature index.",
            "schema/runtime-api.json" => "Structured runtime API metadata.",
            "schema/host-api.json" => "Structured host-side .NET API metadata.",
            "examples/examples.manifest.json" => "Valid and invalid example metadata.",
            _ => null
        };
    }

    private static string GetMimeType(string relativePath)
    {
        var extension = Path.GetExtension(relativePath);
        return extension.ToLowerInvariant() switch
        {
            ".json" => "application/json",
            ".md" => "text/markdown",
            ".txt" => "text/plain",
            ".ebnf" => "text/plain",
            ".as" => "text/plain",
            _ => "text/plain"
        };
    }

    private static string NormalizeResourcePath(string path)
    {
        return path.TrimStart('/', '\\').Replace('\\', '/');
    }

    private sealed record LanguageResource(
        string Id,
        string Uri,
        string Name,
        string Description,
        string MimeType,
        string RelativePath)
    {
        private const string ResourcePrefix = "AuroraScript.Mcp.Documents/";

        public static string? TryGetRelativePath(string resourceName)
        {
            if (!resourceName.StartsWith(ResourcePrefix, StringComparison.Ordinal))
            {
                return null;
            }

            return NormalizeResourcePath(resourceName.Substring(ResourcePrefix.Length));
        }

        public static string[] GetDocumentRootCandidates()
        {
            return
            [
                Path.Combine(AppContext.BaseDirectory, "documents"),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "documents")),
                Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "documents")),
                Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "documents"))
            ];
        }

        public string ReadText()
        {
            using var stream = OpenEmbeddedResource(RelativePath);
            if (stream != null)
            {
                using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
                return reader.ReadToEnd();
            }

            var filePath = ResolveFilePath(RelativePath);
            if (filePath != null)
            {
                return File.ReadAllText(filePath, Encoding.UTF8);
            }

            throw new FileNotFoundException($"Could not locate MCP document resource '{RelativePath}'.");
        }

        private static Stream? OpenEmbeddedResource(string relativePath)
        {
            var assembly = Assembly.GetExecutingAssembly();
            foreach (var resourceName in GetResourceNameCandidates(relativePath))
            {
                var stream = assembly.GetManifestResourceStream(resourceName);
                if (stream != null)
                {
                    return stream;
                }
            }

            return null;
        }

        private static IEnumerable<string> GetResourceNameCandidates(string relativePath)
        {
            yield return ResourcePrefix + relativePath.Replace('\\', '/');
            yield return ResourcePrefix + relativePath.Replace('/', '\\');
        }

        private static string? ResolveFilePath(string relativePath)
        {
            foreach (var root in GetDocumentRootCandidates())
            {
                var candidate = Path.Combine(root, relativePath);
                if (File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return null;
        }
    }

    private sealed record RuntimeApiEntry(
        string Path,
        string Category,
        string Kind,
        string Returns,
        string Signature,
        string Notes,
        JsonObject Definition)
    {
        public static RuntimeApiEntry FromJson(string path, string category, JsonObject node, string? ownerName = null)
        {
            return new RuntimeApiEntry(
                path,
                category,
                node["kind"]?.GetValue<string>() ?? string.Empty,
                node["returns"]?.GetValue<string>() ?? string.Empty,
                ReadSignature(path, category, node, ownerName),
                ReadNotes(node["notes"]),
                (JsonObject)node.DeepClone());
        }

        public bool Matches(string query)
        {
            return Contains(Path, query) ||
                Contains(Category, query) ||
                Contains(Kind, query) ||
                Contains(Returns, query) ||
                Contains(Signature, query) ||
                Contains(Notes, query);
        }

        public JsonObject ToJson()
        {
            return new JsonObject
            {
                ["path"] = Path,
                ["category"] = Category,
                ["kind"] = Kind,
                ["returns"] = Returns,
                ["signature"] = Signature,
                ["notes"] = Notes,
                ["definition"] = Definition.DeepClone()
            };
        }

        private static bool Contains(string value, string query)
        {
            return value?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static string ReadSignature(string path, string category, JsonObject node, string? ownerName)
        {
            if (node["overloads"] is JsonArray overloads)
            {
                return string.Join(" ", overloads
                    .Select(item => item?.GetValue<string>())
                    .Where(text => !string.IsNullOrWhiteSpace(text)));
            }

            if (node["parameters"] is not JsonArray parameters)
            {
                return path;
            }

            var parts = new List<string>();
            foreach (var parameter in parameters)
            {
                if (parameter is not JsonObject parameterNode)
                {
                    continue;
                }

                var name = parameterNode["name"]?.GetValue<string>() ?? string.Empty;
                var type = parameterNode["type"]?.GetValue<string>() ?? "any";
                var optional = parameterNode["optional"]?.GetValue<bool>() == true;
                var variadic = parameterNode["variadic"]?.GetValue<bool>() == true;
                if (variadic)
                {
                    name = "..." + name;
                }

                parts.Add(name + (optional ? "?: " : ": ") + type);
            }

            var namePart = category == "constructor" && !string.IsNullOrWhiteSpace(ownerName)
                ? "new " + ownerName
                : path;
            return namePart + "(" + string.Join(", ", parts) + "): " + (node["returns"]?.GetValue<string>() ?? string.Empty);
        }

        private static string ReadNotes(JsonNode? notes)
        {
            if (notes is JsonArray array)
            {
                return string.Join(" ", array.Select(item => item?.GetValue<string>()).Where(text => !string.IsNullOrWhiteSpace(text)));
            }

            if (notes is JsonObject obj)
            {
                var parts = new List<string>();
                foreach (var pair in obj)
                {
                    if (pair.Value is JsonArray localized)
                    {
                        parts.AddRange(localized
                            .Select(item => item?.GetValue<string>())
                            .Where(text => !string.IsNullOrWhiteSpace(text))!);
                    }
                }

                return string.Join(" ", parts);
            }

            return string.Empty;
        }
    }
}
