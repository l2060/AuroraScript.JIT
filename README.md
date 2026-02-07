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

AuroraScript is a lightweight, weak-typed script execution engine built on .NET. It compiles scripts into bytecode and executes them on a custom Virtual Machine (VM), designed to be fast, embeddable, and easy to use.

While inspired by JavaScript syntax and mechanisms, AuroraScript is a distinct language with its own optimizations and features, and does not adhere to ECMA specifications. It supports native .NET integration and debugging.

> [!NOTE]
> 🚧 **Work in Progress**: The project is still in development. Performance and API stability are improving. We welcome **PRs** and **Issues** to help make AuroraScript better!

## ✨ Features

- **Lightweight & Fast**: No third-party dependencies. Bytecode compilation and optimized VM execution.
- **Weak Typed**: Flexible variable typing similar to JavaScript.
- **Native Interop**: Seamlessly register and use .NET (CLR) types and functions within scripts.
- **Debugging Support**: Full VS Code debugger support (Breakpoints, stepping, variable inspection, call stack).
- **Module System**:
  - `import xxx from 'xxx'`: Import module exports.
  - `include 'xxx.as'`: Embed script files directly.
  - `@module("NAME")`: Define module name.
- **Advanced Control Flow**:
  - `yield`: Interrupt execution.
  - `Interruption & Continue`: Pause and resume script execution from external host.
  - `debugger`: Programmatic breakpoint.
  - `where` / `for` loop enhancements.
- **Modern Syntax**:
  - Closures, Lambdas, and Function Pointers.
  - Destructuring assignment: `var { a, b } = obj;` and `var [ a, ...b ] = arr;`.
  - Spread operator: `...` for arrays and objects.
  - Template literals: Multi-line strings with `` ` `` or `|>` syntax.
- **Standard Library**: Built-in support for `Math`, `JSON`, `Date`, `Regex`, and `StringBuffer`.

## 🚀 Getting Started

### Installation via NuGet

You can easily install the AuroraScript engine via NuGet:

```bash
dotnet add package AuroraScript
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
engine.RegisterClrType(typeof(Math), "Math2");

// 3. Compile Scripts
await engine.BuildAsync();

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

AuroraScript provides a full-featured VS Code debugger.

### 1. Setup Extension

1.  **Open Extension Folder**: Open `vscode-extension` in VS Code.
2.  **Install Dependencies**: `npm install`
3.  **Package**: `npm run package` -> Generates a `.vsix` file.
4.  **Install**: Install the `.vsix` file via VS Code Extensions menu.

### 2. Configure Wrapper

Create `.vscode/launch.json` in your script project:

```json
{
    "version": "0.2.0",
    "configurations": [
        {
            "type": "AuroraScript",
            "request": "attach",
            "name": "Attach to AuroraScript",
            "host": "localhost",
            "port": 26010
        }
    ]
}
```

### 3. Start Debugging

1.  Enable debugger in your C# host:
    ```csharp
    engine.EnableDebugger();
    await engine.WaitAnyDebugger(TimeSpan.FromSeconds(60));
    ```
2.  Run host.
3.  Press `F5` in VS Code.

### Features
- **Breakpoints**: Set breakpoints in `.as` files.
- **Stepping**: Step Over, Step Into, Step Out.
- **Variables**: Inspect properties of Objects, Arrays, and Closures.
- **Call Stack**: View call frames handling script execution.
- **`debugger` Statement**: Use the `debugger;` keyword in your code to trigger a programmatic breakpoint.

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
| **StringBuffer** | String Builder | `append(str)`, `toString()`, high-perf string concatenation |

### Global Context
- `global`: References the root global scope.
- `$state`: Access user-injected state object (from C# `ExecuteOptions.WithUserState`).
- `$args`: Array of arguments passed to the current function.

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
