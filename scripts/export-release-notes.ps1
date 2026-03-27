[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [string]$ChangelogPath = (Join-Path (Split-Path -Parent $PSScriptRoot) "CHANGELOG.md"),

    [string]$OutputPath
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ChangelogPath)) {
    throw "CHANGELOG file not found at '$ChangelogPath'."
}

$normalizedVersion = $Version.Trim()
if ($normalizedVersion.StartsWith("v", [System.StringComparison]::OrdinalIgnoreCase)) {
    $normalizedVersion = $normalizedVersion.Substring(1)
}

$content = Get-Content -Raw -Path $ChangelogPath
$escapedVersion = [regex]::Escape($normalizedVersion)
$pattern = "(?ms)^## \[$escapedVersion\][^\r\n]*\r?\n(?<body>.*?)(?=^## \[|\z)"
$match = [regex]::Match($content, $pattern)

if (-not $match.Success) {
    throw "Release notes for version '$normalizedVersion' were not found in '$ChangelogPath'."
}

$heading = ([regex]::Match($content, "(?m)^## \[$escapedVersion\][^\r\n]*")).Value.TrimEnd()
$body = $match.Groups["body"].Value.TrimEnd()
$releaseNotes = @"
$heading

$body
"@.TrimEnd() + "`r`n"

if ([string]::IsNullOrWhiteSpace($OutputPath)) {
    Write-Output $releaseNotes
    return
}

$parentDirectory = Split-Path -Parent $OutputPath
if (-not [string]::IsNullOrWhiteSpace($parentDirectory) -and -not (Test-Path $parentDirectory)) {
    New-Item -ItemType Directory -Path $parentDirectory | Out-Null
}

Set-Content -Path $OutputPath -Value $releaseNotes -Encoding UTF8
Write-Host "Release notes exported to $OutputPath"
