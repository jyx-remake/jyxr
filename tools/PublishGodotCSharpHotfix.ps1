param(
    [string]$ProjectPath,
    [string]$ExportDataDir,
    [string]$PublishDir,
    [string]$PatchDir,
    [switch]$SkipRuntimeDescriptors,
    [switch]$IncludePdb
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path -LiteralPath (Join-Path $PSScriptRoot "..")

if ([string]::IsNullOrWhiteSpace($ProjectPath)) {
    $ProjectPath = Join-Path $repoRoot "src/Game.Godot/engine-free-rpg.csproj"
}

if ([string]::IsNullOrWhiteSpace($ExportDataDir)) {
    $ExportDataDir = Join-Path $repoRoot "export\windows\data_engine-free-rpg_windows_x86_64"
}

if ([string]::IsNullOrWhiteSpace($PublishDir)) {
    $PublishDir = Join-Path $repoRoot ".codex-temp\publish-win-x64"
}

if ([string]::IsNullOrWhiteSpace($PatchDir)) {
    $PatchDir = Join-Path $repoRoot "export\windows\patch"
}

$ProjectPath = (Resolve-Path -LiteralPath $ProjectPath).Path
$ExportDataDir = (Resolve-Path -LiteralPath $ExportDataDir).Path

if (-not (Test-Path -LiteralPath (Join-Path $ExportDataDir "hostfxr.dll"))) {
    throw "Target directory does not look like a Godot .NET export data directory: missing hostfxr.dll in $ExportDataDir"
}

if (-not (Test-Path -LiteralPath (Join-Path $ExportDataDir "hostpolicy.dll"))) {
    throw "Target directory does not look like a Godot .NET export data directory: missing hostpolicy.dll in $ExportDataDir"
}

if (Test-Path -LiteralPath $PublishDir) {
    Remove-Item -LiteralPath $PublishDir -Recurse -Force
}

if (Test-Path -LiteralPath $PatchDir) {
    Remove-Item -LiteralPath $PatchDir -Recurse -Force
}

New-Item -ItemType Directory -Path $PatchDir | Out-Null

dotnet publish $ProjectPath `
    -c ExportRelease `
    -r win-x64 `
    --self-contained true `
    -p:GodotTargetPlatform=windows `
    -p:GodotTargetName=win-x64 `
    -o $PublishDir

$runtimeConfigPath = Join-Path $PublishDir "engine-free-rpg.runtimeconfig.json"
$runtimeConfig = Get-Content -LiteralPath $runtimeConfigPath -Raw | ConvertFrom-Json

if ($null -eq $runtimeConfig.runtimeOptions.includedFrameworks) {
    throw "Publish output is not self-contained: runtimeconfig.json does not contain runtimeOptions.includedFrameworks."
}

$files = @(
    "engine-free-rpg.dll",
    "Game.Core.dll",
    "Game.Content.dll",
    "Game.Application.dll",
    "Game.Expressions.dll",
    "Game.Presentation.dll"
)

if (-not $SkipRuntimeDescriptors) {
    $files += @(
        "engine-free-rpg.deps.json",
        "engine-free-rpg.runtimeconfig.json"
    )
}

if ($IncludePdb) {
    $files += @(
        "engine-free-rpg.pdb",
        "Game.Core.pdb",
        "Game.Content.pdb",
        "Game.Application.pdb",
        "Game.Expressions.pdb",
        "Game.Presentation.pdb"
    )
}

foreach ($file in $files) {
    $source = Join-Path $PublishDir $file
    if (-not (Test-Path -LiteralPath $source)) {
        throw "Publish output is missing expected file: $source"
    }

    Copy-Item -LiteralPath $source -Destination $ExportDataDir -Force
    Copy-Item -LiteralPath $source -Destination $PatchDir -Force
    Write-Host "Copied $file"
}

$destinationRuntimeConfigPath = Join-Path $ExportDataDir "engine-free-rpg.runtimeconfig.json"
$destinationRuntimeConfig = Get-Content -LiteralPath $destinationRuntimeConfigPath -Raw | ConvertFrom-Json

if ($null -eq $destinationRuntimeConfig.runtimeOptions.includedFrameworks) {
    throw "Destination runtimeconfig.json is still not self-contained after copy."
}

Write-Host ""
Write-Host "Publish output: $PublishDir"
Write-Host "Updated export data directory: $ExportDataDir"
Write-Host "Patch files: $PatchDir"
Write-Host "Runtime framework bundled: $($destinationRuntimeConfig.runtimeOptions.includedFrameworks[0].name) $($destinationRuntimeConfig.runtimeOptions.includedFrameworks[0].version)"
