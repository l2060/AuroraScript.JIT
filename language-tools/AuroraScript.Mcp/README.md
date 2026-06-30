# AuroraScript MCP Server

This project provides a stdio MCP server for AuroraScript.

It exposes AuroraScript language documentation, schemas, examples, runtime API metadata, host API metadata, script validation, and script execution tools to AI coding clients that support MCP.

It intentionally keeps dependencies small and uses JSON-RPC over stdin/stdout. The NuGet tool package is RID-specific, self-contained, and single-file, so users do not need a local .NET runtime for the selected platform package.

## Install

Install from NuGet:

```bash
dotnet tool install --global AuroraScript.Mcp
```

Update:

```bash
dotnet tool update --global AuroraScript.Mcp
```

The package id is `AuroraScript.Mcp` and the tool command is `aurora-mcp`.

Smoke test:

```powershell
'{"jsonrpc":"2.0","id":1,"method":"resources/read","params":{"uri":"aurora://schema/runtime-api"}}' | aurora-mcp
```

Read the AI authoring guide:

```powershell
'{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"aurora_get_document","arguments":{"id":"script-best-practices"}}}' | aurora-mcp
```

## Codex configuration

Install the tool first:

```bash
dotnet tool install --global AuroraScript.Mcp
```

Then add the MCP server to Codex:

```bash
codex mcp add aurora-script -- aurora-mcp
```

You can also edit Codex config manually:

- Windows: `%USERPROFILE%\.codex\config.toml`
- macOS / Linux: `~/.codex/config.toml`

```toml
[mcp_servers.aurora-script]
type = "stdio"
command = "aurora-mcp"
startup_timeout_sec = 10
tool_timeout_sec = 60
enabled = true
```

If you run a locally published executable instead of the global tool, use an absolute path:

```toml
[mcp_servers.aurora-script]
type = "stdio"
command = "D:\\mcp\\AuroraScript.Mcp.exe"
cwd = "D:\\mcp"
startup_timeout_sec = 10
tool_timeout_sec = 60
enabled = true
```

Restart Codex or open a new Codex session after changing MCP configuration.

## Build

```bash
dotnet build language-tools/AuroraScript.Mcp/AuroraScript.Mcp.csproj
```

## Pack dotnet tool

```bash
dotnet pack language-tools/AuroraScript.Mcp/AuroraScript.Mcp.csproj -c Release -o artifacts/mcp-tool
```

This produces one top-level tool package and one implementation package per supported runtime identifier:

```text
AuroraScript.Mcp.3.0.0.nupkg
AuroraScript.Mcp.win-x64.3.0.0.nupkg
AuroraScript.Mcp.linux-x64.3.0.0.nupkg
AuroraScript.Mcp.osx-x64.3.0.0.nupkg
AuroraScript.Mcp.osx-arm64.3.0.0.nupkg
```

Local install smoke test:

```bash
dotnet tool install AuroraScript.Mcp --version 3.0.0 --add-source artifacts/mcp-tool --tool-path artifacts/mcp-tool-install
```

Publish to NuGet:

```bash
dotnet nuget push artifacts/mcp-tool/AuroraScript.Mcp.win-x64.3.0.0.nupkg --api-key <api-key> --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/mcp-tool/AuroraScript.Mcp.linux-x64.3.0.0.nupkg --api-key <api-key> --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/mcp-tool/AuroraScript.Mcp.osx-x64.3.0.0.nupkg --api-key <api-key> --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/mcp-tool/AuroraScript.Mcp.osx-arm64.3.0.0.nupkg --api-key <api-key> --source https://api.nuget.org/v3/index.json
dotnet nuget push artifacts/mcp-tool/AuroraScript.Mcp.3.0.0.nupkg --api-key <api-key> --source https://api.nuget.org/v3/index.json
```

Push RID packages first, then the top-level `AuroraScript.Mcp.3.0.0.nupkg` package. The NuGet API key must be allowed to push `AuroraScript.Mcp` and `AuroraScript.Mcp.*`.

Users install only the top-level package:

```bash
dotnet tool install --global AuroraScript.Mcp
```

## Publish single-file executable

Publish with a runtime identifier to create a self-contained single-file MCP server that does not require a local .NET runtime:

```bash
dotnet publish language-tools/AuroraScript.Mcp/AuroraScript.Mcp.csproj -c Release -r win-x64 --self-contained true -o artifacts/mcp-win-x64-single
```

The `documents` language pack is embedded in the executable. The loose `documents` folder is only used as a development fallback.

## Run

```bash
dotnet run --project language-tools/AuroraScript.Mcp/AuroraScript.Mcp.csproj
```

Run the published executable directly:

```bash
artifacts/mcp-win-x64-single/AuroraScript.Mcp.exe
```

PowerShell smoke test:

```powershell
$exe = ".\artifacts\mcp-win-x64-single\AuroraScript.Mcp.exe"
'{"jsonrpc":"2.0","id":1,"method":"resources/read","params":{"uri":"aurora://schema/runtime-api"}}' | & $exe
'{"jsonrpc":"2.0","id":2,"method":"tools/call","params":{"name":"aurora_get_document","arguments":{"id":"host-integration"}}}' | & $exe
```

## Tools

- `aurora_get_document`: read a language-pack document by id.
- `aurora_list_documents`: list all embedded language-pack documents, schemas, and examples.
- `aurora_list_features`: return structured language feature metadata.
- `aurora_check_script`: compile-check a module or block and return diagnostics. Supports in-memory dependency sources.
- `aurora_run_script`: compile and run a module or block, returning `result`, `stdout`, `stderr`, diagnostics, and runtime errors.
- `aurora_check_file`: compile-check a file-system entry `.as` file and its import/include graph.
- `aurora_run_file`: compile and run a file-system entry `.as` file and its import/include graph.
- `aurora_build_workspace`: compile all `.as` files visible under a file-system root.
- `aurora_search_runtime_api`: search script-side runtime APIs such as `String.trim`, `HashMap`, or `appendLine`.
- `aurora_get_runtime_api`: read one runtime API entry by path such as `Array.push`.
- `aurora_list_examples`: list valid and invalid examples from the embedded manifest.
- `aurora_get_example`: read an embedded example source file.
- `aurora_validate_best_practices`: check generated script for AI authoring warnings.
- `aurora_explain_diagnostic`: explain common AuroraScript diagnostics.

Recommended AI workflow:

1. Read `aurora://docs/script-best-practices` and `aurora://schema/runtime-api` before generating non-trivial scripts.
2. Use `aurora_search_runtime_api` or `aurora_get_runtime_api` instead of guessing API names.
3. Use `aurora_check_script` or `aurora_check_file` after generation.
4. Use `aurora_run_script` or `aurora_run_file` when a concrete result or console output should be verified.
5. Use `aurora_validate_best_practices` to catch known poor patterns such as repeatedly reading `items.length` in loop conditions.

When generating scripts that use host-defined globals, read `aurora://docs/ai` or `aurora://docs/host-integration` and use `export declare const NAME;`, `export declare var NAME;`, or `export declare func NAME(args);`. Plain `export const NAME;` and `export var NAME;` create module properties and can hide host-defined globals.

## Resolver behavior in tools

- `aurora_check_file` and `aurora_run_file` create a composite resolver with optional in-memory `sources` before the disk resolver rooted at `rootDirectory`.
- Keys in `sources` are normalized with `/` and are resolved relative to `rootDirectory`.
- The parser keeps import/include paths raw. The resolver resolves entry paths from `rootDirectory` and dependency paths from the importing file's full path.
- Earlier resolvers win. A memory overlay can override a disk dependency only when the dependency's resolved full path falls under the memory root.
- Different protocols or non-overlapping roots remain isolated; `mem://overlay/lib.as` does not override `d:/project/lib.as`.

The underlying engine also supports parent-root overlays when a host constructs the resolver manually. For example, if memory is rooted at `d:/a/b/c` and disk is rooted at `d:/a/b/c/d`, a disk script importing `../test` can resolve to `d:/a/b/c/test.as` from memory when memory appears first.

## Resources

- `aurora://docs/ai`
- `aurora://docs/script-best-practices`
- `aurora://docs/language`
- `aurora://docs/performance`
- `aurora://docs/host-integration`
- `aurora://schema/ebnf`
- `aurora://schema/features`
- `aurora://schema/runtime-api`
- `aurora://schema/host-api`
- `aurora://examples/manifest`

`aurora://schema/runtime-api` includes constructor signatures and parameters under each constructor global's `constructors` array.

## Tool Example

Read AI authoring guidance:

```powershell
'{"jsonrpc":"2.0","id":3,"method":"tools/call","params":{"name":"aurora_get_document","arguments":{"id":"script-best-practices"}}}' | & $exe
```

```json
{
  "name": "aurora_check_script",
  "arguments": {
    "mode": "module",
    "sourceName": "main.as",
    "source": "@module(TEST); import lib from './lib'; export func run() { return lib.value(); }",
    "sources": {
      "lib.as": "@module(LIB); export func value() { return 42; }"
    }
  }
}
```

Run a script and capture output:

```json
{
  "name": "aurora_run_script",
  "arguments": {
    "mode": "module",
    "source": "@module(TEST); export func run() { console.log('value', 42); return 42; }"
  }
}
```

Run a `CompileBlock` body with parameters:

```json
{
  "name": "aurora_run_script",
  "arguments": {
    "mode": "block",
    "source": "return left + right;",
    "parameters": ["left", "right"],
    "arguments": [20, 22]
  }
}
```

Run a real `.as` file from disk and let the file-system resolver load imports/includes:

```json
{
  "name": "aurora_run_file",
  "arguments": {
    "rootDirectory": "examples/tests",
    "entryPath": "main.as",
    "moduleName": "MAIN",
    "methodName": "run"
  }
}
```

Check a file with an in-memory overlay that overrides or adds sources before disk lookup:

```json
{
  "name": "aurora_check_file",
  "arguments": {
    "rootDirectory": "scripts",
    "entryPath": "main.as",
    "sources": {
      "generated/config.as": "@module(CONFIG); export const value = 42;"
    }
  }
}
```

Search runtime APIs before generating code:

```json
{
  "name": "aurora_search_runtime_api",
  "arguments": {
    "query": "StringBuffer",
    "limit": 10
  }
}
```

Validate AI authoring guidance:

```json
{
  "name": "aurora_validate_best_practices",
  "arguments": {
    "mode": "block",
    "source": "for (var i = 0; i < items.length; i++) { total += items[i]; } return total;"
  }
}
```

