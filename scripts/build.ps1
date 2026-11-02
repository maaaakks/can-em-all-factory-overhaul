[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$GameDir,
    [string]$Python = "python",
    [string]$DotNet = "dotnet",
    [ValidateSet("Debug", "Release")][string]$Configuration = "Release",
    [switch]$SkipGeometry
)
$ErrorActionPreference = "Stop"
$repoRoot = Split-Path -Parent $PSScriptRoot
$localGame = (Resolve-Path -LiteralPath $GameDir).Path
$project = Join-Path $repoRoot "src/PermanentUpgradesRevamped/PermanentUpgradesRevamped.csproj"
$geometry = Join-Path $repoRoot "src/PermanentUpgradesRevamped/FloorGeometry.json"
if (-not $SkipGeometry) {
    & $Python (Join-Path $repoRoot "tools/export_floor_geometry.py") --game-dir $localGame --output $geometry --force
    if ($LASTEXITCODE -ne 0) { throw "Local geometry generation failed." }
} elseif (-not (Test-Path -LiteralPath $geometry)) {
    throw "No local geometry snapshot exists. Build once without -SkipGeometry."
}
& $DotNet build $project -c $Configuration "-p:GameDir=$localGame"
if ($LASTEXITCODE -ne 0) { throw "Mod build failed." }
Write-Output "Local build complete. No game/save files were modified or installed."
