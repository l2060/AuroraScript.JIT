# AuroraScript AI Language Pack

This directory contains machine-readable and AI-readable language material for AuroraScript.
It is intended to be shipped with NuGet packages, mirrored by documentation sites, and used by AI agents.

Contents:

- `llms.txt`: LLM entry point and document index.
- `docs/aurora-script-ai.md`: compact AI reference for syntax, semantics, runtime APIs, and pitfalls.
- `docs/language-reference.md`: human-readable language reference.
- `docs/performance-best-practices.md`: compiler and runtime performance guidance.
- `schema/aurora-script.ebnf`: grammar summary.
- `schema/*.schema.json`: JSON Schema files for tools.
- `schema/language-features.json`: structured feature index.
- `examples/valid`: examples that should compile and run.
- `examples/invalid`: examples that should fail compilation.
- `examples/examples.manifest.json`: expected result metadata for examples.
- `mcp`: AuroraScript MCP server project.

Recommended public deployment:

1. Include this directory in the AuroraScript NuGet package under `contentFiles/any/any/ai-language-pack`.
2. Publish `llms.txt` at the documentation site root.
3. Publish the MCP server as a .NET tool package.
4. Keep examples and tests aligned with every language semantics change.

