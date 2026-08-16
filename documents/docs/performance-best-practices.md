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

When the compiler proves a native call graph, integer and numeric parameters, locals, arithmetic, comparisons, and returns stay as CIL `int`, `double`, and `bool` values. Conversion to `ScriptDatum` happens only at a dynamic boundary. The generic direct adapter and automatic inference currently cover up to seven parameters; an explicit `@directCall` may exceed that limit when its call graph can be specialized, with the ordinary closure/span path retained as the semantic fallback.

Keep hot helper arguments type-stable. Reassigning a parameter is fine when every assignment preserves its proven native type; assigning unrelated dynamic values forces that parameter back to the dynamic path.

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

## Packed Primitive Arrays

Use `Int32Array`, `Int8Array`, or `BooleanArray` for fixed-size homogeneous data instead of a general `Array`:

```as
var distances = new Int32Array(nodeCount);
var terrain = new Int8Array(nodeCount);
var closed = new BooleanArray(nodeCount);

for (var i = 0; i < nodeCount; i++) {
    distances[i] = i;
    closed[i] = false;
}
```

These arrays have primitive CLR backing storage and rely on CLR zero initialization. When the exact array type remains visible to flow analysis, generated code uses native `ldelem`/`stelem` instructions and keeps numeric values unboxed through the loop.

Within a specialized direct-call graph, packed-array parameters and locals are passed as raw `int[]`, `sbyte[]`, or `bool[]` storage. Native helper-to-helper calls therefore do not reload wrapper fields or allocate replacement wrappers.

Keep the array in an exact local or pass it directly to an `@directCall` helper. Storing it in an ordinary object and reading it back erases the compile-time element type; access remains allocation-free apart from the array itself, but it uses the dynamic helper path and is measurably slower. This explicit boundary keeps the runtime small and predictable without speculative object-shape optimization.

## Integer Kernels

Use signed bitwise operations and `Int32Array`/`Int8Array` values when the algorithm is naturally 32-bit. The compiler keeps proven-safe integer literals, packed-array loads, signed bitwise results, and standard bounded loop induction variables as native CIL `int` values. It widens to `double` whenever JavaScript number semantics require it, including possible arithmetic overflow, division, unsigned right shift, negative zero, `NaN`, and infinity.

This means an integer-oriented loop can avoid repeated `double` conversions without changing observable numeric behavior. Do not add manual casts solely for performance; keep values type-stable and let the flow analysis widen at the first operation that needs number semantics.

## Console

`console.log` and `console.error` format objects and may allocate strings.
Keep them out of hot paths and benchmarks.

## Hot Patch

Disable hot reload for stable production builds unless dynamic patching is required.
Hot patch support is valuable operationally, but it can restrict some optimization choices.

## Module Graph

Use `CompilerOptions.MaxDegreeOfParallelism` to tune large module graph parsing.
The default uses processor count.

