param(
    [Parameter(Mandatory = $true)][string]$InputDirectory,
    [Parameter(Mandatory = $true)][string]$OutputCsv
)

$ErrorActionPreference = 'Stop'
$rows = foreach ($file in Get-ChildItem -LiteralPath $InputDirectory -Filter '*.txt') {
    $counts = @{}
    $binds = @{}
    $analyses = @{}
    foreach ($line in [IO.File]::ReadLines($file.FullName)) {
        if ($line.StartsWith('SLIM,')) {
            $parts = $line.Split(',')
            $metric = $parts[1]
            if ($metric -eq 'Analyze') {
                $metric += '-' + $parts[5]
                $key = "$metric,$($parts[2]),$($parts[3])"
                $analyses[$key] = 1 + $analyses[$key]
            }
            if ($metric -eq 'Bind') {
                $key = "$($parts[2]),$($parts[3])"
                $binds[$key] = 1 + $binds[$key]
            }
            if ($metric -eq 'AnalysisSlots') {
                $counts['LiveFunctions'] += [long]$parts[3]
                $counts['GenericSlots'] += [long]$parts[4]
                $counts['DirectSlots'] += [long]$parts[5]
                $counts['ParameterSlots'] += [long]$parts[6]
            }
            if ($metric -eq 'EmitterSlots') {
                $counts['MethodSlots'] += [long]$parts[4]
                $counts['DirectMethodSlots'] += [long]$parts[5]
            }
            if ($metric -eq 'OverloadCandidate') { $counts['Overload-' + $parts[2]] += 1 }
            $counts[$metric] = 1 + $counts[$metric]
        } elseif ($line -match '^(\d+),\d+(\.\d+)?,\d+,\d+,\d+,\d+$') {
            [pscustomobject]@{
                Sample = $file.BaseName
                Run = [int]$Matches[1]
                Bind = [long]$counts['Bind']
                MaxBindPerFunction = ($binds.Values | Measure-Object -Maximum).Maximum
                GenericAnalyze = [long]$counts['Analyze-generic']
                DirectAnalyze = [long]$counts['Analyze-direct']
                PredictionAnalyze = [long]$counts['Analyze-prediction']
                MaxAnalyzePerFunctionMode = ($analyses.Values | Measure-Object -Maximum).Maximum
                StatementEntries = [long]$counts['StatementVisit']
                ExpressionEntries = [long]$counts['ExpressionVisit']
                CellIndex = [long]$counts['CellIndex']
                CellRoot = [long]$counts['CellRoot']
                CellScan = [long]$counts['CellScan']
                OverloadCandidates = [long]$counts['OverloadCandidate']
                AnalysisOverloadCandidates = [long]$counts['Overload-analysis']
                EmissionOverloadCandidates = [long]$counts['Overload-emission']
                LiveFunctions = [long]$counts['LiveFunctions']
                GenericSlots = [long]$counts['GenericSlots']
                DirectSlots = [long]$counts['DirectSlots']
                ParameterSlots = [long]$counts['ParameterSlots']
                MethodSlots = [long]$counts['MethodSlots']
                DirectMethodSlots = [long]$counts['DirectMethodSlots']
            }
            $counts = @{}
            $binds = @{}
            $analyses = @{}
        }
    }
}
$rows | Export-Csv -NoTypeInformation -Encoding UTF8 -LiteralPath $OutputCsv
$rows | Where-Object Run -eq 0 | Format-Table Sample,Bind,GenericAnalyze,DirectAnalyze,PredictionAnalyze,CellScan,GenericSlots
