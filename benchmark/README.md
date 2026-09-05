# String / numeric formatting benchmarks

Run from the repository root, in Release mode:

```powershell
dotnet run --project benchmark/Benchmark.csproj -c Release -- --string-smoke
dotnet run --project benchmark/Benchmark.csproj -c Release -- --string-compare
dotnet run --project benchmark/Benchmark.csproj -c Release -- --filter '*StringBenchmarks*' --warmupCount 3 --iterationCount 3 --iterationTime 200
```

`--string-smoke` verifies every workload's output without reporting timings.
`--string-compare` is a same-machine A/B probe: 24 warmups, nine samples in
alternating order, and 50 script invocations per sample. It reports median time
and allocations per operation, not compilation time. BenchmarkDotNet supplies
the statistical and memory-diagnostic run. Do not run benchmarks alongside tests
or builds, and do not use wall-clock thresholds as correctness assertions.

Each invocation performs 10,000 operations with runtime inputs. Host arguments
and the compiled domain are created before measurement; the returned value is
consumed. Per-operation allocation includes the amortized domain entry overhead
(approximately 0.012 B/op), so a zero-allocation search may report a tiny nonzero
number in the fast probe.

The cases cover Int32 and Number substring indices, dynamic dispatch, an ordinal
search, `trim().toLowerCase()`, the historical WordToHex implementation, and exact
Int64/UInt64 hexadecimal formatting beyond double's exact range. Eight additional
workloads compare native and dynamic literal replacement, padding, split, and
`matchAll`. The latter retains a Datum result; split and regex results necessarily
allocate their result arrays and match metadata.

Important semantic boundaries:

- `substring`'s second parameter remains an **end** index. Native exports take
  `int`; unconstrained Number indices currently fall back to the compatibility
  callback. This is a known performance regression relative to the former double
  bridge, not a speedup. No unsafe narrowing or duplicated exported bridges are used.
- String length bounds and non-negative bit masks preserve Int32 storage where
  proved safe. WordToHex's byte and literal radix now call `FormatString(int, int)`.
- Numeric formatting shares `AuroraNativeType` metadata with String. Its radix
  parameter is `int`; Int64/UInt64 receivers never pass through double. Only base
  16 selects hexadecimal, as in the existing limited Number API.
- The benchmark validates the existing unusual WordToHex output `7531`, not a
  rewritten MD5 algorithm. It does not claim to eliminate the output strings.

`PerfBenchExampleTests` now follow the example's `Env.elapsedMs()` API. Deterministic
statistics tests lexically shadow Env with a test-only clock object; a separate
integration test uses the real built-in Env. Production Env is neither replaced
nor made mutable, and timing values are not used as correctness thresholds here.

## Measurement snapshot (2026-09-05)

Windows 11, Xeon W-2235, .NET SDK 10.0.400 / runtime 10.0.11, Release.
The same A/B probe was built against an isolated archive of `4279e358` and the
working implementation. These are representative medians, not statistical
speedup guarantees; small timing changes should be treated as noise.

| Workload | `4279e358` ns/op | Current ns/op | Current B/op |
| --- | ---: | ---: | ---: |
| SubstringInt | 16.886 | 14.738 | 40.012 |
| SubstringNumber | 15.707 | 108.643 | 40.013 |
| ContainsNative | 103.907 | 8.216 | 0.012 |
| ChainNative | 212.681 | 77.981 | 96.012 |
| WordToHex | 240.001 | 235.113 | 448.012 |

The clear gains are search and chained primitive calls. WordToHex has cleaner IL,
but this measurement does not establish a meaningful speedup; its 448 B/op output
allocation is unchanged. Number substring indices regress after removing the
double bridge, as noted above. Safe guarded narrowing remains future work.

A separate successful BenchmarkDotNet ShortRun executed the original ten workloads,
without concurrent tests/builds. It measured 12.640 ns / 40 B for SubstringInt,
9.010 ns / 0 B for ContainsNative, 58.220 ns / 96 B for ChainNative, and
202.523 ns / 448 B for WordToHex. Int64/UInt64 hexadecimal formatting measured
30.792 / 29.483 ns, each allocating only its 56-byte result string. The short
three-sample run has wide confidence intervals on several timings; consult the
generated `BenchmarkDotNet.Artifacts/results` reports before drawing precise
timing conclusions.

Before the subsequent factory and PERF_BENCH fixes, String-instance regression results were:
net8 had 908 passes and 4 known PERF_BENCH failures; net9/net10 each had 1016
passes and the same 6 known PERF_BENCH failures. Language services had 150 passes.
Tests cover native signature selection, integer locals, exact 64-bit boundaries,
negative zero, culture behavior, dynamic fallbacks, and all three compilation modes.
String completion also covers regex captures, callback reentrancy, pooled argument
cleanup after exceptions, and native/dynamic result-kind parity. Tests exposed and
fixed a pre-existing Regex-versus-Function tag check that prevented regex replacement.

The final String-completion suite (including the subsequently added no-op allocation
assertion) passed 6/8/8 cases on net8/net9/net10. All 18 benchmark outputs passed smoke
validation. A separate fast probe, without concurrent test/build workloads, measured:

| New workload | Native ns/op | Dynamic ns/op | Native B/op | Dynamic B/op |
| --- | ---: | ---: | ---: | ---: |
| Literal replace | 39.132 | 199.346 | 56.012 | 56.013 |
| Pad left | 13.329 | 182.017 | 64.012 | 64.014 |
| Split | 288.429 | 338.112 | 344.012 | 344.020 |
| Match all | 924.603 | 992.696 | 896.032 | 896.032 |

These compare current native and current dynamic paths, not a historical checkout.
Result allocations are essentially identical; dispatch savings matter most for small
operations. Regex processing and result construction dominate matchAll. The no-op
literal replace and padding cores separately passed a zero-managed-allocation assertion.
These short-probe medians are not statistical speedup guarantees.

## Final factory and receiver update

The two construction workloads bring the suite to 20 cases. String construction
and static members use NativeType; `NativeReceiverType` declares primitive storage
and `AuroraExportTarget.Instance` identifies instance Core methods. The wrapper pool
and its configuration/API have been removed. Native construction reuses the raw
string; unknown receivers still permit dynamic dispatch and wrapper allocation.
Consequently, the earlier dynamic allocation snapshots above predate pool removal.
After its removal, the fast probe measured construction at 1.445 ns/op native and
157.532 ns/op dynamic, both 0.012 B/op (amortized domain entry overhead). Dynamic
contains measured 72.012 B/op versus 0.012 B/op native; native WordToHex remained
448.012 B/op. These remain short-probe observations, not timing guarantees.

Final regression: net8 passed 936 tests; net9/net10 passed 1049 each, with no
failures. Language services passed 150 tests. All 20 benchmark outputs passed the
final smoke run; the example and benchmark projects built without warnings/errors.
The old PERF_BENCH failures are resolved, retaining deterministic statistics assertions
and a separate integration check against the real built-in Env.elapsedMs().
