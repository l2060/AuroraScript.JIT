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

When module const inlining is enabled, eligible primitive `const` values and enum
members are folded at same-module and imported-module use sites. For example,
`constants.LIMIT + 1` loads the constant directly instead of resolving the module
and reading a property at runtime. This also preserves the inferred primitive type
through subsequent arithmetic.

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

Direct-call ABI optimization is explicit. Declare hot helpers with stable
parameter and return contracts as native functions:

```as
native func helper(Number value) Number {
    return value + 1;
}
```

Native parameters, locals, arithmetic, comparisons, and returns stay as CLR
values whenever their declared and inferred types permit it. Conversion to
`ScriptDatum` happens only at a dynamic boundary. Ordinary functions retain
their flexible Datum calling convention and remain hot-patchable.

Keep hot helper arguments type-stable. Reassigning a parameter is fine when every assignment preserves its proven native type; assigning unrelated dynamic values forces that parameter back to the dynamic path.

Host types declared with `[AuroraNativeType]` / `[AuroraExport]` use the same
direct-call idea on the C# side. Unshadowed `Math.abs(x)` with a proven number
calls `AbsCore` directly. Unshadowed `Math.PI` loads `MathSupport.PI` with
`ldsfld`. `params` members such as `Math.max` stay on the generated adapter.
Unshadowed `Conv8.getInt32(bytes, offset)` with a proven `UInt8Array` and integer
offset calls the Core method. Shadowing the global (`var Math = other`) disables
both paths.

Proven native-instance locals use the same idea. Keep a `Vec2` (or similar) in
a local that is never reassigned to an unproven value and never captured by a
closure. This applies to both `new Vec2(...)` and static factories such as
`Vec2.from(...)`. The compiler then stores the CLR instance directly; field
`++`/`+=` and method calls stay on `ldfld`/`stfld`/`callvirt` instead of boxing
through `ScriptDatum`.

A module-level `context player as Vec2;` is the same proof for
`ScriptContext.UserState`. Each used context name is loaded once per function
into its own typed local. Several names in one function are independent caches
of that same instance. Host NativeType names are valid function return
contracts. A native function declared `Vec2` (or another NativeType) returns
that CLR type from `$native`; proven callees call members directly. Only the
`$typed` shell boxes into `ScriptDatum`. The host must pass a matching
`UserState` instance; the compiler does not insert extra compatibility
conversions on the native ABI.

Use `native func` when the ABI itself must be explicit:

```as
export native func weighted(Number value, Object options) Number {
    // value remains a CLR double. The dynamic property read crosses a
    // ScriptDatum boundary only for this expression.
    return value * options.factor;
}
```

The native entry receives `ScriptContext`, so dynamic calls and module access
still work. Its body is emitted directly without per-call frame management or
an exception wrapper. On failure, runtime error conversion analyzes the CLR
exception stack and combines native method names with recorded script source
locations. Exported or escaped native functions have
a Datum-compatible closure shell for script callers. Private same-module
calls use the native entry whenever their arguments are proven compatible;
unproven calls use the shell and preserve exact parameter checks. Qualified
cross-module calls also use the imported native entry directly when its
native arguments are proven; dynamic arguments keep the exported shell path.
Native functions require a declared return type. Use `void` for a procedure
that does not return a value. Its native entry has a CLR `void` return and
native-to-native statement calls do not create or discard a `ScriptDatum`;
exported and other dynamic calls still observe `null`. Trailing defaults are supported only
when the compiler can fold them to primitive constants; an explicitly typed
parameter requires an exact matching default type. Native functions still reject
rest parameters, assignment, and all hot patches. Rebuild the module
normally when changing one.

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

## General Arrays

Use a general `Array` when elements are heterogeneous or the collection must grow. Exact local arrays and direct-call parameters use a precise `ScriptArray` path for `length`, numeric index reads/writes, compound assignments, increment/decrement, and an unshadowed `push`.

```as
var values = Array.withCapacity(count);
for (var i = 0; i < count; i++) {
    values.push(i);
}
```

Prefer `Array.withCapacity(count)` when the final capacity is known but the logical length must start at zero. `new Array(count)` creates `count` actual null slots and has different semantics. If an array crosses an ordinary object property or another dynamic boundary, subsequent access keeps full behavior but no longer has an exact compile-time array type.

## Packed Primitive Arrays

Use `Int32Array`, `Float32Array`, `Float64Array`, `Int8Array`, or `BooleanArray` for fixed-size homogeneous data instead of a general `Array`:

```as
var distances = new Int32Array(nodeCount);
var scores = new Float64Array(nodeCount);
var terrain = new Int8Array(nodeCount);
var closed = new BooleanArray(nodeCount);

for (var i = 0; i < nodeCount; i++) {
    distances[i] = i;
    closed[i] = false;
}
```

These arrays have primitive CLR backing storage and rely on CLR zero initialization. When the exact array type remains visible to flow analysis, generated code uses native `ldelem`/`stelem` instructions and keeps numeric and boolean values unboxed through the loop.

Packed-array parameters are nullable. A literal `null` is inferred from the
declared parameter type, so call `nativeWork(null)` without adding an `as`
assertion. Native-to-native calls continue to pass the raw CLR array reference.
When storage crosses an object or other dynamic boundary, the runtime restores
its script wrapper without copying and preserves wrapper identity; that work
occurs only at the boundary, not on element access.

`typeof` reports the constructor name (`"Int8Array"`, `"Float64Array"`, …). The datum `Kind` stays `Object`; do not treat `ValueKind` as the packed-array type registry.

Within a specialized direct-call graph, packed-array parameters and locals are passed as raw `int[]`, `float[]`, `double[]`, `sbyte[]`, or `bool[]` storage. Native helper-to-helper calls therefore do not reload wrapper fields or allocate replacement wrappers.

Keep the array in an exact local or pass it directly to a `native func` helper. Storing it in an ordinary object and reading it back erases the compile-time element type; access remains allocation-free apart from the array itself, but it uses the dynamic helper path and is measurably slower. This explicit boundary keeps the runtime small and predictable without speculative object-shape optimization.

Choose the narrowest type that matches the required semantics:

- `Int32Array`: signed 32-bit indexes, distances, parents, and queue tables.
- `UInt32Array`: unsigned 32-bit words for hashes, checksums, and bit-mixing kernels.
- `Float32Array`: compact IEEE-754 binary32 values; script numbers round on store.
- `Float64Array`: fractional values, `NaN`, infinities, and general script-number data.
- `Int8Array`: compact signed values in the `-128..127` range.
- `BooleanArray`: flags and visited/closed tables.

## Integer Kernels

Use signed bitwise operations and `Int32Array`/`Int8Array` values when the algorithm is naturally 32-bit. A local whose every assignment is an integer — integer literals, `int32` parameters, fields, and returns, packed-array loads, signed bitwise results, and `+`, `-`, `*`, `%` over those — keeps native CIL `int` storage for its whole lifetime and wraps on overflow. Expressions built from those locals wrap the same way, so `astarHeuristic(currentX - 1, currentY, ...)` stays on the `int` ABI. It is not conservatively widened to `double`. Division, unsigned right shift, and any fractional or `Number` assignment still produce `double`, as do the operations that need `NaN` or infinity.

This means an integer-oriented loop avoids repeated `double` conversions. The trade-off is that such a local cannot hold negative zero or `NaN`, so an integer `%` with a zero divisor raises a runtime error instead. When a `/` quotient is an exact integer, assert it with `((current - currentX) / width) as int32` (parentheses required: `as` binds tighter than `/`). Do not add `Math.floor` solely to recover an `int`; if a value genuinely needs number semantics, give it `Number` storage with a `D` suffix or a fractional assignment.

For unsigned 32-bit word kernels, declare parameters, returns, and shape fields
as `uint32`, store words in `UInt32Array`, and suffix constants with `U`/`u`
such as `0xD76AA478u`. These values use the CLR `uint` ABI, `+`, `-`, and `*`
wrap modulo 2^32, and `>>` is logical. Unsuffixed literals retain the normal
`Int32`/`Int64` inference rules.

## Console

`console.log` and `console.error` format objects and may allocate strings.
Keep them out of hot paths and benchmarks.

## Hot Patch

Disable hot reload for stable production builds unless dynamic patching is required.
Hot patch support is valuable operationally, but it can restrict some optimization choices.

## Module Graph

Use `CompilerOptions.MaxDegreeOfParallelism` to tune large module graph parsing.
The default uses processor count.

