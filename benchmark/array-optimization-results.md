# Array NativeType optimization (2026-09-08)

## Final design

- Array keeps NativeType registration. The handwritten ArrayConstructor is replaced
  by ScriptArray's generated Type, Export constructor adapter and static exports.
- Length exports the existing CLR getter, not an extra LengthCore wrapper. Generic
  native binding passes an already-typed ScriptArray directly and returns int.
- Known Int32 and Number capacities call int/double Core overloads without packing
  arguments into ScriptDatum. Unknown arguments and dynamic calls share the Datum
  Core and its capacity clamp; exact Int64/UInt64 values retain their original path.
- Index access, push's own-member guard and evaluation ordering, mutable length,
  array construction and loop specializations remain intact. The live array
  prototype and the existing Array native-function ABI are preserved.
- No new built-in marker or optimization registry is introduced.

An initial unified candidate showed slower weak/dynamic capacity measurements.
Instead of abandoning NativeType, the final version optimizes the typed arguments
and common conversion Core, and exports the original length accessor directly.

## Runtime A/B

Windows 11, Core i7-13700KF, .NET SDK 10.0.400 / runtime 10.0.11,
BenchmarkDotNet 0.15.8, Release, workstation GC. Both snapshots run the same
benchmark assembly. Baseline is the working runtime before this Array task, not
git HEAD; its DLL SHA-256 is
`4EFBAB565368267B8F83401032B4DC2C10B4682478856DE650325FD566080B51`.

Separate processes, in-process emit toolchain, affinity mask 1, six warmups,
ten measured iterations, requested iteration time 300 ms. Compilation and setup
are excluded. The final optimized run was followed by the baseline run.

| Workload | Baseline mean | Final mean | Change | Allocated/op |
| --- | ---: | ---: | ---: | ---: |
| WithCapacityInt | 35.91 ns | 28.73 ns | -20.0% | 224 B -> 224 B |
| WithCapacityNumber | 52.23 ns | 32.05 ns | -38.6% | 224 B -> 224 B |
| WithCapacityDynamic | 89.62 ns | 82.02 ns | -8.5% | 224 B -> 224 B |

Raw reports: [baseline](results/array-baseline.csv),
[optimized](results/array-optimized.csv).

## Compiler-only controls

Both compiled script assemblies were also executed against the final runtime,
in alternating order on one thread. This measures emitted code, not runtime-version
differences. Results below are medians from that separate probe and must not be
mixed with the BenchmarkDotNet means above.

| Workload | Baseline ns/op | Final ns/op |
| --- | ---: | ---: |
| Length | 0.443 | 0.451 |
| MutableLength | 2.724 | 2.784 |
| IndexRead | 2.211 | 2.203 |
| IndexWrite | 1.461 | 1.431 |
| Push | 1.559 | 1.535 |
| WithCapacityInt | 11.900 | 9.999 |
| WithCapacityNumber | 10.658 | 10.290 |
| WithCapacityDynamic | 26.345 | 25.992 |
| Construct | 10.535 | 10.604 |

Unchanged controls fluctuate within about 2.2% in this probe; do not claim those
small differences as speedups. Allocation was identical for every case: 224.059
B/op for creation and 0.059 B/op for reused arrays, including the amortized host
invocation cost. Raw data: [paired probe](results/array-paired-il.csv).

Decoded IL comparison, including resolved member references, found identical
length, mutable-length, read, write, push, dynamic-capacity and construction bodies.
Int32/Number factory functions each lose three instructions and nine IL bytes,
including the Datum packing call and temporary store/reload. Their Array return
ABI is unchanged. Unlike a raw byte comparison, this resolves metadata tokens.

## Scope and verification

Focused tests cover all three backends, static exports, typed/weak capacities,
negative/default inputs, oversized inputs, constructor versus capacity semantics,
aliases, spreads, argument side effects, push overrides, length mutation and
native-function ABI. CLR getter export is also tested in the generator.

The measurements support retaining the unified implementation with these fast
paths. They are not a guarantee for every machine or script, nor a compilation-time
speedup claim. No full test suite was repeatedly run for this change.
