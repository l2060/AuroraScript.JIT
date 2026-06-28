# AuroraScript AI Language Pack

This directory contains machine-readable and AI-readable language material for AuroraScript.
It is intended to be shipped with NuGet packages, mirrored by documentation sites, and used by AI agents.

Contents:

- `llms.txt`: LLM entry point and document index.
- `docs/aurora-script-ai.md`: compact AI reference for syntax, semantics, runtime APIs, and pitfalls.
- `docs/script-authoring-best-practices.md`: recommended defaults for AI-generated AuroraScript modules and blocks.
- `docs/language-reference.md`: human-readable language reference.
- `docs/performance-best-practices.md`: compiler and runtime performance guidance.
- `docs/host-integration.md`: .NET host API and advanced integration guide, including custom source resolver rules.
- `schema/aurora-script.ebnf`: grammar summary.
- `schema/*.schema.json`: JSON Schema files for tools.
- `schema/language-features.json`: structured feature index.
- `schema/runtime-api.json`: machine-readable script-side runtime API index.
- `schema/host-api.json`: machine-readable .NET host-side API index.
- `examples/valid`: examples that should compile and run.
- `examples/invalid`: examples that should fail compilation.
- `examples/examples.manifest.json`: expected result metadata for examples.
- `language-tools/AuroraScript.Mcp/README.md`: MCP server usage, Codex configuration, and tool examples in the repository root.

Recommended public deployment:

1. Include this directory in the AuroraScript NuGet package under `documents/`.
2. Publish `llms.txt` at the documentation site root.
3. Publish the MCP server as a .NET tool package.
4. Keep examples and tests aligned with every language semantics change.

