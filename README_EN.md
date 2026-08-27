<p align="center">
  <img src="icon.png" width="128" alt="AuroraScript Logo" />
</p>

<p align="center">
  <a href="./README.md">简体中文</a> | <a href="./README_EN.md">English</a>
</p>

# AuroraScript

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-4.0.0-orange.svg)](src/AuroraScript.csproj)
[![Target](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blueviolet.svg)](src/AuroraScript.csproj)

AuroraScript is a lightweight scripting engine for .NET hosts. It compiles scripts to CIL for CLR/JIT execution, making it suitable for embedded rules, business logic, configurable workflows, hot patches, and small expressions.

Its syntax borrows familiar JavaScript-style expressions, objects, arrays, closures, and modules. It is not an ECMAScript implementation and does not promise browser or Node.js compatibility.

## Highlights

- CIL/JIT execution, modules, and isolated `ScriptDomain` instances.
- `Dynamic`, `OnlyRun`, and `Persistence` compilation modes.
- Controlled CLR type registration and interop.
- Memory, file-system, and composite script sources.
- Hot patching, `CompileBlock`, standard objects, and language tooling.

## Install

```bash
dotnet add package AuroraScript.JIT --version 4.0.0
```

The package targets `net8.0`, `net9.0`, and `net10.0`. `Dynamic` and `OnlyRun` work on all targets; `Persistence` requires `net9.0` or later.

## Quick Start

Create `scripts/main.as`:

```javascript
@module(MAIN);

export func main(value) {
    return value + 22;
}
```

Build and execute it from the host:

```csharp
using AuroraScript;
using AuroraScript.Core;
using AuroraScript.Runtime;

var options = EngineOptions.Default.WithCompiler(compiler =>
{
    compiler.SourceResolver = ScriptSources.FileSystem("./scripts");
    compiler.Mode = CompilationMode.Dynamic;
});

var engine = new AuroraEngine(options);
await engine.BuildAsync("main.as");

using var domain = engine.CreateDomain();
var result = domain.Execute("MAIN", "main", ScriptDatum.FromNumber(20));
Console.WriteLine(result); // 42
```

`@module(MAIN);` gives the entry an explicit lookup name. The host can address it through `Execute("MAIN", ...)`, `GetMethod`, or `GetModule`, and scripts can query it dynamically through `global.getModule("MAIN")`. Files used only through script `import` / `include` may omit `@module`; those modules remain anonymous, and no default name is derived from the filename.

## Documentation

The detailed overview, tutorials, per-object API Reference, benchmark data, MCP, language server, and Visual Studio extension documentation live in the Wiki:

- [Wiki home](https://github.com/l2060/AuroraScript.JIT/wiki)
- [Getting Started](https://github.com/l2060/AuroraScript.JIT/wiki/Getting-Started)
- [Language Guide](https://github.com/l2060/AuroraScript.JIT/wiki/Language-Guide)
- [Script API Reference](https://github.com/l2060/AuroraScript.JIT/wiki/API-Script-Reference)
- [.NET Host API Reference](https://github.com/l2060/AuroraScript.JIT/wiki/API-Host-Reference)
- [Native host exports](https://github.com/l2060/AuroraScript.JIT/wiki/Host-Native-Exports)
- [MCP, LSP, and VSIX](https://github.com/l2060/AuroraScript.JIT/wiki/Tooling)
- [Performance and Benchmarks](https://github.com/l2060/AuroraScript.JIT/wiki/Performance-and-Benchmarks)

The Wiki is Chinese-first: each English translation follows its Chinese source in a quote block. API headings (`Parameters:`, `Returns`) and parameter names remain English for copyability and searchability.

## Build from Source

```bash
git clone https://github.com/l2060/AuroraScript.JIT.git
cd AuroraScript.JIT
dotnet build src/AuroraScript.csproj -c Release
```

## License

Released under the [MIT License](LICENSE).
