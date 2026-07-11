[CmdletBinding()]
param(
    [string[]]$SourceRoots = @(
        (Join-Path $PSScriptRoot '..\src'),
        (Join-Path $PSScriptRoot '..\test')
    )
)

$resolvedRoots = $SourceRoots | ForEach-Object { (Resolve-Path -LiteralPath $_).Path }
$ignoredSegments = @('.godot', 'bin', 'obj')

function Test-IgnoredPath([string]$Path) {
    $segments = $Path -split '[\\/]'
    return $segments | Where-Object { $ignoredSegments -contains $_ }
}

$scripts = $resolvedRoots | ForEach-Object {
    Get-ChildItem -LiteralPath $_ -Recurse -File -Filter '*.cs'
} |
    Where-Object { -not (Test-IgnoredPath $_.FullName) }
$uids = $resolvedRoots | ForEach-Object {
    Get-ChildItem -LiteralPath $_ -Recurse -File -Filter '*.cs.uid'
} |
    Where-Object { -not (Test-IgnoredPath $_.FullName) }

$missingUids = $scripts |
    Where-Object { -not (Test-Path -LiteralPath ($_.FullName + '.uid')) }
$orphanUids = $uids |
    Where-Object { -not (Test-Path -LiteralPath ($_.FullName -replace '\.uid$', '')) }

if ($missingUids.Count -eq 0 -and $orphanUids.Count -eq 0) {
    Write-Host 'Godot C# UID files are complete and paired.'
    exit 0
}

if ($missingUids.Count -gt 0) {
    Write-Error ("Missing .uid files:`n" + ($missingUids.FullName -join "`n"))
}

if ($orphanUids.Count -gt 0) {
    Write-Error ("Orphan .uid files:`n" + ($orphanUids.FullName -join "`n"))
}

exit 1
