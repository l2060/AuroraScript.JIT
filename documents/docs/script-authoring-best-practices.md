# AuroraScript Script Authoring Best Practices

Version target: AuroraScript.JIT 3.0.0

This document tells AI agents how to write AuroraScript code by default. Use it with `docs/aurora-script-ai.md`, `docs/language-reference.md`, and `schema/runtime-api.json`.

## Default Workflow For AI

1. Decide whether the user needs a full module or a `CompileBlock` body.
2. For a full script file, start with `@module(NAME);` and put every `include` or `import` before declarations.
3. Write the smallest runnable module with one clear exported entry function.
4. Prefer runtime APIs listed in `schema/runtime-api.json`; do not invent JavaScript APIs.
5. Run `aurora_check_script` before returning generated code.
6. Run `aurora_run_script` when expected output or behavior can be checked quickly.

## Authoring Priority

When several valid styles are possible, choose this order:

1. Correct, compile-checked AuroraScript over JavaScript-like code.
2. Clear module boundaries over hidden `include` coupling.
3. Plain serializable return values over console-only output.
4. Local variables for repeated dynamic reads over repeated property access.
5. Small named helpers over duplicated logic.
6. A runnable example over an unverified snippet.

## Choose Module Or CompileBlock

Use a full module when the script has any of these:

- Named exported functions.
- Imports or includes.
- Shared helpers or constants.
- Host-visible behavior that should be built once and executed many times.
- Code that may be hot patched.

Use `CompileBlock` only for a small function body that the host invokes directly with known parameter names.

Do not transform a module into a block just to make a short answer. If the user asks for a `.as` file, generate a module.

## Full Module Template

Use this shape for normal script files:

```as
@module(MAIN);
import util from "./util";

const DEFAULT_LIMIT = 10;

func normalize(value) {
    if (value == null) {
        return "";
    }
    return value.toString().trim();
}

export func run(input) {
    var text = normalize(input);
    return {
        text,
        limit: DEFAULT_LIMIT
    };
}
```

Rules:

- Put `@module(NAME);` first when present.
- Put `include` and `import` immediately after `@module`.
- Put exported API near the bottom after private helpers.
- Export only values the host or other modules need.
- Use stable uppercase module names such as `MAIN`, `UTIL`, or domain names from the user.
- Keep module initialization cheap. Put work inside exported functions unless a module-level constant is enough.

## CompileBlock Template

Use this only when the host wants a function body:

```as
var total = 0;
var count = values.length;
for (var i = 0; i < count; i++) {
    total += values[i];
}
return total;
```

Rules:

- Do not use `@module`, `import`, `include`, `export`, or `declare`.
- Treat provided parameter names as local variables.
- End with `return` unless side effects are the only goal.
- Validate with the exact parameter names the host will pass.

## Imports And Includes

Prefer `import` when a dependency has exported members:

```as
import math from "./math";

export func run(value) {
    return math.square(value);
}
```

Use `include` only when the dependency is intentionally merged for declarations or shared setup:

```as
include "./shared";
```

Rules:

- Use relative paths from the importing file: `./lib`, `../shared/util`.
- Omit `.as` unless the user explicitly writes it; the resolver can add the configured extension.
- Do not place imports after variables or functions.
- Do not assume file-system roots, `compiler.Directory`, or a process working directory. Path resolution is resolver-owned.
- The parser preserves raw paths; the module graph calls `ResolveAsync`, then loads the returned reference with `GetSourceAsync`.
- Entry paths resolve from the resolver root. Dependency paths resolve from the directory of the importing file's full path.
- Use `/` in paths that you generate, including on Windows.
- Prefer one module per file. Give each imported file its own `@module(NAME);`.
- In examples for MCP validation, include dependency text in the `sources` object using paths relative to the tool root/source root.
- Memory overlays only override later sources when the resolved target falls under the memory root; otherwise different roots or protocols remain isolated.
- If a file under `d:/a/b/c/d` imports `../test`, the resolved target is `d:/a/b/c/test.as`. A memory overlay rooted at `d:/a/b/c` can provide that source when it is ordered before the file-system resolver.

## Declarations And Scope

Prefer `const` for names that are never reassigned:

```as
const taxRate = 0.13;
var total = subtotal + subtotal * taxRate;
```

Use `var` when the value changes:

```as
var total = 0;
var itemCount = items.length;
for (var i = 0; i < itemCount; i++) {
    total += items[i].price;
}
```

Rules:

- Declare one name per `var` or `const` statement.
- Do not write `var a = 1, b = 2;`.
- Do not redeclare a name in the same scope.
- A child block may shadow an outer `var`, but should only do so when it improves clarity.
- Do not shadow a visible `const`.
- Do not assign, increment, or compound-assign a `const`.

## Functions

Prefer named private helpers for reusable logic:

```as
func clamp(value, min, max) {
    if (value < min) {
        return min;
    }
    if (value > max) {
        return max;
    }
    return value;
}
```

Use lambdas for short local transformations:

```as
var toName = item => item.name;
```

Rules:

- Keep parameter names unique.
- Put a rest parameter last.
- Do not give a rest parameter a default value.
- Use block lambdas when there is more than one expression.
- Avoid creating closures inside hot loops unless the closure is the intended result.

## Data Shape

Prefer plain objects and arrays for host-friendly results:

```as
return {
    success: true,
    items: results,
    count: results.length
};
```

Prefer `null` for missing optional values:

```as
if (user == null) {
    return null;
}
```

Use `HashMap` only when key lookup behavior is central to the algorithm. For JSON-like outputs, use objects.

## Input And Output Contracts

For host-called scripts, make the entry function contract obvious:

```as
@module(PRICING);

const DEFAULT_RATE = 0.1;

func readAmount(input) {
    if (input == null) {
        return 0;
    }
    if (input.amount == null) {
        return 0;
    }
    return input.amount;
}

func readRate(input) {
    if (input == null) {
        return DEFAULT_RATE;
    }
    if (input.rate == null) {
        return DEFAULT_RATE;
    }
    return input.rate;
}

export func run(input) {
    var rate = readRate(input);
    var amount = readAmount(input);
    var tax = amount * rate;
    return {
        amount,
        rate,
        tax
    };
}
```

Rules:

- Return data from the entry function; use `console.log` only for diagnostics.
- Prefer objects with stable property names for multi-value results.
- Check nullable input before reading nested properties.
- Do not mutate host input unless the user explicitly asks for mutation.

## Strings

Prefer template strings for formatting:

```as
return `name=${name}, count=${count}`;
```

Use `StringBuffer` for large loop-built strings:

```as
var buffer = new StringBuffer("");
var rowCount = rows.length;
for (var i = 0; i < rowCount; i++) {
    buffer.appendLine(rows[i]);
}
return buffer.stringAndRelease();
```

Rules:

- Do not manually build long strings with repeated `+` in loops.
- Use `stringAndRelease()` when the buffer is no longer needed.
- Use `toString()` when the buffer will continue to be used.

## Arrays And Loops

Use index loops when order and performance matter:

```as
var total = 0;
var count = values.length;
for (var i = 0; i < count; i++) {
    total += values[i];
}
return total;
```

Use `for-in` when enumerating dynamic object keys or collection values is clearer:

```as
for (var key in object) {
    console.log(key);
}
```

Rules:

- Cache repeated dynamic lookups:

```as
var items = model.items;
var itemCount = items.length;
for (var i = 0; i < itemCount; i++) {
    process(items[i]);
}
```

- Cache loop bounds in locals before index loops. Do not use `items.length` directly as the loop condition.
- Prefer runtime array methods only when they make the code simpler and are known in `runtime-api.json`.
- Do not assume unsupported JavaScript methods exist.
- Avoid closures in hot loops. Use a named helper or an index loop unless a closure is the result you need.

## Performance Defaults For AI

Use these defaults unless the user asks for a different style:

- Cache loop bounds before index loops: `var count = values.length;`.
- Cache repeated object paths before reuse: `var items = model.items;`.
- Use `const` for module constants and local values that do not change.
- Use `StringBuffer` for large loop-built strings.
- Keep `console.log` and `console.error` out of hot paths.
- Avoid array methods such as `map`, `filter`, or `reduce` in performance-sensitive examples unless clarity matters more and the API is known in `runtime-api.json`.

## Error Handling

Use `try/catch/finally` when the script can recover or must clean up:

```as
try {
    return readValue();
} catch (error) {
    console.error(error);
    return null;
}
```

Use `throw` when the caller should handle the failure:

```as
if (name == null) {
    throw new Error("name is required");
}
```

Rules:

- Return structured errors when the host expects data.
- Throw `Error` when execution should fail.
- Do not swallow errors silently.

## Host Interop Friendly Code

When scripts are called from .NET hosts:

- Export one stable entry function such as `run`, `main`, or a user-specified name.
- Return values that serialize cleanly: number, string, boolean, null, arrays, and plain objects.
- Keep console output separate from return values.
- Avoid relying on ambient globals unless the host explicitly defines them.
- When using host-defined services, keep access behind a small function so tests can replace it.

Example:

```as
@module(REPORT);

func logInfo(message) {
    if (hostLog != null) {
        hostLog(message);
    }
}

export func run(items) {
    var count = items.length;
    logInfo(`items=${count}`);
    return { count };
}
```

## Hot Patch Friendly Code

If a module may be patched:

- Keep exported function names stable.
- Prefer small functions over large monolithic functions.
- Keep module-level mutable state minimal and explicit.
- Avoid hiding behavior in `include` files unless the patch process intentionally includes them.
- In scripts, prefer `HotPatch.replace(script)` or `HotPatch.incremental(script)` when patching the current module.
- If a script supplies a patch module path, relative paths are resolved from the current module full path. Host-side patch APIs still require an absolute file path or virtual full path.

## Validation Examples

Always validate the exact shape being returned to the user. If dependencies are shown, validate them together.

When writing a module, validate with dependencies:

```json
{
  "mode": "module",
  "sourceName": "main.as",
  "source": "@module(MAIN); import util from './util'; export func run() { return util.value(); }",
  "sources": {
    "util.as": "@module(UTIL); export func value() { return 42; }"
  }
}
```

When writing a block:

```json
{
  "mode": "block",
  "source": "return left + right;",
  "parameters": ["left", "right"],
  "arguments": [20, 22]
}
```

When validating loop code, use the recommended cached-bound form:

```json
{
  "mode": "block",
  "source": "var total = 0; var count = values.length; for (var i = 0; i < count; i++) { total += values[i]; } return total;",
  "parameters": ["values"],
  "arguments": [[1, 2, 3, 4]]
}
```

## Common AI Mistakes To Avoid

- Do not write JavaScript-only APIs without checking `runtime-api.json`.
- Do not use `let` or `class`.
- Do not write multi-binding declarations such as `var a = 1, b = 2;`.
- Do not put `import` or `include` after normal declarations.
- Do not use module syntax inside `CompileBlock`.
- Do not redeclare same-scope names.
- Do not mutate `const`.
- Do not use repeated dynamic property reads such as `items.length` directly in loop conditions; cache the bound first.
- Do not assume a path is file-system based; custom resolvers may load from memory, database, or virtual stores.
- Do not return console output as the only result when the host expects a value.
- Do not generate a full JavaScript program. Generate AuroraScript syntax and validate it with AuroraScript tooling.
- Do not use broad runtime APIs until they are confirmed in `schema/runtime-api.json`.
