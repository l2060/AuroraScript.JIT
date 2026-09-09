param(
    [Parameter(Mandatory = $true)][string]$BaselineJson,
    [Parameter(Mandatory = $true)][string]$CandidateJson,
    [Parameter(Mandatory = $true)][string]$OutputCsv
)

$ErrorActionPreference = 'Stop'
$baseline = @((Get-Content -LiteralPath $BaselineJson -Raw | ConvertFrom-Json) | Sort-Object Method,ILBytes)
$candidate = @((Get-Content -LiteralPath $CandidateJson -Raw | ConvertFrom-Json) | Sort-Object Method,ILBytes)
if ($baseline.Count -ne $candidate.Count) { throw 'Method counts differ; inspect the full JSON manifests.' }

function Normalize-Instructions($method) {
    $instructions = @($method.Instructions)
    for ($index = 0; $index -lt $instructions.Count; $index++) {
        $instruction = $instructions[$index] -replace
            'D:/SourceCode/AuroraScript.JIT/\.git/slimming-validation/(baseline-examples|candidate-(final|verified)-examples)/tests', '<scripts>'
        # Only the module registration hash is process-dependent. Do not mask ordinary integer operands.
        if ($method.Method.StartsWith('AuroraScriptInitializer::') -and $instruction.StartsWith('ldc.i4 ') -and
            $index + 7 -lt $instructions.Count -and
            $instructions[$index + 7] -like '*ScriptGlobal::Void RegisterModule(Int32,*') {
            $instruction = 'ldc.i4 <path-hash>'
        }
        $instruction
    }
}

$rows = for ($index = 0; $index -lt $baseline.Count; $index++) {
    $left = $baseline[$index]
    $right = $candidate[$index]
    if ($left.Method -cne $right.Method) { throw "Signature mismatch at method $index" }
    $sameInstructions = (($left.Instructions -join "`n") -ceq ($right.Instructions -join "`n"))
    $normalized = ((@(Normalize-Instructions $left) -join "`n") -ceq (@(Normalize-Instructions $right) -join "`n"))
    [pscustomobject]@{
        Method = $left.Method
        BaselineIL = $left.ILBytes
        CandidateIL = $right.ILBytes
        BaselineLocals = $left.Locals.Count
        CandidateLocals = $right.Locals.Count
        SameInstructions = $sameInstructions
        SameNormalizedInstructions = $normalized
        SameLocals = (($left.Locals -join "`n") -ceq ($right.Locals -join "`n"))
        SameInitLocals = $left.InitLocals -eq $right.InitLocals
        SameMaxStack = $left.MaxStackSize -eq $right.MaxStackSize
        SameExceptions = ((ConvertTo-Json -InputObject $left.Exceptions -Depth 5 -Compress) -ceq
            (ConvertTo-Json -InputObject $right.Exceptions -Depth 5 -Compress))
    }
}
$rows | Export-Csv -NoTypeInformation -Encoding UTF8 -LiteralPath $OutputCsv
$rows | Where-Object { !$_.SameNormalizedInstructions -or !$_.SameLocals -or !$_.SameExceptions } | Format-Table -AutoSize
