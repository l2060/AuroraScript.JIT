# ScriptDatum numeric optimization

## Scope

The 16-byte representation, public signatures, NaN normalization, signed-zero
bits, subnormals, numeric coercions and exact Int64/UInt64 semantics are preserved.
The compiler fix deliberately changes incorrect negative-zero constant emission
when module constant inlining is enabled.

- Integer Number factories bypass NaN/subnormal checks and only special-case zero.
- Double encoding combines the three reserved raw patterns into one range check.
- Decoding checks the contiguous escape range before handling special values.
- Number truthiness compares encoded values without decoding to double.
- Numeric coercions have small Number fast paths separated from other kinds.
- Equality caches kinds and compares exact signed/unsigned integers before coercion.
- No layout changes, unsafe numeric construction, wrapper-cache changes or public
  calling-convention changes were introduced.

## Measurement

BenchmarkDotNet 0.15.8, .NET 10.0.11 x64 RyuJIT, Intel Core i7-13700KF,
Windows 11. One launch, three warmup iterations, five measured iterations of
200 ms. Baseline and optimized runs used the same benchmark source and inputs.

Each invocation processes 1,024 runtime-loaded values. Factory benchmarks include
writing the result into a preallocated ScriptDatum array; these are amortized
throughput measurements, not isolated call latency. Integer inputs include zeros
and signed random Int32 values; the long workload widens those inputs. Double
inputs also include negative zero, NaN and the smallest positive subnormal.

| Workload | Baseline ns/value | Optimized ns/value | Time reduction |
| --- | ---: | ---: | ---: |
| Int32 Number creation + store | 0.8795 | 0.6234 | 29.1% |
| UInt32 Number creation + store | 0.9119 | 0.6523 | 28.5% |
| Int64-to-Number creation + store | 0.8765 | 0.5819 | 33.6% |
| Double creation + store | 0.9032 | 0.8931 | Approximately unchanged |
| Number reading | 0.5377 | 0.5346 | Approximately unchanged |
| Number truthiness | 1.7883 | 1.0885 | 39.1% |
| TryToNumber on Number values | 0.8133 | 0.6142 | 24.5% |
| Exact signed/unsigned equality | 4.6902 | 2.3605 | 49.7% |

All measured workloads allocate zero managed bytes per operation. Disassembly
confirms FromNumber/CreateNumber are inlined: the optimized Int32 loop performs a
zero test, integer-to-double conversion, bit transfer and two field stores, with
no numeric factory calls or NaN/subnormal checks. Its total code shrank from 199
to 105 bytes; double creation shrank from 202 to 122 bytes and reading from 159 to
113 bytes.

These short, single-machine runs establish a local direction, not a general
application speedup. Mixed-type conversions, other CPUs and runtimes may behave
differently. The smallest timing differences should not be treated as gains.

Raw reports and assembly are in `results/datum-baseline/results/` and
`results/datum-optimized/results/`.

```powershell
dotnet run -c Release --project benchmark/Benchmark.csproj -- --filter '*ScriptDatumBenchmarks*' --job short --warmupCount 3 --iterationCount 5 --iterationTime 200 --launchCount 1 --artifacts benchmark/results/datum-optimized
```

## Semantic verification

- Special IEEE-754 patterns plus 10,000 deterministic random bit patterns test
  factories, overwriting reference-valued destinations, decoding, truthiness,
  conversion and NaN behavior.
- Integer boundary tests include values beyond the exact double integer range.
- A legacy equality oracle covers cross-kind comparisons in both directions.
- Negative-zero tests run with constant inlining both enabled and disabled in
  Dynamic, OnlyRun and Persistence modes. With the old long-emission condition,
  all three enabled-inlining cases fail; with the fix all six pass.
- The Release .NET 10 runtime test suite passes all 1,144 tests.
