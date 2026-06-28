# AuroraScript Performance Best Practices

This document lists practical rules that match AuroraScript.JIT compiler and runtime behavior.

## Compilation Mode

- `CompilationMode.Dynamic`: fastest in-memory path and best for production execution when persisted output is not needed.
- `CompilationMode.OnlyRun`: in-memory assembly path, useful when dynamic-method limitations matter.
- `CompilationMode.Persistence`: emits persistent assembly output and is the right choice for DLL/PDB workflows.

## Constants

Use `const` for values that should not change.

```as
const prefix = "item";
export const label = `${prefix}:42`;
```

When module const inlining is enabled, eligible module-level constants can be folded into use sites.
This reduces runtime reads and can remove repeated conversions.

Do not assign to `const`. The compiler rejects `=`, `+=`, `-=`, `*=`, `/=`, `%=`, `++`, and `--` against constants.

## Template Strings

Use template strings for formatting.

```as
return `id=${id}, value=${value}`;
```

The compiler emits small templates with concat-style code and larger templates with builder-style code.
Avoid manually expanding templates into repeated `+` operations unless you have measured a real gain.

## StringBuffer

Use `StringBuffer` for loops that append many parts.

```as
var buffer = new StringBuffer("");
for (var i = 0; i < count; i++) {
    buffer.append(i, "\n");
}
return buffer.stringAndRelease();
```

Use `stringAndRelease()` when the buffer will not be used again. It returns the built string and returns the backing builder to the runtime pool.

## Direct Calls

For stable same-module helper functions, let the compiler infer direct calls.
If a helper must remain directly callable through conservative cases, use:

```as
@directCall
func helper(value) {
    return value + 1;
}
```

Use `@directCall(false)` to disable the directive for a function.

## Loops And Closures

Avoid creating closures in hot loops unless each closure is required.

Prefer:

```as
var count = values.length;
for (var i = 0; i < count; i++) {
    total += values[i];
}
```

Cache loop bounds such as `values.length` before index loops. Do not put dynamic property reads directly in hot loop conditions.

Use closures when behavior needs captured state, not as a default loop abstraction.

## Dynamic Property Access

Cache repeated dynamic lookups in local variables when the same value is used multiple times.

```as
var items = model.items;
var itemCount = items.length;
for (var i = 0; i < itemCount; i++) {
    process(items[i]);
}
```

## Console

`console.log` and `console.error` format objects and may allocate strings.
Keep them out of hot paths and benchmarks.

## Hot Patch

Disable hot reload for stable production builds unless dynamic patching is required.
Hot patch support is valuable operationally, but it can restrict some optimization choices.

## Module Graph

Use `CompilerOptions.MaxDegreeOfParallelism` to tune large module graph parsing.
The default uses processor count.

