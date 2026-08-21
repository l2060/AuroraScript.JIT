# Next: keep inference simple, keep codegen native

Goal: more scripts stay on the existing typed CIL path (`int`/`double` locals, `ldelem`/`stelem`, `@directCall $native`) without a new IR, hidden-class runtime, or per-call type guards.

Rules:

- High performance, low allocation.
- Prefer “this local is exactly one packed/array type” or fall back to `Dynamic`.
- No runtime shape objects, no dual specialized methods, no deopt tables.
- Do not add A*-specific compiler logic. A* is only a stress test.

Current bottleneck is already local: `GetProperty` almost always becomes `Dynamic`, so `state.scores[i]` never uses packed element ops. Packed construction already works. Direct-call argument evidence already works for scalars and packed arrays. `tdoc Type $(value)` exists, but interpolations are currently typed as `Dynamic`.

## Do not build

- SSA / CFG rewrite
- Runtime object shapes or inline caches
- Guarded fast/slow clones of exported functions
- Tracing JIT
- Algorithm rewrites (A* to JPS, etc.)

Those would raise allocation, code size, and maintenance cost for a small extra win on top of typed locals.

## Phase 1 — trust explicit `tdoc` interpolations

Smallest compiler change. One cast at a boundary, then native access in the loop.

Today in `TypedFunctionBuilder.GetTypedDocumentFlowType`:

- `tdoc Int8Array $(x)` is `Dynamic` because interpolations are rejected.

Change:

- If `TypeName` is `Array` or a packed array name, keep that `FlowValueType` even for `$(...)`.
- Emitter already has `castclass` / binder. Do not add a second check.
- If the runtime value is the wrong type, fail as today. Do not emit a slow fallback.

Files:

- `src/Compiler/Backend/Code/TypedFunctionBuilder.cs`
- existing typed-document tests

Done when:

- `tdoc Int32Array $(value); a[i]` compiles to packed get/set, not `ObjectOps.GetElementNumber`.
- No extra allocation in the loop beyond that one entry cast.

## Phase 2 — local object-literal fields only

Compile-time side table, not a new type system.

Track `LocalSlotId -> { fieldName -> FlowValueType }` only when:

- the local is assigned a `MapExpression` / object literal
- the local is not captured
- field names are static identifiers
- field values are `Array` or packed arrays (ignore other fields)

Then `local.field` uses that field type.

Kill the whole table for that local on any of:

- reassignment
- capture / closure
- `local[index]`
- `local.field =` with a different type
- passing `local` to a non-direct call
- returning `local` from a non-direct function
- computed property names

Implementation:

- reuse `FlowValueType`
- store the table next to `_locals` in `TypedFunctionBuilder`
- no runtime data
- merge by intersection: mismatch → drop field, not `Dynamic` union of packed kinds

Files:

- `src/Compiler/Backend/Code/TypedFunctionBuilder.cs`
- `src/Compiler/Backend/Code/TypedFunctionCode.cs` only if emitter must see field types on `GetPropertyExpression` (prefer encoding the result type on the expression, which already happens)

Tests (small scripts, not A*):

```
var s = { xs: new Int32Array(8) };
s.xs[0] = 1;
return s.xs[0];
```

Must use `ldelem`/`stelem`.

```
var s = { xs: new Int32Array(8) };
take(s);
s.xs[0] = 1;
```

Must stay dynamic after `take(s)`.

## Phase 3 — same-module direct calls, arguments only

Do not invent object ABI.

`DirectCallCollector` already records argument types. After Phase 2, `finder.xs` can be packed at a same-module call:

```
work(finder.xs, n)
```

If every call site agrees, `$native` already accepts packed/`Array` parameters. No new calling convention.

Do not:

- pass whole objects as native structs
- return packed arrays through `$native` (already excluded; keep it)
- specialize exported host calls

Host/`export` arguments stay `Dynamic`. Callers that care extract fields or use `tdoc`.

## Phase 4 — keep `$native` cheap

Already started: `AggressiveInlining` on `$native` methods.

Follow-up only if it stays generic:

- keep `$native` free of `ScriptDatum` boxing on packed element access
- do not add wrapper adapters that allocate
- do not change `ScriptDatum` layout

Measure with existing integer/packed tests, not a custom A* pipeline.

## Script guidance (not compiler special cases)

Until Phase 2 lands, hot code should:

- keep buffers as packed arrays
- extract `tdoc Type $(obj.field)` once per call, not per element
- pass buffers into `@directCall` helpers instead of the whole object
- keep open-sets that may grow as `Array` (fixed packed heaps can throw)

This matches the engine: typed locals are free; object property hits are not.

## Verification

Add focused compiler tests, not a new framework:

1. `tdoc Int8Array $(x)` element load is packed.
2. Object-literal field load is packed; escape kills it.
3. Same-module `@directCall` packed argument still uses `$native`.
4. Wrong `tdoc` type still throws; no silent fallback.
5. Existing packed/integer/direct-call suites stay green.
6. Optional: one 1000×1000 A* run as a regression number, same path length and expanded count.

Pass/fail:

- hot loop allocation stays ~0 B after warmup
- no new per-element `ScriptDatum` boxes on typed packed loads
- compiler code growth stays inside `TypedFunctionBuilder` / emitter property get, not a new analysis project

## Order

1. Phase 1 (half day, highest ROI)
2. Phase 2 (1–2 days)
3. Phase 3 (only if Phase 2 tests show call-site types)
4. Phase 4 only if traces still show call overhead

Stop when typed locals cover the hot arrays. Do not add guards or shapes after that.
