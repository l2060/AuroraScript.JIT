<p align="center">
  <img src="icon.png" width="128" alt="AuroraScript Logo" />
</p>

<p align="center">
  <a href="./README.md">简体中文</a> | <a href="./README_EN.md">English</a>
</p>

# AuroraScript

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-2.0.0-orange.svg)](src/AuroraScript.csproj)
[![Target](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blueviolet.svg)](src/AuroraScript.csproj)

AuroraScript is a lightweight scripting engine for .NET host applications. Scripts are compiled to CIL and executed by the .NET runtime, making the engine suitable for embedded rules, business logic, configurable workflows, hot fixes, and small expressions.

AuroraScript borrows familiar syntax from JavaScript, including expressions, objects, arrays, closures, and modules. It is not an ECMAScript implementation and does not attempt to match browser or Node.js semantics. The capabilities documented here are based on the current source code and test suite.

> [!NOTE]
> The project is still under active development. Public APIs, performance behavior, and language edges may change. Pin the NuGet version and run your own regression suite before production use.

## Supported Platforms

NuGet package: `AuroraScript.JIT`

The current `2.0.0` package targets:

| Target framework | Support |
|---|---|
| `net8.0` | Supports `Dynamic` / `OnlyRun`; does not support `CompilationMode.Persistence` |
| `net9.0` | Supports `Dynamic` / `OnlyRun` / `Persistence` |
| `net10.0` | Supports `Dynamic` / `OnlyRun` / `Persistence` |

The project is built as `AnyCPU`. The runtime has no native dependency, so `Dynamic` and `OnlyRun` are expected to work on x64 and ARM64. `Persistence` produces an IL-only assembly and no longer marks generated script DLLs as x64, but ARM64 should still be validated on the target OS before being promised as a supported production platform.

## Highlights

- **CIL/JIT execution**: Scripts are compiled to .NET IL instead of being interpreted in a dispatch loop.
- **Easy embedding**: Use `AuroraEngine` to compile scripts and `ScriptDomain` to isolate and execute module functions.
- **Three compilation modes**: Choose between dynamic methods, in-memory assemblies, and persisted DLL/PDB output.
- **CLR interop**: Register CLR types and call constructors, properties, fields, instance methods, static methods, overloads, optional parameters, and `params` arguments from scripts.
- **Runtime hot patching**: Apply patches from the host with `DynamicPatch` or from scripts with `HotPatch.replace` / `HotPatch.incremental`.
- **Modules and domain isolation**: Supports `@module`, `import`, and `include`. Each `ScriptDomain` has its own global object and module instances.
- **CompileBlock**: Compile small script blocks outside the module system for formulas, filters, and high-frequency rules.
- **Built-in standard objects**: `Object`, `Array`, `String`, `Date`, `Regex`, `HashMap`, `StringBuffer`, `JSON`, `Math`, `console`, `Proxy`, and `HotPatch`.
- **Broad regression coverage**: Tests cover lexing, parsing, expressions, statements, modules, compilation modes, CLR interop, JSON, hot reload, concurrency, and release regressions.

## Installation

```bash
dotnet add package AuroraScript.JIT
```

Build from source:

```bash
git clone https://github.com/l2060/AuroraScript.git
cd AuroraScript
dotnet build src/AuroraScript.csproj -c Release
```

## Quick Start

### Host Code

```csharp
using AuroraScript;
using AuroraScript.Runtime;
using System.Text;

var options = EngineOptions.Default
    .WithBaseDirectory("./scripts")
    .WithCompilationMode(CompilationMode.Dynamic)
    .WithOptimizeOption(OptimizeOptions.Release);

var engine = new AuroraEngine(options);
engine.RegisterType(typeof(Math), "Math2");

await engine.BuildAsync(engine.SearchAllFileSource(Encoding.UTF8));

var domain = engine.CreateDomain();
var result = domain.Execute("MAIN", "main", ScriptDatum.FromNumber(20));
Console.WriteLine(result);
```

### Script Code

```javascript
@module(MAIN);

export func main(value) {
    var total = Math2.Abs(-value);
    var items = [1, 2, 3];

    for (var item in items) {
        total = total + item;
    }

    return total;
}
```

### User State

```csharp
using AuroraScript.Runtime;
using AuroraScript.Runtime.Types;

public sealed class MyState : ScriptObject
{
    public MyState()
    {
        Define("Name", StringValue.Of("Aurora"));
        Define("Count", NumberValue.Of(3));
    }
}

var userState = new MyState();
var domain = engine.CreateDomain(userState: userState);
```

`userState` must inherit from `ScriptObject`. Scripts can access it through `$state`:

```javascript
@module(MAIN);

export func name() {
    return $state.Name;
}
```

### Global Values and Functions

```csharp
var domain = engine.CreateDomain(global =>
{
    global.Define("HOST_ADD", (Func<int, int, int>)((a, b) => a + b));
    global.Define("HOST_NAME", "Aurora");
});
```

```javascript
@module(MAIN);

export declare func HOST_ADD(left, right);

export func run() {
    return HOST_NAME + ":" + HOST_ADD(20, 22);
}
```

## CompileBlock

`CompileBlock` treats source text as an anonymous function body. It does not create a module and does not participate in hot reload. It is intended for formulas, rules, filters, and small snippets that are invoked frequently.

```csharp
var engine = new AuroraEngine(EngineOptions.Default.WithBaseDirectory("."));

var block = engine.CompileBlock("""
func clamp(v, min, max) {
    if (v < min) return min;
    if (v > max) return max;
    return v;
}

return clamp(x, 0, 100);
""", new CompileBlockOptions
{
    Parameters = ["x"],
    SourceName = "rules/clamp.as"
});

var result = block.Invoke(ScriptDatum.FromNumber(125));
```

Parameter names are declared through `CompileBlockOptions.Parameters`; runtime values are passed positionally. Parameter names cannot be empty, duplicated, or equal to `global`, `$args`, or `$state`.

## Compilation Modes

| Mode | Description | Typical use |
|---|---|---|
| `Dynamic` | Emits CIL through `DynamicMethod` and does not produce a persisted assembly | Fast runtime execution, rules, small scripts |
| `OnlyRun` | Runs through a collectible dynamic assembly in memory | In-memory execution that is easier for runtime tools to observe |
| `Persistence` | Generates a DLL and can include debug symbols | Debugging, diagnostics, persisted script assemblies |

Limitations:

- `Persistence` requires `net9.0` or later. On `net8.0`, using it throws `PlatformNotSupportedException`.
- Visual Studio source-level script debugging depends on `Persistence` mode and debug optimization settings.

## Hot Patching

Hot patching is controlled by `EngineOptions.EnableHotReload` and is enabled by default. Disabling it blocks host-side `DynamicPatch` and script-side `HotPatch`, and allows the compiler to use more aggressive module-local direct-call optimizations.

Host side:

```csharp
domain.DynamicPatch(
    engine.MemorySource("main.as", "@module(MAIN); export func run() { return 42; }"),
    HotPatchType.Replace);
```

Script side:

```javascript
HotPatch.replace("main.as", "@module(MAIN); export func run() { return 42; }");
HotPatch.incremental("main.as", "export func added() { return 1; }");
```

Notes:

- `Replace` clears existing members of the target module before applying new code.
- `Incremental` preserves existing members and adds or updates members declared in the patch.
- Top-level code in the patch source is executed when the patch is applied.

## Language Features

The current test suite and examples cover:

- `var` / `const` / `func` / `function` / `return`
- `if` / `else` / `for` / `for-in` / `while` / `break` / `continue`
- `try` / `catch` / `finally` / `throw`
- `enum`
- closures, recursion, default parameters, `$args`
- expression lambdas: `(a, b) => a + b`
- block lambdas: `(value) => { return value * 2; }`
- object literals, array literals, sparse arrays
- object and array destructuring
- object shorthand and object/array spread
- template strings and nested templates
- regex literals and regex/division disambiguation
- `typeof`, `in`, `delete`, increment/decrement, compound assignment

## Built-in API

### Global Objects

- `Array`
- `String`
- `Boolean`
- `Object`
- `Number`
- `Date`
- `Error`
- `HashMap`
- `Regex`
- `Proxy`
- `StringBuffer`
- `console`
- `JSON`
- `Math`
- `HotPatch`
- `global`
- `$state`
- `$args`

### Object

Static members:

- `Object.equal$(a, b)`
- `Object.equal(a, b)`
- `Object.deepEqual(a, b)`
- `Object.assign(target, ...sources)`
- `Object.keys(obj)`
- `Object.clone(obj)`
- `Object.deepClone(obj)`
- `Object.freeze(obj)`

Instance members:

- `obj.length`
- `obj.toString()`

> `Object.extends` is registered in the constructor, but the current implementation does not return an effective value, so it is not documented as a usable API.

### Array

Static members:

- `Array.from(iterable, [mapCallback])`
- `Array.isArray(value)`
- `Array.of(...items)`

Instance members:

- `length`
- `has(value)`
- `indexOf(value)`
- `lastIndexOf(value)`
- `push(...items)`
- `pop()`
- `sort([compare])`
- `join([separator])`
- `slice(start, [end])`
- `reverse()`
- `unshift(...items)`
- `shift()`
- `concat(...items)`
- `find(callback)`
- `findIndex(callback)`
- `findLast(callback)`
- `findLastIndex(callback)`
- `map(callback)`
- `filter(callback)`
- `some(callback)`
- `every(callback)`
- `flat([depth])`
- `reduce(callback)`

### String

Static members:

- `String.fromCharCode(code)`
- `String.valueOf(value)`
- `String.compare(a, b)`

Instance members:

- `length`
- `contains(text)`
- `indexOf(text)`
- `lastIndexOf(text)`
- `startsWith(text)`
- `endsWith(text)`
- `substring(start, [end])`
- `slice(start, [end])`
- `split(separator)`
- `match(regex)`
- `matchAll(regex)`
- `replace(search, replacement)`
- `padLeft(width, [char])`
- `padRight(width, [char])`
- `trim()`
- `trimLeft()`
- `trimRight()`
- `toString()`
- `charCodeAt(index)`
- `toLowerCase()`
- `toUpperCase()`

### Date

Static members:

- `Date.now()`
- `Date.utcNow()`
- `Date.parse(value)`

Instance members:

- `year`
- `month`
- `day`
- `hour`
- `minute`
- `second`
- `millisecond`
- `dayOfWeek`
- `dayOfYear`
- `ticks`
- `toString([format])`

### HashMap

- `set(key, value)`
- `get(key)`
- `getOrInsert(key, valueOrCallback)`
- `has(key)`
- `delete(key)`
- `clear()`
- `keys`
- `values`
- `size`

### Regex

- `test(text)`

Strings also provide `match(regex)` and `matchAll(regex)`.

### StringBuffer

- `append(...items)`
- `appendLine(...items)`
- `insert(index, value)`
- `clear()`
- `toString()`
- `release()`
- `stringAndRelease()`

### JSON

- `JSON.parse(text)`
- `JSON.stringify(value, [indented])`

JSON supports script primitives, objects, arrays, HashMap, and related script values. Circular references throw a runtime exception.

### Math

Commonly used members covered by the runtime and tests include:

- `Math.PI`
- `Math.E`
- `Math.abs(x)`
- `Math.max(...values)`
- `Math.min(...values)`
- `Math.random()`
- `Math.floor(x)`
- `Math.round(x)`
- `Math.pow(x, y)`
- `Math.log(x)`
- `Math.exp(x)`
- `Math.sin(x)`
- `Math.cos(x)`
- `Math.tan(x)`

### console

- `console.log(...args)`
- `console.error(...args)`
- `console.time(label)`
- `console.timeEnd(label)`

Output writers can be configured with `EngineOptions.WithConsoleStdOut` and `WithConsoleErrorOut`.

### Proxy

```javascript
var proxy = new Proxy(target, {
    get: (obj, key) => obj[key],
    set: (obj, key, value) => { obj[key] = value; return value; }
});
```

`Proxy` construction requires `get` and `set` handlers. Tests cover property read, write, and delete-related behavior.

## CLR Interop

Register a CLR type:

```csharp
engine.RegisterType<HostCalculator>("Calculator");
```

Use it from script:

```javascript
@module(MAIN);

export func run() {
    var host = new Calculator(5);
    host.Value = 7;
    host.Field = 3;
    return [host.Add(2), Calculator.Multiply(3, 4), host.Value, host.Field];
}
```

Tested capabilities:

- constructors
- property and field get/set
- instance and static methods
- overload resolution
- optional parameters
- `params` arguments
- global CLR values, collections, and delegates
- type access restrictions: `TypeAccess.All`, `Constructor`, `Static`
- duplicate aliases and registry lifetime errors

## Tests

Test project: `tests/AuroraScript.Tests`

Current `net10.0` test discovery finds **282** test cases. Breakdown by test class:

| Test class | Cases | Focus |
|---|---:|---|
| `LexerTests` | 37 | Lexing, numbers/strings/regex, comments, malformed tokens |
| `ParserSyntaxTests` | 79 | Grammar branches, modules, import/include/export, syntax diagnostics |
| `ExpressionExecutionTests` | 35 | Expressions, operators, member/index access, spread, assignment |
| `StatementExecutionTests` | 7 | Control flow, loops, closures, recursion, exceptions, domain isolation |
| `LanguageFeatureExecutionTests` | 16 | enum, lambdas, sparse arrays, truthiness, templates, assignment semantics |
| `ModuleCompilationTests` | 12 | Dependencies, parallel compile, cycles, error aggregation, cancellation |
| `CompileBlockTests` | 21 | CompileBlock parameters, invocation modes, invalid inputs, diagnostics |
| `CompilationModeTests` | 5 | Dynamic/OnlyRun/Persistence behavior and hot-reload settings |
| `RuntimeApiAndErrorTests` | 9 | Runtime APIs, error paths, `$state`, disposed domains |
| `BuiltInLibraryTests` | 8 | Math, String, Array, JSON, HashMap, Regex, StringBuffer, Console |
| `AdvancedRuntimeTypeTests` | 6 | Constructors, Object, freeze, Date, Proxy |
| `ClrInteropTests` | 5 | CLR constructors/properties/fields/methods/overloads/access restrictions |
| `SerializationTests` | 9 | JSON serialization/deserialization, circular references, malformed JSON |
| `ScriptDatumTests` | 4 | Datum payloads, equality, CLR collection conversion, Span helpers |
| `HotReloadTests` | 4 | Disabled hot reload, incremental patch, replacement patch, domain isolation |
| `ConcurrentRuntimeTests` | 3 | Same-domain/multi-domain concurrency, detached closure concurrency |
| `ReleaseRegressionTests` | 9 | Release direct calls, closure slots, stack balance, confusion, empty modules |
| `ClosureFunctionContextTests` | 3 | Context pool lifetime and detached invocation |
| `EngineOptionsAndSourceTests` | 10 | EngineOptions, source paths, extensions, parallelism, null input |

Coverage summary:

- Lexer: keywords, identifiers, Unicode, operators, numbers, strings, regex, comments, CRLF locations, malformed tokens.
- Parser: module metadata, import/include/export, declarations, expressions, lambdas, destructuring, control flow, exceptions, templates, regex, diagnostics.
- CompileBlock: parameter validation, local functions, domain/no-domain invocation, module-only rejection, source names, null input.
- Expressions/statements: precedence, arithmetic, bitwise operations, comparison, logical operators, member access, spread, assignment, loops, exceptions, closures, recursion.
- Module compilation: relative paths, diamond dependencies, duplicate roots, wide dependency graphs, cycles, error aggregation, cancellation, concurrent builds.
- Compilation modes: Dynamic, OnlyRun, Persistence parity; net8 Persistence limitation.
- Runtime APIs and errors: pre-build use, missing modules/methods, script stack traces, const writes, `$state`, disposal.
- Built-ins: Math, String, Array, JSON, HashMap, Regex, StringBuffer, Console, Date, Proxy.
- CLR interop, serialization, ScriptDatum, hot reload, concurrency, and release regressions.

Run tests:

```bash
dotnet test tests/AuroraScript.Tests/AuroraScript.Tests.csproj
```

The test project targets `net8.0;net9.0;net10.0`. Running each target requires the matching .NET runtime on the machine.

## Benchmark

The unified benchmark project lives in `benchmark/` and includes runtime metrics plus compiler pipeline metrics.

Smoke run:

```bash
dotnet run --project benchmark/Benchmark.csproj -c Release -- --smoke
```

Simple CSV-like comparison:

```bash
dotnet run --project benchmark/Benchmark.csproj -c Release -- --compare
```

BenchmarkDotNet run:

```bash
dotnet run --project benchmark/Benchmark.csproj -c Release
```

Current key metrics include:

- domain creation, empty calls, function calls, module calls, closure calls
- object, array, HashMap, string, JSON, Regex, and CLR interop paths
- Lexer, Parser, Emitter, single/multi-module compile, and CompileBlock

The latest summarized results come from the `RuntimeBenchmarks` and `CompilerPipelineBenchmarks` reports generated on 2026-06-22 under `benchmark/bin/Release/net10.0/BenchmarkDotNet.Artifacts/results/`. The older `ScriptBenchmark` report in the same directory is historical and is no longer used as the current benchmark reference.

Environment:

- BenchmarkDotNet `0.15.8`
- Windows 11 `10.0.26200.8655`
- Intel Core i7-13700KF
- .NET SDK `10.0.301`
- Runtime `.NET 10.0.9`
- Job `ShortRun`

Runtime summary:

| Metric | Scale | Mean | Allocated | Notes |
|---|---:|---:|---:|---|
| `EmptyCall` | 1 call | 152 ns | 0 B | Low host-to-script empty call overhead |
| `CreateDomain` | 1 domain | 2.8 us | 5.32 KB | Lightweight domain creation |
| `NumericLoop` | 10,000 | 78.0 us | 48 B | Numeric loops are effectively allocation-free |
| `FunctionCallLoop` | 10,000 | 1.10 ms | 48 B | Local function calls are almost allocation-free |
| `ModuleCallLoop` | 10,000 | 1.32 ms | 48 B | Module calls are roughly 20% slower than local calls |
| `ClosureInvoke` | 10,000 | 660 us | 208 B | Stable low allocation |
| `ObjectCreateSetGet` | 10,000 | 1.97 ms | 1.91 MB | Object creation/property writes allocate linearly |
| `ArrayPushIndex` | 10,000 | 930 us | 768 KB | Gen2 appears; array growth path needs attention |
| `HashMapSetGet` | 10,000 | 7.10 ms | 4.51 MB | High allocation and Gen2 activity |
| `JsonStringify` | 10,000 | 7.11 ms | 7.55 MB | JSON serialization allocates heavily |
| `JsonParse` | 10,000 | 10.76 ms | 8.54 MB | JSON parsing allocates heavily |
| `RegexMatchAll` | 10,000 | 30.67 ms | 32.58 MB | One of the heaviest regular runtime paths |
| `StringBufferAppend` | 10,000 | 1.33 ms | 408 KB | Much better than direct string concatenation |
| `StringConcat` | 10,000 | 55.71 ms | 457.79 MB | Very expensive; use `StringBuffer` for this pattern |
| `ClrPropertyGetSet` | 10,000 | 592 us | 234 KB | CLR property access is relatively healthy |
| `ClrArrayArgument` | 10,000 | 6.89 ms | 4.04 MB | Script-array to CLR-array conversion still allocates heavily |
| `ClrInstanceMethod` | 10,000 | 8.31 ms | 2.90 MB | Instance method interop still has optimization headroom |
| `ClrStaticMethod` | 10,000 | 11.59 ms | 4.04 MB | Static method binding/argument conversion is a clear hotspot |

Compiler pipeline summary:

| Metric | Mean | Allocated | Notes |
|---|---:|---:|---|
| `CompileBlock` | 28.8 us | 17.85 KB | Low compile cost for small script blocks |
| `FullCompile_MultiModule` | 358 us | 64.5 KB | Current multi-module sample is small and healthy |
| `FullCompile_SingleModule` | 7.25 ms | 2.78 MB | Main cost for large-module full compilation |
| `EmitOnly_ParsedLargeModule` | 4.31 ms | 1.24 MB | Emitter is a major large-module hotspot |
| `LexerOnly_Large` | 579 us | 21.26 KB | Large-source lexing allocates little |
| `ParseOnly_Large` | 845 us | 1.53 MB | AST construction is the main parser allocation cost |
| `ParseOnly_TemplateInterpolation` | 166 us | 412.59 KB | Template interpolation parsing allocates relatively heavily |

Observed hotspots:

- `StringConcat` allocates about `457.79 MB` for 10,000 iterations. This is expected for repeated immutable-string concatenation, but it is far too expensive for hot paths; use `StringBuffer`.
- `RegexMatchAll` allocates about `32.58 MB` per 10,000 iterations. Match result arrays and capture/group objects are good future optimization targets.
- CLR interop paths `ClrStaticMethod` and `ClrArrayArgument` allocate about `4.04 MB/10,000`; static binding and script-array to CLR-array conversion remain hotspots.
- `HashMapSetGet` triggers Gen2 at 10,000 iterations, likely due to string key creation and dictionary growth.
- `ArrayPushIndex` also shows Gen2 at 10,000 iterations; array growth strategy and benchmark capacity setup should be reviewed.
- `ParseOnly_TemplateInterpolation` allocates heavily relative to source size and is worth a focused parser optimization pass.

## Examples

- [examples/tests/main.as](examples/tests/main.as): module loading and entry-point sample.
- [examples/tests/unit.as](examples/tests/unit.as): built-in types, standard library, and language feature samples.
- [tests/AuroraScript.Tests](tests/AuroraScript.Tests): recommended behavioral specification.
- [benchmark/scripts/runtime.as](benchmark/scripts/runtime.as): runtime benchmark script.

## VS Code Extension

The `vscode-extension` directory contains the VS Code extension project. It currently focuses on syntax highlighting and basic editing support.

```bash
npm install
npm run package
```

Then install the generated `.vsix`.

---

Made by [l2060](https://github.com/l2060)
