# Compilation optimization results (2026-09-07)

## Scope and method

Baseline: `68aea1d`, built from a detached worktree. Both versions used the same
Examples executable, script snapshot and settings: Release, Persistence,
HotReload enabled, module-constant inlining enabled, and DLL output enabled.
The existing local change to `examples/tests/builtin.as` was preserved in both
workloads. The source directory contained 16 .as files totaling 63,136 bytes.
SDK 10.0.400, runtime 10.0.11, Windows x64.

The final A/B run used a separate temporary output directory, swapping only
AuroraScript.dll between processes. Five process pairs alternated execution
order. Each process performed 16 complete builds; run 0 is the first build
after engine initialization and runs 8-15 supply 40 warm samples per version.
Timings include discovery, parsing, analysis, emission, PE serialization, disk
writing and assembly loading. Script execution is excluded. Allocations use
GC.GetTotalAllocatedBytes(true), including worker threads.

## End-to-end results

| Metric | Baseline | Optimized | Result |
| --- | ---: | ---: | --- |
| Warm build median | 730.382 ms | 628.611 ms | 13.9% less time |
| Warm allocated bytes, median | 205,938,272 | 16,861,032 | 91.8% less allocation |
| Warm Gen0 collections, 40 builds | 560 | 45 | 92.0% fewer |
| Warm Gen2 collections, 40 builds | 38 | 3 | 92.1% fewer |
| First-build median, 5 processes | 1,703.463 ms | 1,805.183 ms | No demonstrated improvement |
| First-build allocated bytes, median | 206,818,576 | 17,784,200 | 91.4% less allocation |

There was substantial machine-wide timing variability. An earlier five-process
batch measured warm medians of 473.512 -> 363.695 ms and first-build medians of
1,392.727 -> 1,112.038 ms. The later isolated first-build result reversed that
direction (+6.0%), so **a reliable cold-start latency improvement is not claimed**.
These are measured samples, not a latency guarantee or proof of statistical
significance. Allocation reductions were consistent across the batches.

## Retained changes

1. Reuse branch shape snapshots within each TypeAnalyzer, bounded by live branch
   nesting rather than branch count times fixed-point passes. Buffers are ordinary
   managed arrays owned by that analysis, not a global pool. Nested branches keep
   separate live snapshots; merging rules and convergence limits are unchanged.
   Diagnostic AStar type-analysis allocation fell from about 180.53 MB to 2.74 MB.
2. Memoize whole-function syntactic write checks per local slot within the same
   analyzer, using a lazily allocated sbyte array. This avoids repeated whole-AST
   scans when checking native length bounds. The AST/bindings do not change during
   these passes. An immediate diagnostic comparison reduced warm MD5 type-analysis
   time from about 143 ms to 70 ms; this is not an end-to-end timing claim.
3. Pass already-read source text to the lexer in both full and incremental parsing.
   This removes the second read at these call sites, without caching FileSource
   across builds or changing import resolution. Parsing/linking allocation in the
   diagnostic probe fell by about 0.38 MB per build.
4. Filter native method candidates by name/return type before materializing export
   attributes. This saved approximately 0.50 MB per full example build. Attribute,
   overload and receiver validation remain intact.

No optimization level, native ABI, direct-call behavior, parallelism setting or
deployment mode was reduced. No new compiler stage, global metadata cache,
incremental graph cache, or AST pool was introduced. Broader metadata-sharing and
inference-scheduling changes were deferred after measurement identified the much
larger scratch-allocation and repeated-traversal costs.

## Verification

- .NET 10 core tests: 1,091 passed.
- Language services tests: 152 passed; language server tests: 26 passed.
- Regression tests cover one source read per full/incremental parse, fresh reads
  across builds, and nested if/try/catch/finally snapshots in all three backends.
- Isolated PE comparison: all 237 non-domain-initializer method bodies were
  byte-identical, with matching signatures, local-signature handles, maximum
  stack and exception regions. InitializeDomain had the same length and 68
  differing bytes corresponding to 17 process-salted path hashes. Module MVIDs
  also differ across builds; whole-file hashes are not a determinism assertion.
- net8.0 and net9.0 builds succeeded. Runtime tests on those versions were not
  executed because only the .NET 10 runtime is installed.

Reproduction commands and probe semantics are in [README.md](README.md).
