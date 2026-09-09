param(
    [Parameter(Mandatory = $true)][string]$BaselineAssembly,
    [Parameter(Mandatory = $true)][string]$CandidateAssembly,
    [Parameter(Mandatory = $true)][string]$OutputDirectory,
    [string[]]$ProbeArguments = @('--compile-benchmark')
)

$ErrorActionPreference = 'Stop'
$assemblies = @{
    baseline = (Resolve-Path -LiteralPath $BaselineAssembly).Path
    candidate = (Resolve-Path -LiteralPath $CandidateAssembly).Path
}
New-Item -ItemType Directory -Force -Path $OutputDirectory | Out-Null
$output = (Resolve-Path -LiteralPath $OutputDirectory).Path
$rows = [Collections.Generic.List[string]]::new()
$rows.Add('Variant,Pair,Run,ElapsedMs,AllocatedBytes,Gen0,Gen1,Gen2')
for ($pair = 1; $pair -le 5; $pair++) {
    $order = if ($pair % 2) { @('baseline', 'candidate') } else { @('candidate', 'baseline') }
    foreach ($variant in $order) {
        $runDirectory = Join-Path $output "$pair-$variant"
        New-Item -ItemType Directory -Path $runDirectory | Out-Null
        Push-Location -LiteralPath $runDirectory
        try {
            $ErrorActionPreference = 'Continue'
            $raw = @(& dotnet $assemblies[$variant] @ProbeArguments 2>&1)
            $exitCode = $LASTEXITCODE
        } finally {
            $ErrorActionPreference = 'Stop'
            Pop-Location
        }
        $raw | Set-Content -LiteralPath (Join-Path $runDirectory 'output.txt') -Encoding UTF8
        if ($exitCode -ne 0) { throw "Probe failed: $variant pair $pair" }
        $samples = @($raw | ForEach-Object { $_.ToString() } |
            Where-Object { $_ -match '^\d+,\d+(\.\d+)?,\d+,\d+,\d+,\d+$' })
        if ($samples.Count -ne 16) { throw "Expected 16 samples: $variant pair $pair" }
        foreach ($sample in $samples) { $rows.Add("$variant,$pair,$sample") }
        [IO.File]::WriteAllLines((Join-Path $output 'full-build.csv'), $rows,
            [Text.UTF8Encoding]::new($false))
        Write-Output "Completed $variant pair $pair"
    }
}
