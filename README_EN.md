<p align="center">
  <img src="icon.png" width="128" alt="AuroraScript Logo" />
</p>

<p align="center">
  <a href="./README.md">简体中文</a> | <a href="./README_EN.md">English</a>
</p>

# AuroraScript

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Version](https://img.shields.io/badge/version-3.0.1-orange.svg)](src/AuroraScript.csproj)
[![Target](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-blueviolet.svg)](src/AuroraScript.csproj)

AuroraScript is a lightweight scripting engine for .NET host applications. Scripts are compiled to CIL and executed by the .NET runtime, making the engine suitable for embedded rules, business logic, configurable workflows, hot fixes, and small expressions.

AuroraScript borrows familiar syntax from JavaScript, including expressions, objects, arrays, closures, and modules. It is not an ECMAScript implementation and does not attempt to match browser or Node.js semantics. The capabilities documented here are based on the current source code and test suite.

> [!NOTE]
> The project is still under active development. Public APIs, performance behavior, and language edges may change. Pin the NuGet version and run your own regression suite before production use.

## Supported Platforms

NuGet package: `AuroraScript.JIT`

The current `3.0.1` package targets:

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
- **Explicit performance annotations**: Mark hot module functions with `@directCall` so eligible call sites can use a more direct execution path.
- **Modules and domain isolation**: Supports `@module`, `import`, and `include`. Each `ScriptDomain` has its own global object and module instances.
- **CompileBlock**: Compile small script blocks outside the module system for formulas, filters, and high-frequency rules.
- **Built-in standard objects**: `Object`, `Array`, `Int32Array`, `Int8Array`, `BooleanArray`, `String`, `Date`, `Regex`, `HashMap`, `StringBuffer`, `Path`, `JSON`, `Math`, `console`, `Proxy`, and `HotPatch`.
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
using AuroraScript.Core;
using AuroraScript.Runtime;

var options = EngineOptions.Default
    .WithCompiler(compiler =>
    {
        compiler.SourceResolver = ScriptSources.FileSystem("./scripts");
        compiler.Mode = CompilationMode.Dynamic;
    })
    .WithOptimization(optimization => optimization.Level = OptimizeOptions.Release);

var engine = new AuroraEngine(options);
engine.RegisterType(typeof(Math), "Math2");

await engine.BuildAsync();

var domain = engine.CreateDomain();
var result = domain.Execute("MAIN", "main", ScriptDatum.FromNumber(20));
Console.WriteLine(result);
```

> 3.0 change: script input is no longer configured through `compiler.Directory`, `BaseDirectory`, or `SearchAllFileSource`. Configure `compiler.SourceResolver` and build with `BuildAsync()`, `BuildAsync("main.as")`, or `BuildAsync(params ScriptSource[])`.

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

When the value is already in runtime representation, call `Define(string, ScriptDatum, ...)` directly to avoid boxing it as a `ScriptObject` and converting it back to `ScriptDatum`.

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
    global.Define("HOST_COUNT", ScriptDatum.FromNumber(3));
});
```

Host-injected `global` members do not require an `@global()` declaration. Without one, scripts can still read those values from the domain `global` object at runtime. `@global()` is an optional compile-time contract for host APIs, giving the editor semantic coloring, go-to-definition, and better static diagnostics. It behaves like a TypeScript `.d.ts`: it provides a symbol contract and emits no runtime code.

`globals.as`:

```javascript
@global();

declare func HOST_ADD(left, right);
declare const HOST_NAME;
declare var HOST_COUNT;
```

Normal modules can use those global symbols directly:

```javascript
@module(MAIN);

export func run() {
    return HOST_NAME + ":" + HOST_ADD(20, 22);
}
```

`@global()` file rules:

- `@global();` must be the first effective statement; only blank lines and comments may appear before it.
- `@global()` cannot appear together with `@module`.
- `@global()` files may contain only `declare` statements; `export declare` is invalid.
- `declare` is not allowed in `@module` files.
- `@global()` files cannot be imported or included and are not compiled as modules.
- A project may contain multiple `@global()` files, but global names must not be duplicated; functions do not support overloads.

## Script Source Resolver

Starting with 3.0, script loading is extended through `IScriptSourceResolver`. A resolver has three responsibilities: relocate raw import/include paths into stable references, read a source by reference, and enumerate all visible sources. Whether the source comes from the file system, memory, a database, object storage, or a virtual directory is entirely resolver-defined.

Common factories live in `AuroraScript.Core.ScriptSources`:

```csharp
using AuroraScript.Core;

var options = EngineOptions.Default.WithCompiler(compiler =>
{
    compiler.SourceResolver = ScriptSources.FileSystem("./scripts");
});
```

`BuildAsync()` enumerates all scripts exposed by the current resolver through `GetAllSourcesAsync`; `BuildAsync("main.as")` resolves the entry first and then lets module graph building follow its `import` / `include` dependencies:

```csharp
await engine.BuildAsync();          // compile every source exposed by the resolver
await engine.BuildAsync("main.as"); // compile from one entry and its dependencies
```

In-memory sources are useful for tests, generated scripts, and plugin scripts:

```csharp
var memory = ScriptSources.Memory("mem://app/")
    .Add("main.as", """
        @module(MAIN);
        import lib from './lib';
        export func run() { return lib.value; }
        """)
    .Add("lib.as", """
        @module(LIB);
        export const value = 42;
        """);

var engine = new AuroraEngine(
    EngineOptions.Default.WithCompiler(compiler => compiler.SourceResolver = memory));

await engine.BuildAsync("main.as");
```

Use `Composite` when you want memory sources on top of file-system sources. Earlier resolvers have higher priority; when the resolved import/include target falls under an earlier resolver root, that resolver can override it. Different protocols or non-overlapping roots remain isolated script namespaces. Prefer `/` in script paths:

```csharp
var scriptRoot = Path.GetFullPath("./scripts");

var resolver = ScriptSources.Composite(
    ScriptSources.Memory(scriptRoot)
        .Add("generated.as", "@module(GEN); export const value = 42;"),
    ScriptSources.FileSystem(scriptRoot));

var options = EngineOptions.Default.WithCompiler(compiler =>
{
    compiler.SourceResolver = resolver;
});
```

If `Memory("d:/a/b/c")` is placed before `FileSystem("d:/a/b/c/d")`, a file-system script importing `../test` first resolves to `d:/a/b/c/test.as`, so the parent memory root can override it. Custom stores only need to implement `IScriptSourceResolver`. `ResolveAsync` should preserve importer context so `import lib from './lib';` and `include '../shared';` resolve relative to the importer itself, not to a global compiler directory; then check whether the resolved target belongs to the current resolver root.

Resolver rules:

- The parser keeps the raw path written in the script; module graph building calls `ResolveAsync`.
- `ResolveAsync(null, entryPath, ...)` resolves an entry from the resolver `Root`; dependency resolution starts from the directory of `importer.FullPath`.
- `Composite` resolves in resolver order. The first resolver that returns a reference wins; no extra `ProviderId` is needed.
- `MemorySourceResolver` only resolves sources that fall under its root and have been added to the memory table, so it can override later resolvers only when the resolved target is under the memory root.
- `FileSystemScriptSourceResolver` enumerates files under its root for `BuildAsync()`. Dependency resolution follows the importer path and may resolve outside the root when the file exists; the returned reference still routes back to that file-system resolver.
- `GetSourceAsync` routes by exact `ScriptSourceReference.BaseDirectory`, not by searching target paths again.
- `BuildAsync()` enumerates through `GetAllSourcesAsync`; `Composite` de-duplicates by normalized `FullPath`, so earlier sources hide later sources with the same identity.
- Before module analysis, the compiler scans resolver-visible project `.as` files; when `@global()` declaration files exist, their optional declarations are loaded. They do not depend on `import` / `include`.
- Resolver implementations should normalize roots/source keys when they are built or added, use `/` internally, and avoid repeated normalization in hot comparisons.

## CompileBlock

`CompileBlock` treats source text as an anonymous function body. It does not create a module and does not participate in hot reload. It is intended for formulas, rules, filters, and small snippets that are invoked frequently.

`CompileBlock` accepts statement bodies only. It does not support `@module`, `@global()`, `import`, `include`, `export`, or `declare`.

```csharp
var engine = new AuroraEngine(EngineOptions.Default);

var block = engine.CompileBlock("""
func clamp(v, min, max) {
    if (v < min) return min;
    if (v > max) return max;
    return v;
}

return clamp(x, 0, 100);
""", ["x"]);

var result = block.Invoke(ScriptDatum.FromNumber(125));
```

Parameter names can be declared directly with `CompileBlock(source, string[] parameters)`; runtime values are passed positionally. Use `CompileBlockOptions.SourceName` when diagnostics need a custom source name. Parameter names cannot be empty, duplicated, or equal to `global`, `$args`, or `$state`.

After a `CompiledBlock` becomes unreachable, its GC finalizer automatically releases dynamic delegates registered during compilation. Call `Dispose()` only when deterministic release is needed.

## Compilation Modes

| Mode | Description | Typical use |
|---|---|---|
| `Dynamic` | Emits CIL through `DynamicMethod` and does not produce a persisted assembly | Fast runtime execution, rules, small scripts |
| `OnlyRun` | Runs through a collectible dynamic assembly in memory | In-memory execution that is easier for runtime tools to observe |
| `Persistence` | Generates a DLL and can include debug symbols | Debugging, diagnostics, persisted script assemblies |

Limitations:

- `Persistence` requires `net9.0` or later. On `net8.0`, using it throws `PlatformNotSupportedException`.
- When `Output.AssemblyFile` is empty, `Persistence` does not write a DLL, but it still serializes an in-memory PE and loads it through `Assembly.Load(byte[])` so Debug embedded PDB/sequence point semantics are preserved.
- Visual Studio source-level script debugging depends on `Persistence` mode and debug optimization settings. `OnlyRun` is a collectible dynamic-assembly mode and does not provide equivalent source-level debug symbols.

## Hot Patching

Hot patching is controlled by `EngineOptions.Runtime.EnableHotReload` and is enabled by default. Disabling it blocks host-side `DynamicPatch` and script-side `HotPatch`.

Host side:

```csharp
var mainPath = Path.GetFullPath(Path.Combine(scriptRoot, "main.as"));
domain.ReplacePatch(mainPath, "@module(MAIN); export func run() { return 42; }");
domain.IncrementalPatch(mainPath, "export func added() { return 1; }");
```

Host-side string overloads require an absolute file path or virtual full path. The path must fall under one of the current `SourceResolver` roots; `Composite` chooses the longest matching root. Imports/includes inside the patch then continue resolving in the same resolver namespace instead of being assigned to an unrelated root.

Script side:

```javascript
HotPatch.replace("@module(MAIN); export func run() { return 42; }");
HotPatch.incremental("./main", "export func added() { return 1; }");
```

On the script side, passing only `script` patches the current module. When a relative `modulePath` is supplied, it resolves from the current module `FullPath` directory.

Notes:

- `Replace` clears existing members of the target module before applying new code.
- `Incremental` preserves existing members and adds or updates members declared in the patch.
- The host or script author chooses between `Replace` and `Incremental` based on the patch scope. When a patch changes a group of functions that call each other, `Replace` is usually a better fit for keeping the module contents consistent.
- Top-level code in the patch source is executed when the patch is applied.

## Module and Function Annotations

Declare a module name with `@module(NAME);`. `@module` must be the first effective statement in a module; only whitespace or comments may appear before it.

An `@module` file is a compilable module and may use top-level `import` / `include` / `export`. An `@global()` file is a global declaration file used only for compile-time symbol assistance; it cannot be mixed with `@module` and cannot be imported or included.

```javascript
// module metadata must be first
@module(MAIN);

export func run() {
    return 42;
}
```

Function annotations are written before function declarations. The currently supported function annotation is `@directCall`.

```javascript
@module(MAIN);

@directCall
func addOne(value) {
    return value + 1;
}

export func run(value) {
    return addOne(value);
}
```

`@directCall` and `@directCall()` are equivalent. They explicitly enable module-local direct-call optimization for the function while preserving the function object on the module, so the function can still be exported, read, or invoked from the host.

```javascript
@module(MAIN);

@directCall()
export func checksum(value) {
    return value + 100;
}
```

When automatic module direct-call inference is enabled, use `@directCall(false)` to disable the optimization for one function:

```javascript
@module(MAIN);

@directCall(false)
func normalCall(value) {
    return value;
}
```

Hosts can enable automatic inference with `EngineOptions.WithOptimization(optimization => optimization.EnableAutoModuleDirectCall = true)`. Explicit `@directCall` does not depend on that option; marked functions are still considered for direct calls when automatic inference is disabled.

Usage guidance:

- `@directCall` is intended for stable, frequently called module-level functions with fixed parameters.
- Functions that use default parameters, `$args`, captured outer variables, or other dynamic call shapes continue to use the normal function-call path.
- Do not reassign the name of a function marked with `@directCall` inside the module.

### directCall Call Path Optimization

`@directCall` optimizes the path for calling a known module function. It is not function inlining. The target remains a separate CIL method, while return values and exception-stack behavior stay observable. Calls reuse the active `ScriptContext` with a lightweight frame instead of allocating a child context; a proven numeric-only call graph can go further and call methods with native `int`, `double`, and `bool` signatures without materializing runtime values inside that graph.

A normal module function call roughly follows this path:

1. Read the module member for the function name. For example, `addOne` is read from the current `ScriptContext.Module` by string key.
2. The module read enters `ScriptObject.GetPropertyDatum`: hidden class/property metadata lookup, property descriptor checks, and any getter, CLR binding, prototype chain, or CLR fallback handling.
3. Convert the read value to a callable object.
4. Enter the compact call boundary, such as `CallOps.Invoke0..Invoke7` or `InvokeMany`.
5. The helper checks whether the target is a `ClosureFunction`, then enters `ClosureFunction.Invoke*`.
6. `ClosureFunction` pushes a lightweight frame on the active context, selects a fixed-arity or span delegate, invokes it, then restores the frame.

With direct call, an eligible call site takes a shorter path:

1. At compile time, AuroraScript proves that `addOne(value)` targets a specific function in the same module and binds the call site to that function's `FunctionId` / IL method.
2. At runtime, arguments are still evaluated in source order; fixed arguments are stored in temporary locals, missing arguments become `null`, and extra arguments are evaluated then discarded.
3. The generic direct adapter uses a lightweight frame on the active context for stack traces and error locations; no child `ScriptContext` is allocated.
4. The emitted IL directly `call`s the target method. When the compiler proves a numeric-only signature and body, parameters, locals, arithmetic, comparisons, and the return value remain native CIL values (`int` / `double` / `bool`) until a dynamic boundary is required. Proven-safe 32-bit integer loops, bitwise operations, and packed-array index values use `int`; operations that require JavaScript number semantics or may overflow use `double`.

Direct call mainly removes module property lookup, function-object conversion, `CallOps.Invoke*` dispatch, `ClosureFunction.Invoke*` dispatch, delegate arity switching, and many branches on the dynamic path. Generic direct calls retain a lightweight frame; proven native numeric calls inside the compiled call graph need neither dynamic dispatch nor `ScriptDatum` conversion. The win is most visible when a small numerical function is called repeatedly in a loop.

An eligible direct-call site must satisfy these rules:

- The call must be a static same-module name call, such as `addOne(x)`. Calls like `obj.addOne(x)`, `module.addOne(x)`, `var f = addOne; f(x)`, and cross-module import calls do not use this path.
- The call cannot use spread arguments. `addOne(...items)` falls back to the normal call path.
- The lightweight generic direct adapter and automatic direct-call inference currently cover functions with no more than 7 parameters. An explicitly marked `@directCall` function may have more than 7 parameters: when the call graph proves a native-specializable signature and body, static same-module call sites invoke that native method directly; otherwise the normal closure/span path remains available. Default parameters, `$args`, closure captures, and locals captured by nested functions still prevent this native path.
- Explicit `@directCall` preserves the function object on the module, so the function can still be exported, read, or invoked from the host. It only lets proven module-local call sites use the direct path.
- Automatic inference only runs when `EngineOptions.WithOptimization(optimization => optimization.EnableAutoModuleDirectCall = true)` is enabled, and only for module functions that are not assigned, not read as values, and appear only as direct call targets.
- direct call is never applied across modules, and marking a function does not force every call site to optimize. Ineligible call sites keep the normal path.

### Local Variables vs Module Members

Function parameters, `var` / `const` inside a function body, and local function declarations are bound to local slots by the backend. Proven number, Boolean, and string locals use native CLR slots (`double`, `bool`, and `string`); only dynamic values use `ScriptDatum`. Reads and writes are direct `ldloc` / `stloc` operations, with conversion deferred until an object, call, scope, or other dynamic runtime boundary.

Module-level declarations are different. Top-level `var`, `const`, `func`, `enum`, and import/export symbols are members of the module object. When a function accesses a module member, the compiler cannot treat it as a plain local because the module object is observable at runtime: the host can call `GetModule` / `Execute`, scripts can import/export it, hot patches can replace module members, and properties may have getter/setter behavior or be redefined. Therefore module member access usually starts from `ScriptContext.Module` and calls `ScriptObject.GetPropertyDatum` or `SetPropertyDatum` by name.

This is why locals are much faster:

- A local is a compile-time slot number and is read or written directly.
- A module member is a runtime property and must resolve property metadata by string name.
- Module reads must preserve property descriptors, getters, binding functions, prototype chains, and CLR fallback behavior.
- Module writes must check setters, writability, and hidden class/property slot updates.
- Repeated module access inside a loop repeats those dynamic checks; local access does not.

For performance-sensitive code, keep loop counters, accumulators, and repeatedly used module constants in method-local variables:

```javascript
@module(MAIN);

const scale = 3;

export func fastSum(items) {
    var localScale = scale;
    var total = 0;

    for (var item in items) {
        total = total + item * localScale;
    }

    return total;
}
```

If module state must be updated, read it into a local before the loop and write it back after the loop. That pays the module read/write cost only once:

```javascript
@module(MAIN);

var counter = 0;

export func addMany(items) {
    var value = counter;

    for (var item in items) {
        value = value + item;
    }

    counter = value;
    return value;
}
```

Captured variables sit between the two: they are no longer plain `ldloc` / `stloc`, because they are stored through an upvalue object, but they still avoid string-based module property lookup. In hot paths, prefer parameters and ordinary locals first, necessary captured variables second, and use module members for cross-function state, exported APIs, or host-observable data.

### Module-Level Const Inlining

Module-level `const` declarations are still treated as module members by default. Compile-time inlining is enabled explicitly with `EngineOptions.WithOptimization(optimization => optimization.EnableModuleConstInlining = true)`. This switch is independent from `Runtime.EnableHotReload`: hot reload being enabled does not automatically disable or enable this optimization; the compile option decides it.

When enabled, the compiler scans top-level `const` / `export const` declarations in source order inside the same module. If the initializer can be proven to be a side-effect-free primitive value at compile time, later reads in same-module function bodies are replaced with literals, and the constant's own module initialization writes the folded value directly:

```javascript
@module(TEST);

export const a1 = 1;
export const a5 = a1 + 4; // compiled as a5 = 5

export func test() {
    console.log(a5); // compiled as console.log(5)
}
```

More complex primitive expressions are folded in declaration order as well:

```javascript
export const NUM = 3.141592678987654321;
export const STR = 'this is string';
export const BOOL = true;
export const BASE = 10;
export const COMPLEX = BASE * NUM + 5;       // 36.41592678987654
export const TAG = BASE + '_' + 1;           // '10_1'
export const TEMPLATE = STR + BASE + '_' + TAG; // 'this is string10_10_1'
```

When the option is enabled, module initialization should write the folded values directly. For example, `COMPLEX` should emit something like `ScriptDatum.FromNumber(36.41592678987654)`, not re-run `GetPropertyDatum("BASE") * GetPropertyDatum("NUM") + 5`.

Currently eligible values and expressions are `null`, booleans, numbers, strings, and `+`, `-`, `*`, `/`, `%`, `!`, `&&`, `||`, or grouped expressions made from earlier same-module inlineable consts. Folding follows the runtime helpers where applicable: string operands in `+` concatenate, `null` is `0` for numeric arithmetic, and logical operators preserve short-circuit result semantics.

These cases are not inlined:

- Initializers containing function calls, constructors, property reads, index reads, array/object literals, lambdas, assignments, or other expressions that may have side effects.
- `export const fv = func();` is not inlined, because the result of `func()` is only known at runtime and the call may have side effects.
- Forward references are not inlined. In `export const a5 = a1 + 4; export const a1 = 1;`, `a5` is not folded.
- Imported or included module members are not inlined across module boundaries.
- If a local variable, parameter, or upvalue shadows the module const name, that local binding wins and is not replaced with the module constant.

This optimization only changes reads that are proven constant. It does not remove the module member itself: `export const` is still defined on the module object and remains visible to hosts and other modules with the original module semantics. Reads already inlined into same-module function bodies no longer perform a module property lookup, so they will not observe later host-side or hot-patch replacement of that const member; leave this option off when that dynamic observability matters.

## Language Features

The current test suite and examples cover:

- `var` / `const` / `func` / `function` / `return`
- `@module`, `@global()`, `@directCall`
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
- `Int32Array`
- `Int8Array`
- `BooleanArray`
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
- `Path`
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
- `Array.withCapacity(capacity)`

`Array.withCapacity(capacity)` creates an empty array with reserved backing storage. Unlike `new Array(n)`, it does not create `n` null slots, so it is intended for known-size push-heavy paths.

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

### Int32Array / Int8Array / BooleanArray

Packed arrays use contiguous CLR primitive storage for pathfinding, image data, state tables, and numeric loops:

```as
var scores = new Int32Array(1000000);
var costs = new Int8Array(1000000);
var visited = new BooleanArray(1000000);

scores[10] = 42;
visited[10] = true;
scores.fill(0);
```

- Length is fixed; new instances are initialized to `0`, `0`, and `false`, respectively.
- Numeric index reads/writes, read-only `length`, and `fill(value)` are supported. `push`, `pop`, and element deletion are not.
- Out-of-range access throws a runtime error. `Array.isArray(value)` returns `false` for packed arrays.
- When the compiler retains the exact packed-array type, it emits direct CLR array CIL and converts elements to `ScriptDatum` only at dynamic object, script-call, or host boundaries.
- Inside a native direct-call graph, packed-array parameters and locals use their `int[]`, `sbyte[]`, or `bool[]` backing storage directly. Calls between native functions neither reload a wrapper field nor re-wrap the array.
- Keep packed arrays in locals or pass them to `@directCall` hot functions for the fastest path. Reading one back from a dynamic object preserves semantics but uses the dynamic element-access boundary.

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

### Path

`Path` is the script-side protocol-aware path object. It stores normalized path text, uses `/` separators, and supports both file-like paths and protocol paths such as `mem://app/main.as` or `asset://pkg/textures/ui.png`. `Path` is an object type, and `==` compares normalized path text by value.

Constructor and static members:

- `new Path(root, ...segments)`
- `Path.of(root, ...segments)`
- `Path.isPath(value)`
- `Path.join(root, ...segments)`
- `Path.baseModule(...segments)`
- `Path.normalize(path)`
- `Path.directoryName(path)`
- `Path.fileName(path)`
- `Path.extName(path)`
- `Path.protocol(path)`
- `Path.changeExt(path, extension)`
- `Path.isRooted(path)`
- `Path.isUnderRoot(root, path)`
- `Path.currentFile()`
- `Path.currentDirectory()`

Instance members:

- `append(...segments)`
- `reset(root, ...segments)`
- `changeExt(extension)`
- `directoryName()`
- `fileName()`
- `extName()`
- `protocol()`
- `clone()`
- `toString()`

`Path.baseModule(...segments)` joins from the current module source directory, which is useful for module-local resource paths. `Path.currentFile()` and `Path.currentDirectory()` return the current module full path and directory; outside a module context they return `null`.

```javascript
@module(MAIN);

export func run() {
    var config = Path.changeExt(Path.baseModule("../assets", "config"), "as");
    var path = new Path("mem://app/scripts", "../shared", "main");
    path.changeExt("as");
    return [
        path.toString(),
        path.extName(),
        path.protocol(),
        config,
        Path.join(Path.currentDirectory(), "generated", "out.as")
    ];
}
```

### JSON

- `JSON.parse(text)`
- `JSON.stringify(value, [indented])`

JSON supports script primitives, objects, general and packed arrays, HashMap, and related script values. Circular references throw a runtime exception.

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

Output writers can be configured with `EngineOptions.WithRuntime(runtime => { runtime.ConsoleStdOut = ...; runtime.ConsoleErrorOut = ...; })`.

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

Current `net10.0` coverage matrix:

| Test class | Focus |
|---|---|
| `LexerTests` | Lexing, numbers/strings/regex, comments, malformed tokens |
| `ParserSyntaxTests` | Grammar branches, modules, import/include/export, function annotations, syntax diagnostics |
| `ExpressionExecutionTests` | Expressions, operators, member/index access, spread, assignment |
| `StatementExecutionTests` | Control flow, loops, closures, recursion, exceptions, domain isolation |
| `LanguageFeatureExecutionTests` | enum, lambdas, sparse arrays, truthiness, templates, assignment semantics |
| `ModuleCompilationTests` | Dependencies, parallel compile, cycles, error aggregation, cancellation |
| `CompilerBackendPlanTests` | Typed code, native CIL slots/signatures, direct calls, closures/upvalues, control flow, and constant folding |
| `CompileBlockTests` | CompileBlock parameters, invocation modes, invalid inputs, diagnostics, dynamic delegate lifetime |
| `CompilationModeTests` | Dynamic/OnlyRun/Persistence behavior and hot-reload settings |
| `RuntimeApiAndErrorTests` | Runtime APIs, error paths, `$state`, disposed domains |
| `BuiltInLibraryTests` | Math, String, Array, JSON, HashMap, Regex, StringBuffer, Console |
| `AdvancedRuntimeTypeTests` | Constructors, Object, freeze, Date, Proxy |
| `ClrInteropTests` | CLR constructors/properties/fields/methods/overloads/access restrictions |
| `SerializationTests` | JSON serialization/deserialization, circular references, malformed JSON |
| `ScriptDatumTests` | Datum payloads, equality, CLR collection conversion, Span helpers |
| `ValueOpsTests` | Arithmetic, comparison, bitwise, and conversion semantics at dynamic boundaries |
| `HotReloadTests` | Disabled hot reload, incremental patch, replacement patch, domain isolation |
| `ConcurrentRuntimeTests` | Same-domain/multi-domain concurrency, detached closure concurrency |
| `ReleaseRegressionTests` | Release direct calls, closure slots, stack balance, confusion, empty modules |
| `ClosureFunctionContextTests` | Context pool lifetime and detached invocation |
| `EngineOptionsAndSourceTests` | EngineOptions, SourceResolver, extensions, parallelism, null input |
| `CustomSourceResolverUsageTests` | Custom resolvers, virtual paths, memory/file composition, and entry resolution |
| `CoreSemanticsRegressionTests` | Core semantics, null addition, coercion, short-circuit logic, array capacity/indexing, object properties, closures/loops |

Coverage summary:

- Lexer: keywords, identifiers, Unicode, operators, numbers, strings, regex, comments, CRLF locations, malformed tokens.
- Parser: module metadata, import/include/export, declarations, expressions, lambdas, destructuring, control flow, exceptions, templates, regex, diagnostics.
- CompileBlock: parameter validation, local functions, domain/no-domain invocation, module-only rejection, source names, null input, dynamic delegate release.
- Expressions/statements: precedence, arithmetic, bitwise operations, comparison, logical operators, member access, spread, assignment, loops, exceptions, closures, recursion.
- Core semantic regressions: null numeric addition, string-participating addition, truthiness, short-circuit return values, array capacity versus `new Array(n)` semantics, object properties, closure loops.
- Module compilation: relative paths, diamond dependencies, duplicate roots, wide dependency graphs, cycles, error aggregation, cancellation, concurrent builds.
- Compiler backend plans: typed code, function annotations, slot/upvalue binding, native CIL signatures/locals, control flow, constant folding, and runtime-boundary calls.
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

The latest runtime hot-path results come from a BenchmarkDotNet `ShortRun` on Release/net10.0, measured on 2026-08-15:

```bash
dotnet run --project benchmark/Benchmark.csproj -c Release -- \
  --filter "*FunctionCallLoop*" "*ModuleCallLoop*" "*NumericLoop*"
```

Numbers from the old runtime architecture are no longer used as the current reference. Run without `--filter` to regenerate the complete suite.

Environment:

- BenchmarkDotNet `0.15.8`
- Windows 11 `10.0.28000.2704`
- Intel Core i7-13700KF
- .NET SDK `10.0.400`
- Runtime `.NET 10.0.11`
- Job `ShortRun`

Core runtime hot paths:

| Metric | Scale | Mean | Allocated | Notes |
|---|---:|---:|---:|---|
| `NumericLoop` | 1,000 | 1.477 µs | 0 B | Numeric locals and arithmetic stay in native CIL |
| `NumericLoop` | 10,000 | 13.483 µs | 0 B | Linear scaling with no loop-local allocation |
| `FunctionCallLoop` | 1,000 | 82.668 µs | 0 B | Same-module numeric calls use the native specialization |
| `FunctionCallLoop` | 10,000 | 766.803 µs | 0 B | Reused call frames avoid per-call contexts |
| `ModuleCallLoop` | 1,000 | 122.591 µs | 0 B | Cross-module generic call boundary is allocation-free |
| `ModuleCallLoop` | 10,000 | 1.186 ms | 0 B | Controlled dynamic-dispatch cost with no per-call allocation |

The benchmark reuses a prebuilt host argument array, so `Allocated` reflects the execution path itself; no managed allocations were detected for these six cases. `ShortRun` is intended for regression checks, and absolute timings should be remeasured on the target deployment hardware.

## Examples

- [examples/tests/main.as](examples/tests/main.as): module loading and entry-point sample.
- [examples/tests/unit.as](examples/tests/unit.as): built-in types, standard library, and language feature samples.
- [tests/AuroraScript.Tests](tests/AuroraScript.Tests): recommended behavioral specification.
- [benchmark/scripts/runtime.as](benchmark/scripts/runtime.as): runtime benchmark script.

## Language Tools

Language tooling lives under `language-tools/`:

- `AuroraScript.LanguageServices`: shared parsing, semantic indexing, diagnostics, and definition services.
- `AuroraScript.LanguageServer`: LSP server.
- `AuroraScript.VisualStudio`: Visual Studio VSIX project that packages the language server and TextMate grammar.
- `AuroraScript.Mcp`: MCP server for AI tools, exposing `documents` docs, schema, examples, and script validation.

---

Made by [l2060](https://github.com/l2060)
