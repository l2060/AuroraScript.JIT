# AuroraScript MCP Server

This project provides a stdio MCP server for AuroraScript.

It intentionally keeps dependencies small and uses JSON-RPC over stdin/stdout.

## Build

```bash
dotnet build ai-language-pack/mcp/AuroraScript.Mcp.csproj
```

## Run

```bash
dotnet run --project ai-language-pack/mcp/AuroraScript.Mcp.csproj
```

## Tools

- `aurora_get_document`: read a language-pack document by id.
- `aurora_list_features`: return structured language feature metadata.
- `aurora_check_script`: compile-check a module or block and return diagnostics.
- `aurora_explain_diagnostic`: explain common AuroraScript diagnostics.

## Resources

- `aurora://docs/ai`
- `aurora://docs/language`
- `aurora://docs/performance`
- `aurora://schema/ebnf`
- `aurora://schema/features`
- `aurora://schema/runtime-api`
- `aurora://examples/manifest`

## Tool Example

```json
{
  "name": "aurora_check_script",
  "arguments": {
    "mode": "module",
    "sourceName": "main.as",
    "source": "@module(TEST); export func run() { return 42; }"
  }
}
```

