# AuroraScript Compiler And Runtime Map

Use this map when changing language behavior.

## Frontend

- Lexer: `src/Compiler/Analyzer/AuroraLexer.cs`
- Parser: `src/Compiler/Analyzer/AuroraParser.cs`
- Resolved source identity: `src/Core/ScriptSourceReference.cs`
- Module graph resolution/linking: `src/Compiler/ScriptCompiler.cs`
- Operator table: `src/Compiler/Operator.cs`
- AST nodes: `src/Compiler/Ast`

`@module(NAME);` is optional explicit-name metadata used by host lookup APIs and script `global.getModule`. Module graph identity is normalized `ScriptSourceReference.FullPath`; anonymous modules have no filename-derived name, and only explicit names participate in name-conflict checks.

## Backend

- Module planning: `src/Compiler/Backend/BackendCompiler.cs`
- Function binding and local scopes: `src/Compiler/Backend/Binding/FunctionBinder.cs`
- Closure capture planning: `src/Compiler/Backend/Binding/ClosurePlanner.cs`
- Const assignment checks: `src/Compiler/Backend/Analysis/ConstAssignmentAnalyzer.cs`
- Module const inlining: `src/Compiler/Backend/Analysis/ModuleConstInliningAnalyzer.cs`
- Typed flow analysis: `src/Compiler/Backend/Code/TypedFunctionBuilder.cs`, `src/Compiler/Backend/Code/TypedModuleCode.cs`
- Typed CIL emission: `src/Compiler/Backend/Emission/TypedCilEmitter.cs`
- Native-object locals, field mutation, and direct CLR calls: `src/Compiler/Backend/Emission/TypedCilEmitter.NativeObjects.cs`
- Generated host-export catalog and direct calls: `src/Compiler/Backend/HostExportCatalog.cs`, `src/Compiler/Backend/Emission/TypedCilEmitter.cs`
- Hosting source generator (packed as an analyzer inside `AuroraScript.JIT`): `language-tools/AuroraScript.Hosting.Generators`
- Module and domain initialization: `src/Compiler/Backend/Emission/ModuleInitializerEmitter.cs`, `src/Compiler/Backend/Emission/BackendBuildEmitter.cs`

## Runtime

- Engine entry point: `src/AuroraEngine.cs`
- Domain: `src/Runtime/ScriptDomain.cs`
- Path-keyed module registry and explicit-name lookup: `src/Runtime/ScriptGlobal.cs`
- Runtime module metadata: `src/Runtime/ScriptModule.cs`
- Compact value representation: `src/Runtime/ScriptDatum.cs`
- Storage tags (not the script type-name registry): `src/Runtime/ValueKind.cs`
- Interned `typeof` strings: `src/Runtime/TypeNames.cs`
- Object identity for `typeof` / `GetTypeName`: `ScriptObject.TypeOfValue` in `src/Runtime/Types/ScriptObject.cs`
- Exact `check TypeName` assertions: `src/Runtime/TypeCheckOps.cs`
- Packed-array wrapper restore at object/dynamic boundaries: `src/Runtime/PackedArrayBoundaryOps.cs`
- Primitive dynamic boundaries: `src/Runtime/ValueOps.cs`
- Object and collection boundaries: `src/Runtime/ObjectOps.cs`, `src/Runtime/IterationOps.cs`
- Calls and lightweight frames: `src/Runtime/CallOps.cs`, `src/Runtime/CallFrameOps.cs`
- Scope and exception boundaries: `src/Runtime/ScopeOps.cs`, `src/Runtime/ExceptionOps.cs`
- Prototypes: `src/Runtime/Types/Prototypes.cs`
- Console/JSON/TDoc/Math/HotPatch (NativeType): `src/Runtime/Builtin`
- NativeType source generator: `language-tools/AuroraScript.Hosting.Generators/AuroraNativeObjectGenerator.cs`
- Host export attributes: `src/Hosting` (`AuroraNativeTypeAttribute`, `AuroraExportAttribute`)
- Compiler native-type catalog: `src/Compiler/Backend/HostExportCatalog.cs`; application types come from `CompilerOptions.NativeTypes`
- Hot-patch graph and path matching: `src/Compiler/IncrementalCompiler.cs`, `src/Compiler/Backend/Emission/HotPatchEmitter.cs`

## Tests To Update

- Syntax and parser behavior: `tests/AuroraScript.Tests/ParserSyntaxTests.cs`
- Lexer behavior: `tests/AuroraScript.Tests/LexerTests.cs`
- Backend plans and diagnostics: `tests/AuroraScript.Tests/CompilerBackendPlanTests.cs`
- Runtime language behavior: `tests/AuroraScript.Tests/LanguageFeatureExecutionTests.cs`, `tests/AuroraScript.Tests/StatementExecutionTests.cs`
- `typeof` and native object names: `tests/AuroraScript.Tests/ExpressionExecutionTests.cs`, `tests/AuroraScript.Tests/PackedArrayTests.cs`
- Exact `check` assertions: `tests/AuroraScript.Tests/TypeCheckTests.cs`
- Module graph and build errors: `tests/AuroraScript.Tests/ModuleCompilationTests.cs`
- Regression coverage: `tests/AuroraScript.Tests/ReleaseRegressionTests.cs`, `tests/AuroraScript.Tests/CompilationModeTests.cs`
- Host export generator and diagnostics: `tests/AuroraScript.Tests/HostExportGeneratorTests.cs`, `tests/AuroraScript.Tests/AuroraExportGeneratorDiagnosticTests.cs`
- Native-object direct calls and CLR locals: `tests/AuroraScript.Tests/NativeObjectDirectCallTests.cs`

