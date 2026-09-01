[CmdletBinding()]
param(
    [string]$Source,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$projectRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$workspaceRoot = Split-Path $projectRoot -Parent
$converterRoot = Join-Path $projectRoot 'jyx-legacy-data'
$converter = Join-Path $converterRoot 'scripts\xmjh_convert.py'
$runtimeData = Join-Path $projectRoot 'mods\xmjh\data'
if ([string]::IsNullOrWhiteSpace($Source)) {
    $Source = Join-Path $workspaceRoot 'XMJH'
}

$bundledPython = Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\python\python.exe'
if (Test-Path -LiteralPath $bundledPython) {
    $pythonPath = $bundledPython
} else {
    $python = Get-Command python -ErrorAction SilentlyContinue
    if ($null -eq $python) {
        throw 'Python 3 was not found. Install Python 3 or add python.exe to PATH.'
    }
    $pythonPath = $python.Source
}

$bundledNode = Join-Path $env:USERPROFILE '.cache\codex-runtimes\codex-primary-runtime\dependencies\node\bin\node.exe'
if (Test-Path -LiteralPath $bundledNode) {
    $env:Path = (Split-Path $bundledNode -Parent) + ';' + $env:Path
} elseif ($null -eq (Get-Command node -ErrorAction SilentlyContinue)) {
    throw 'Node.js was not found. Install Node.js or add node.exe to PATH.'
}

$arguments = @(
    '-S', $converter,
    '--source', (Resolve-Path $Source).Path,
    '--runtime-data', $runtimeData
)
if ($DryRun) {
    $arguments += '--dry-run'
}

Write-Host "XMJH source : $Source"
Write-Host "Runtime data: $runtimeData"
& $pythonPath @arguments
if ($LASTEXITCODE -ne 0) {
    throw "XMJH conversion failed with exit code $LASTEXITCODE. Runtime data was not published."
}
if ($DryRun) {
    Write-Host 'XMJH conversion plan is complete; no files were generated or published.'
} else {
    Write-Host 'XMJH conversion, Game.Content validation, animation generation, and runtime publish completed.'
}
