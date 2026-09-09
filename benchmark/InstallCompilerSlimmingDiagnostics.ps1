param([Parameter(Mandatory = $true)][string]$Checkout)

$ErrorActionPreference = 'Stop'
$root = (Resolve-Path -LiteralPath $Checkout).Path
$workspace = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
if ($root.TrimEnd('\', '/') -eq $workspace.TrimEnd('\', '/')) {
    throw 'Install diagnostic instrumentation only in an isolated checkout.'
}

function Insert-Diagnostic([string]$file, [string]$needle, [string]$statement) {
    $path = Join-Path $root $file
    $source = [IO.File]::ReadAllText($path)
    $index = $source.IndexOf($needle, [StringComparison]::Ordinal)
    if ($index -lt 0 -or $source.IndexOf($needle, $index + $needle.Length, [StringComparison]::Ordinal) -ge 0) {
        throw "Expected one diagnostic insertion point: $file / $needle"
    }
    $source = $source.Insert($index, $statement + [Environment]::NewLine)
    [IO.File]::WriteAllText($path, $source, [Text.UTF8Encoding]::new($false))
}

function Insert-BodyDiagnostic([string]$file, [string]$signature, [string]$statement) {
    $path = Join-Path $root $file
    $source = [IO.File]::ReadAllText($path)
    $matches = [regex]::Matches($source, [regex]::Escape($signature) + '\s*\{')
    if ($matches.Count -ne 1) { throw "Expected one method: $file / $signature" }
    $source = $source.Insert($matches[0].Index + $matches[0].Length, [Environment]::NewLine + $statement)
    [IO.File]::WriteAllText($path, $source, [Text.UTF8Encoding]::new($false))
}

$builder = 'src/Compiler/Backend/Code/TypedFunctionBuilder.cs'
Insert-Diagnostic $builder '            var binder = new NameBinder(module, function, directFunctions);' `
    '            System.Console.WriteLine($"SLIM,Bind,{module.Name},{function.Id.Value},{function.Name}");'
Insert-Diagnostic $builder '            var analyzer = new TypeAnalyzer(' `
    '            System.Console.WriteLine($"SLIM,Analyze,{binding.Module.Name},{binding.Function.Id.Value},{binding.Function.Name},{(callableReturnPrediction != null ? "prediction" : parameterTypes != null ? "direct" : "generic")}");'
Insert-BodyDiagnostic $builder 'private void AnalyzeStatement(Statement statement)' `
    '                System.Console.WriteLine($"SLIM,StatementVisit,{_module.Name},{_function.Id.Value}");'
Insert-BodyDiagnostic $builder 'private FlowValueType AnalyzeExpression(Expression expression)' `
    '                System.Console.WriteLine($"SLIM,ExpressionVisit,{_module.Name},{_function.Id.Value}");'

$cells = 'src/Compiler/Backend/Code/CapturedCellTypes.cs'
Insert-Diagnostic $cells '            var plans = new Dictionary<int, FunctionPlan>();' `
    '            System.Console.WriteLine($"SLIM,CellIndex,{module.Name},{module.Functions.Count}");'
Insert-Diagnostic $cells '            for (var depth = 0; depth < 64; depth++)' `
    '            System.Console.WriteLine($"SLIM,CellRoot,{slot.SourceFunction.Value},{slot.Id.Value}");'
Insert-BodyDiagnostic $cells 'public void Scan(AstNode node)' `
    '                System.Console.WriteLine($"SLIM,CellScan,{_function.Id.Value}");'

$emitter = 'src/Compiler/Backend/Emission/TypedCilEmitter.cs'
Insert-Diagnostic $emitter '            _functionsByDeclaration = new Dictionary<FunctionDeclaration, FunctionPlan>(ReferenceEqualityComparer.Instance);' `
    '            System.Console.WriteLine($"SLIM,EmitterSlots,{module.Name},{module.Functions.Count},{_methods.Length},{_directMethods.Length}");'

$module = 'src/Compiler/Backend/Code/TypedModuleCode.cs'
Insert-Diagnostic $module '            var generic = new TypedFunctionCode[size];' `
    '            System.Console.WriteLine($"SLIM,AnalysisSlots,{module.Name},{module.Functions.Count},{size},{size},{size}");'

foreach ($file in @($builder, $emitter, 'src/Compiler/Backend/Emission/TypedCilEmitter.NativeObjects.cs',
    'src/Compiler/Backend/Code/HostExportArgumentFacts.cs', 'src/Compiler/Backend/HostExportCatalog.NativeObjects.cs')) {
    $path = Join-Path $root $file
    $source = [IO.File]::ReadAllText($path)
    $source = [regex]::Replace($source, 'for\s*\([^\r\n]*candidate\.NextOverload\)\s*\{',
        '$0' + [Environment]::NewLine + '                System.Console.WriteLine("SLIM,OverloadCandidate," + (System.Environment.StackTrace.Contains("TypedFunctionBuilder") ? "analysis" : "emission"));')
    [IO.File]::WriteAllText($path, $source, [Text.UTF8Encoding]::new($false))
}

Write-Output "Diagnostic sources installed in $root. Do not use this build for timing or allocation comparisons."
