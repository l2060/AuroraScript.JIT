<p align="center">
  <img src="icon.png" width="128" alt="AuroraScript Logo" />
</p>

<p align="center">
  <a href="./README.md">简体中文</a> | <a href="./README_EN.md">English</a>
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
When using `Persistence` mode, you can debug your scripts:
- **Breakpoints**: Set breakpoints in `.as` files.
- **Stepping**: Step Over, Step Into, Step Out.
- **Variables**: Inspect local variables, objects, and arrays.
- **Call Stack**: View the mixed call stack of script and C#.

### 2. Start Debugging
1.  Enable debugger in your C# host (if applicable).
2.  Set breakpoints in your `.as` files within Visual Studio.
3.  Run your host application.

*Visual Studio Debugging Example:*
![Debugger Demo](documents/debugger.png)


### 3. VS Code Extension
The current VS Code extension provides **Syntax Highlighting** and code colorizing to improve development experience.
- To install: Open `vscode-extension`, run `npm install`, `npm run package`, and install the generated `.vsix`.


*Built-in type definitions for easier development:*
![Type Definitions](documents/lib.d.as.png)

## 📚 Built-in API

The AuroraScript runtime provides a comprehensive standard library.

### Core Types

#### **Object**
The base object type from which all other objects derive.
- **Static Methods**:
  - `equal$(a, b)`: Strict equality comparison (reference check).
  - `equal(a, b)`: Value-based equality comparison.
  - `deepEqual(a, b)`: Deep recursive comparison of object contents.
  - `assign(target, ...sources)`: Copies all enumerable own properties from one or more source objects to a target object.
  - `keys(obj)`: Returns an array of a given object's own enumerable property names.
  - `clone(obj)` / `deepClone(obj)`: Creates a shallow or deep copy of the object.
  - `extends(proto, [target])`: Creates a new object with the specified prototype.
  - `freeze(obj)`: Freezes an object, preventing new properties from being added or existing ones from being removed or modified.
- **Instance Members**:
  - `length`: [Read-only] Returns the number of properties owned by the object.
  - `toString()`: Returns a string representation of the object.

#### **Array**
An ordered collection that supports dynamic resizing and common functional operations.
- **Static Methods**:
  - `from(iterable)`: Creates a new array from an array-like or iterable object.
  - `isArray(obj)`: Determines whether the passed value is an Array.
  - `of(...items)`: Creates a new array with a variable number of arguments.
- **Instance Members**:
  - `length`: [Property] Gets or sets the number of elements in the array.
  - `push(...items)` / `pop()`: Adds or removes elements at the end of the array.
  - `shift()` / `unshift(...items)`: Removes or adds elements at the beginning of the array.
  - `slice(start, [end])`: Returns a shallow copy of a portion of an array.
  - `join([sep])`: Joins all elements of an array into a string, separated by `sep` (default is `,`).
  - `reverse()` / `sort([cmp])`: Reverses the array in place or sorts it using an optional comparator.
  - `indexOf(val)` / `lastIndexOf(val)` / `has(val)`: Search and existence checks for elements.
  - `find(cb)` / `findIndex(cb)` / `findLast(cb)` / `findLastIndex(cb)`: Finds elements or their indices that satisfy a condition.
  - `map(cb)` / `filter(cb)` / `reduce(cb)` / `flat([depth])`: Closure-driven iteration and transformation operations.
  - `some(cb)` / `every(cb)`: Logical predicate checks.

#### **String**
An immutable sequence of characters.
- **Static Methods**:
  - `fromCharCode(...codes)`: Returns a string created from the specified sequence of UTF-16 code units.
  - `compare(a, b)`: Returns a number indicating whether a reference string comes before, after, or is the same as the given string.
- **Instance Members**:
  - `length`: [Property] Returns the number of characters in the string.
  - `substring(start, [end])` / `slice(start, [end])`: Extracts a section of a string.
  - `indexOf(sub)` / `lastIndexOf(sub)` / `contains(sub)`: Substring searching and matching.
  - `startsWith(sub)` / `endsWith(sub)`: Prefix and suffix checks.
  - `split(sep)`: Splits a string into an array of substrings.
  - `replace(search, repl)`: Replaces matches with a replacement string or the result of a callback.
  - `match(regex)` / `matchAll(regex)`: Pattern matching using regular expressions.
  - `trim()` / `trimLeft()` / `trimRight()`: Removes whitespace from ends.
  - `toLowerCase()` / `toUpperCase()`: Case conversion.
  - `charCodeAt(index)`: Returns the numeric Unicode value of the character at the given index.

#### **Date**
Handling of dates and times.
- **Static Methods**:
  - `now()` / `utcNow()`: Returns the current local or UTC time.
  - `parse(str)`: Parses a string representation of a date.
- **Instance Properties**:
  - `year` / `month` / `day` / `hour` / `minute` / `second` / `millisecond`: Access individual time components (read-only).
  - `dayOfWeek` / `dayOfYear` / `ticks`: Access week index, day of year, or raw ticks.

#### **HashMap**
A high-performance, thread-safe key-value collection powered by `ConcurrentDictionary`.
- **Instance Members**:
  - `size`: [Property] Returns the number of elements in the collection.
  - `set(key, val)` / `get(key)`: Access key-value pairs. Supports any type as a key.
  - `has(key)` / `delete(key)`: Check for existence or remove a specific member.
  - `getOrInsert(key, defaultVal/cb)`: Retrieves a value or atomically inserts a default/callback result if missing.
  - `keys` / `values`: [Property] Returns iterable collections of all keys or values.
  - `clear()`: Removes all elements from the collection.

#### **Regex**
Regular expression objects.
- `test(str)`: Executes a search for a match between a regular expression and a specified string.

---

### Standard Library

#### **console**
Standard I/O and performance debugging.
- `log(...args)`: Prints general information to the console. Multiple arguments are automatically comma-separated.
- `error(...args)`: Prints error information, including the call stack.
- `time(label)`: Starts a timer with the given label.
- `timeEnd(label)`: Stops the timer and prints the elapsed time in milliseconds.

#### **Math**
Common mathematical constants and functions.
- **Constants**: `PI`, `E`, `Tau`, `DEG_PER_RAD`.
- **Methods**:
  - `abs(x)`: Returns the absolute value of x.
  - `max(...args)` / `min(...args)`: Returns the largest or smallest of the provided numbers.
  - `random()`: Returns a pseudo-random number in the range [0, 1).
  - `floor(x)` / `round(x)`: Rounds down or to the nearest integer.
  - `pow(x, y)` / `log(x)` / `exp(x)`: Power, natural logarithm, and exponential functions.
  - `sin(x)` / `cos(x)` / `tan(x)`: Standard trigonometric functions (radians).

#### **JSON**
Utilities for JSON serialization and deserialization.
- `parse(text)`: Parses a JSON string into a script object.
- `stringify(obj, [indented])`: Serializes a script object to a JSON string. Enables pretty-printing if `indented` is true.

#### **StringBuffer**
A builder designed for high-performance large-scale string concatenation.
- `append(...args)`: Appends one or more items to the end.
- `appendLine(...args)`: Appends content followed by a platform-specific newline.
- `insert(index, str)`: Inserts a string at the specified index offset.
- `clear()`: Resets the buffer.
- `toString()`: Generates the final concatenated string.

#### **Proxy**
Intercepts and defines custom behavior for fundamental object operations.
- `new Proxy(target, handlers)`:
  - **Notes**: A complete `handlers` object must be provided. It currently supports intercepting `get`, `set`, and `unset` (i.e., `delete`) operations.

#### **HotPatch**
Dynamic runtime module patching and repair.
- `replace(modulePath, script, [ignoreDeps])`: Fully replaces the logic of a module at the specified path.
- `incremental(modulePath, script, [ignoreDeps])`: Incrementally adds or updates module members.
  - **Notes**: Top-level code in the patch script (e.g., variable initialization) will re-execute immediately upon application.

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
|  |  |  |  |
|  |  |  |  |
|  |  |  |  |
|  |  |  |  |

> Measured on Intel Core i7-13700KF, .NET 10.0.1.

## 📂 Examples

- [**Basic Tests**](examples/tests/main.as): Syntax and module loading.
- [**Benchmarks**](benchmark/scripts/unit.as): Performance scripts.

---

Made with ❤️ by [l2060](https://github.com/l2060)
