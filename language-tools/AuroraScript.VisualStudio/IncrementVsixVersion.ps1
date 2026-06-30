param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath
)

$ErrorActionPreference = 'Stop'

if (-not (Test-Path -LiteralPath $ManifestPath)) {
    throw "VSIX manifest not found: $ManifestPath"
}

$manifest = New-Object System.Xml.XmlDocument
$manifest.PreserveWhitespace = $true
$manifest.Load($ManifestPath)

$namespaceManager = New-Object System.Xml.XmlNamespaceManager($manifest.NameTable)
$namespaceManager.AddNamespace('vsix', 'http://schemas.microsoft.com/developer/vsx-schema/2011')

$identity = $manifest.SelectSingleNode('/vsix:PackageManifest/vsix:Metadata/vsix:Identity', $namespaceManager)
if ($null -eq $identity) {
    throw "VSIX manifest Identity node was not found: $ManifestPath"
}

$versionText = $identity.GetAttribute('Version')
if ([string]::IsNullOrWhiteSpace($versionText)) {
    throw "VSIX manifest Identity Version is empty: $ManifestPath"
}

$parts = $versionText.Split('.')
if ($parts.Length -lt 1 -or $parts.Length -gt 4) {
    throw "VSIX version must contain 1 to 4 numeric parts: $versionText"
}

$numbers = New-Object int[] $parts.Length
for ($i = 0; $i -lt $parts.Length; $i++) {
    $number = 0
    if (-not [int]::TryParse($parts[$i], [System.Globalization.NumberStyles]::None, [System.Globalization.CultureInfo]::InvariantCulture, [ref]$number)) {
        throw "VSIX version contains a non-numeric part: $versionText"
    }

    $numbers[$i] = $number
}

$numbers[$numbers.Length - 1]++
$newVersion = ($numbers | ForEach-Object { $_.ToString([System.Globalization.CultureInfo]::InvariantCulture) }) -join '.'

$identity.SetAttribute('Version', $newVersion)

$settings = New-Object System.Xml.XmlWriterSettings
$settings.Encoding = New-Object System.Text.UTF8Encoding -ArgumentList $false
$settings.Indent = $false
$settings.NewLineHandling = [System.Xml.NewLineHandling]::None

$writer = [System.Xml.XmlWriter]::Create($ManifestPath, $settings)
try {
    $manifest.Save($writer)
}
finally {
    $writer.Dispose()
}

Write-Host "AuroraScript VSIX version: $versionText -> $newVersion"
