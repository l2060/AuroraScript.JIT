<p align="center">
  <img src="icon.png" width="128" alt="AuroraScript Logo" />
</p>

<p align="center">
  <a href="./README_CN.md">简体中文</a> | <a href="./README.md">English</a>
</p>

# AuroraScript

[![License](https://img.shields.io/badge/license-MIT-blue.svg)](LICENSE)
[![Build Status](https://img.shields.io/badge/build-passing-brightgreen.svg)](https://github.com/l2060/AuroraScript)
[![Version](https://img.shields.io/badge/version-1.0.0-orange.svg)](package.json)

AuroraScript is a lightweight, weak-typed script execution engine built on .NET. It compiles scripts directly into CIL (Common Intermediate Language) and executes them using the .NET JIT compiler, designed to be extremely fast, embeddable, and high-performance.

While inspired by JavaScript syntax and mechanisms, AuroraScript is a distinct language with its own optimizations and features, and does not adhere to ECMA specifications. It leverages native .NET infrastructure for execution, interop, and debugging.

> [!NOTE]
> 🚧 **Work in Progress**: The project is still in development. Performance and API stability are improving. We welcome **PRs** and **Issues** to help make AuroraScript better!

## ✨ Features

- **High Performance**: No third-party dependencies. Compiles to native CIL/MSIL, leveraging the .NET JIT compiler for execution.
- **Weak Typed**: Flexible variable typing similar to JavaScript.
- **Native Interop**: Seamlessly register and use .NET (CLR) types and functions within scripts.
- **Debugging Support**: Full Visual Studio debugger support. (VS Code extension currently provides syntax highlighting only).
- **Module System**:
  - `import xxx from 'xxx'`: Import module exports.
  - `include 'xxx.as'`: Embed script files directly.
  - `@module("NAME")`: Define module name.
- **Advanced Control Flow**:
  - `debugger`: Programmatic breakpoint.
  - `where` / `for` loop enhancements.
- **Compilation Modes**:
  - `Persistence`: Compiles to persistent assemblies (DLL) with PDB symbols. Supports source-level debugging and programmatic breakpoints. Fully inspectable and dumpable.
  - `OnlyRun`: Transient in-memory compilation. No managed debug mapping. Transparent to external profilers/dumpers; code resides in readable memory segments.
  - `Dynamic`: Emits CIL via `DynamicMethod`. Metadata-free for peak performance. Black-box execution: non-inspectable and non-dumpable.
- **HotPatch (Hot-fix)**: Update script logic in a running domain without losing state. Supports `Replace` and `Incremental` modes via .NET or script APIs.
- **Obfuscation**: Built-in support for obscuring constants, member names, and code structure.
- **Modern Syntax**:
  - Closures, Lambdas, and Function Pointers.
  - Destructuring assignment: `var { a, b } = obj;` and `var [ a, ...b ] = arr;`.
  - Spread operator: `...` for arrays and objects.
  - Template literals: Multi-line strings with `` ` `` or `|>` syntax.
- **Standard Library**: Built-in support for `Math`, `JSON`, `Date`, `Regex`, `HashMap`, `Proxy`, and `StringBuffer`.

## 🚀 Getting Started

### Installation via NuGet

You can easily install the AuroraScript engine via NuGet:

```bash
dotnet add package AuroraScript.JIT
```

### Manual Installation

Clone the repository:

```bash
git clone https://github.com/l2060/AuroraScript.git
cd AuroraScript
```

### Compiling the Project

To build the core engine library:

```bash
dotnet build src/AuroraScript.csproj -c Release
```

## 📖 Usage

### Running a Script (Host Application)

You can host the engine in your own .NET application.

```csharp
using AuroraScript;
using AuroraScript.Runtime;

// 1. Initialize Engine
var options = EngineOptions.Default.WithBaseDirectory("./scripts/");
var engine = new AuroraEngine(options);

// 2. Register CLR Types/Functions
engine.RegisterType<Math>("Math2");

// 3. Compile Scripts (Search and build all .as files in base directory)
await engine.BuildAsync(engine.SearchAllFileSource(Encoding.UTF8));

// 4. Create Domain & Execute
var domain = engine.CreateDomain();
// Execute 'main' function in 'MAIN' module
domain.Execute("MAIN", "main");
```

### Writing Scripts

AuroraScript uses a syntax familiar to JavaScript developers:

```javascript
@module("MAIN");

func main() {
    console.log("Hello, AuroraScript!");
    
    // Using imported CLR library
    var res = Math2.Abs(-100);
    console.log("Abs result: " + res);

    var list = [1, 2, 3, 4, 5];
    for (var item in list) {
        if (item % 2 == 0) {
            console.log("Even number: " + item);
        }
    }
}
```

## 🐞 Debugging Scripts

AuroraScript supports full-featured debugging within **Visual Studio**.

### 1. Visual Studio Debugging
When using `Persistence` or `OnlyRun` mode, you can debug your scripts:
- **Breakpoints**: Set breakpoints in `.as` files.
- **Stepping**: Step Over, Step Into, Step Out.
- **Variables**: Inspect local variables, objects, and arrays.
- **Call Stack**: View the mixed call stack of script and C#.

### 2. VS Code Extension
The current VS Code extension provides **Syntax Highlighting** and code colorizing to improve development experience.
- To install: Open `vscode-extension`, run `npm install`, `npm run package`, and install the generated `.vsix`.

### 3. Start Debugging
1.  Enable debugger in your C# host (if applicable).
2.  Set breakpoints in your `.as` files within Visual Studio.
3.  Run your host application.

*Visual Studio Debugging Example:*
![Debugger Demo](documents/debugger.png)

*Built-in type definitions for easier development:*
![Type Definitions](documents/lib.d.as.png)

## 📚 Built-in API

The AuroraScript runtime provides a comprehensive standard library.

### Core Types

| Type | Description | Key Methods |
| :--- | :--- | :--- |
| **Object** | Base object type | `keys()`, `values()`, `assign()`, `toString()` |
| **String** | Textual data | `length`, `substring`, `indexOf`, `split`, `replace`, `trim` |
| **Number** | Numeric values | `toFixed`, `toString`, `isNaN` |
| **Boolean** | Logical values | `toString`, `valueOf` |
| **Array** | Ordered collection | `push`, `pop`, `shift`, `slice`, `splice`, `join`, `map` |
| **Function** | Callable object | `call`, `apply`, `bind` |

### Standard Library

| Object | Description | Key Methods |
| :--- | :--- | :--- |
| **console** | Logging I/O | `log(msg)`, `error(msg)`, `warn(msg)` |
| **Math** | Math Utilities | `sin`, `cos`, `tan`, `sqrt`, `pow`, `random`, `PI`, `E` |
| **JSON** | JSON Serialization | `parse(string)`, `stringify(object)` |
| **Date** | Date Time | `now()`, `parse(string)`, constructors `new Date()` |
| **Regex** | Regular Expressions | constructors `new Regex(pattern)`, `match(str)`, `replace(str, repl)` |
| **HashMap** | Key-Value collection | `set(key, val)`, `get(key)`, `has(key)`, `delete(key)`, `clear()` |
| **Proxy** | Object Proxying | constructors `new Proxy(target, handler)`, intercept get/set/etc. |
| **StringBuffer** | String Builder | `append(str)`, `toString()`, high-perf string concatenation |

### Global Context
- `global`: References the root global scope.
- `$state`: Access user-injected state object (from C# `ExecuteOptions.WithUserState`).
- `$args`: Array of arguments passed to the current function.

## 🔥 HotPatch (Hot-fix)

AuroraScript provides powerful hot-fix capabilities, allowing you to update script logic in a running `ScriptDomain` without restarting the application or losing runtime state.

### 1. .NET API (Host Side)
Use `domain.DynamicPatch` to apply patches from the host:

```csharp
// Apply a replacement patch
domain.DynamicPatch(engine.MemorySource("module.as", "func newFunc() { ... }"), HotPatchType.Replace);

// Apply an incremental patch
domain.DynamicPatch(engine.MemorySource("module.as", "var newVar = 1;"), HotPatchType.Incremental);
```

### 2. Script API (Script Side)
The global `HotPatch` object allows scripts to patch themselves or other modules:

```javascript
// Replace all members of 'MAIN' module
HotPatch.replace("MAIN", "func main() { console.log('Fixed!'); }");

// Incrementally add/update members in 'UTILS' module
HotPatch.incremental("UTILS", "func helper() { return 42; }");
```

### 3. Working Mechanism
Hot-patching works via the `IncrementalCompiler`, which performs a partial JIT compilation. It links the new code to the existing `ScriptGlobal` environment and updates the `ScriptObject` representing the target module.

### 4. Precautions & Best Practices
- **Top-level Re-execution**: When a module is patched, its top-level code (variable initializations, etc.) will re-execute.
- **Function Signatures**: Ensure new function signatures match existing call sites to maintain compatibility.
- **Replace vs. Incremental**: 
    - `Replace`: Destructive. Clears all existing properties of the module before applying new code.
    - `Incremental`: Safe. Keeps existing properties and only updates or adds new members.
- **State Persistence**: Variables defined at the module level will be re-initialized if they are part of the patch code.

## 📊 Benchmark Results

Performance is a priority. We encourage community contributions to optimize further!

| Method | Mean | StdDev | Allocated |
| :--- | :---: | :---: | :---: |
| **TestIfTrue** | 225.1 ns | 0.19 ns | - |
| **TestAssuming** | 227.0 ns | 0.51 ns | - |
| **TestAddVar** | 240.6 ns | 0.50 ns | - |
| **TestGetVar** | 243.5 ns | 0.37 ns | - |
| **TestSetVar** | 250.7 ns | 0.39 ns | - |
| **TestSetProperty** | 257.0 ns | 1.05 ns | 48 B |
| **TestGetProperty** | 269.3 ns | 0.44 ns | - |
| **TestClone** | 515.6 ns | 1.34 ns | 1072 B |
| **TestIterator** | 822.1 ns | 2.09 ns | 1056 B |
| **TestJson** | 1,907.2 ns | 6.53 ns | 4520 B |
| **TestRegex** | 3,604.4 ns | 7.77 ns | 8696 B |
| **TestStrings** | 979.7 $\mu$s | 3.27 $\mu$s | 3.48 MB |
| **TestClosure** | 1.00 ms | 2.27 $\mu$s | 496 B |
| **TestObjects** | 1.38 ms | 5.06 $\mu$s | 5.14 MB |
| **TestArrays** | 1.40 ms | 15.13 $\mu$s | 2.22 MB |
| **TestFor100W** | 10.25 ms | 112.7 $\mu$s | - |

> Measured on Intel Core i7-13700KF, .NET 10.0.1.

## 📂 Examples

- [**Basic Tests**](examples/tests/main.as): Syntax and module loading.
- [**Benchmarks**](benchmark/scripts/unit.as): Performance scripts.

---

Made with ❤️ by [l2060](https://github.com/l2060)
