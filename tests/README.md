# AuroraScript Release Test Suite

This project is the executable specification for AuroraScript compiler and runtime behavior. All engine instances created by the shared fixture use `OptimizeOptions.Release`; compilation-mode tests additionally cover `Dynamic`, `OnlyRun`, and `Persistence`.

## Test Matrix

| Area | Test file | Coverage |
|---|---|---|
| Lexer | `LexerTests.cs` | Keywords, identifiers, Unicode, every operator, numbers, strings, regex/division disambiguation, comments, CRLF locations, snapshots, long payloads, malformed tokens |
| Parser | `ParserSyntaxTests.cs` | Module metadata, imports/includes/exports, declarations, expressions, lambdas, destructuring, control flow, exceptions, templates, regex, malformed and truncated syntax, diagnostics |
| CompileBlock | `CompileBlockTests.cs` | Parameter validation, local functions, domain/no-domain invocation, module-only rejection, source names, null API input |
| Expressions | `ExpressionExecutionTests.cs` | Precedence, arithmetic, shifts, bitwise, comparison, equality, logical, unary, `typeof`, `in`, member/index access, templates, spread, assignment, increment/decrement, delete |
| Statements | `StatementExecutionTests.cs` | `if/else`, `for`, `for-in`, `while`, break/continue, closures, recursion, defaults, `$args`, high arity, spread calls, destructuring, try/catch/finally, domain isolation |
| Language behavior | `LanguageFeatureExecutionTests.cs` | Enums, expression/block lambdas, sparse arrays, object shorthand/spread, short circuit, truthiness, declared host functions, nested templates, right-associative assignment, null returns |
| Module compiler | `ModuleCompilationTests.cs` | Relative import/include, diamond deduplication, duplicate roots, wide parallel graphs, cycles, name conflicts, deterministic error aggregation, missing files, cancellation, concurrent builds, failed rebuild rollback |
| Compilation modes | `CompilationModeTests.cs` | Release behavior parity for Dynamic/OnlyRun/Persistence, persisted PE output, repeated domain creation, hot-reload on/off paths |
| Runtime API/errors | `RuntimeApiAndErrorTests.cs` | Pre-build use, missing module/method, non-function execution, script stack traces, invalid operations, const writes, `$state`, disposal |
| Built-ins | `BuiltInLibraryTests.cs` | Math, String, Array, JSON, HashMap, Regex, StringBuffer, Console output and error paths |
| Advanced types | `AdvancedRuntimeTypeTests.cs` | Constructors, static helpers, Object operations, freeze, Date, Proxy handlers and invalid proxy configuration |
| CLR interop | `ClrInteropTests.cs` | Constructor/property/field/method/static binding, global marshalling, delegates, overload/optional/params resolution, access restrictions, registry lifetime |
| Serialization | `SerializationTests.cs` | All JSON kinds, nested values, indentation, malformed JSON, circular graphs, NaN/Infinity |
| Datum model | `ScriptDatumTests.cs` | Tagged payloads, equality/coercion, CLR collection conversion, Span conversion bounds |
| Hot reload | `HotReloadTests.cs` | Disabled mode, incremental update/add, replacement removal, domain isolation |
| Concurrency | `ConcurrentRuntimeTests.cs` | Same-domain context pooling, multi-domain isolation, detached closure calls under parallel load |
| Release regressions | `ReleaseRegressionTests.cs` | Direct-call arities 0-8+, closure slot capture, deep access stack balance, logical array spread length, repeated compilation/pools, large CRLF/Unicode source, confusion mode, empty modules |
| Context lifetime | `Runtime/ClosureFunctionContextTests.cs` | Detached invocation after context return and rejection of expired pooled contexts |

## Release Validation

Run the suite only after all test generation and implementation work is complete:

```powershell
dotnet restore tests\AuroraScript.Tests.csproj --ignore-failed-sources
dotnet build tests\AuroraScript.Tests.csproj -c Release --no-restore
dotnet test tests\AuroraScript.Tests.csproj -c Release --no-build --logger "console;verbosity=normal"
```

Coverage collection:

```powershell
dotnet test tests\AuroraScript.Tests.csproj -c Release --no-build --settings tests\coverlet.runsettings --collect:"XPlat Code Coverage"
```

Release acceptance requires:

- Zero build warnings and zero failed/skipped tests.
- Line coverage at least 85% and branch coverage at least 75% for `AuroraScript.dll` after excluding generated `*.g.cs` files.
- Every compiler/runtime defect found during execution receives a focused regression test before the fix.
- No timeout in dependency discovery, cancellation, concurrent build, or concurrent runtime tests.
- Dynamic, OnlyRun, and Persistence return identical observable results for the compilation-mode corpus.

## Failure Classification

When the first full run is performed, classify failures before changing tests:

1. `AuroraLexicalException` / `AuroraParseException`: source grammar or diagnostic defect.
2. `AuroraCompileReportException`: worker aggregation, dependency discovery, or emitter defect. Inspect every inner `AuroraCompileException`.
3. `InvalidProgramException`: invalid emitted IL and a release blocker; never weaken or skip the test.
4. Wrong `ScriptDatum` kind/value: emitter stack/type conversion or runtime semantic defect.
5. Timeout/deadlock: compiler channel accounting, build serialization, context pooling, or callback lifetime defect.
6. Allocation/performance regression: measure and fix in the benchmark projects; functional tests remain deterministic and do not use timing as an assertion.

The assembly disables xUnit test parallelization because runtime string-pooling configuration is process-global. Explicit concurrency tests create and synchronize their own workloads.
