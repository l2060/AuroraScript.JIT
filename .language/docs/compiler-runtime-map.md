# AuroraScript Compiler And Runtime Map

Use this map when changing language behavior.

## Frontend

- Lexer: `src/Compiler/Analyzer/AuroraLexer.cs`
- Parser: `src/Compiler/Analyzer/AuroraParser.cs`
- Operator table: `src/Compiler/Operator.cs`
- AST nodes: `src/Compiler/Ast`

## Backend

- Module planning: `src/Compiler/Backend/BackendCompiler.cs`
- Function binding and local scopes: `src/Compiler/Backend/Binding/FunctionBinder.cs`
- Closure capture planning: `src/Compiler/Backend/Binding/ClosurePlanner.cs`
- Const assignment checks: `src/Compiler/Backend/Analysis/ConstAssignmentAnalyzer.cs`
- Module const inlining: `src/Compiler/Backend/Analysis/ModuleConstInliningAnalyzer.cs`
- Lowering: `src/Compiler/Backend/Lowering/FunctionLowerer.cs`
- Emission: `src/Compiler/Backend/Emission`

## Runtime

- Engine entry point: `src/AuroraEngine.cs`
- Domain: `src/Runtime/ScriptDomain.cs`
- Value representation: `src/Runtime/ScriptDatum.cs`
- CIL helpers: `src/Runtime/CILHelper.cs`
- Prototypes: `src/Runtime/Types/Prototypes.cs`
- Console/JSON/Math/HotPatch: `src/Runtime/Extensions`

## Tests To Update

- Syntax and parser behavior: `tests/ParserSyntaxTests.cs`
- Lexer behavior: `tests/LexerTests.cs`
- Backend plans and diagnostics: `tests/CompilerBackendPlanTests.cs`
- Runtime language behavior: `tests/LanguageFeatureExecutionTests.cs`, `tests/StatementExecutionTests.cs`
- Module graph and build errors: `tests/ModuleCompilationTests.cs`
- Regression coverage: `tests/ReleaseRegressionTests.cs`

