[CmdletBinding()]
param(
    [ValidateSet('Windows', 'Android', 'Shared')]
    [string]$Target = 'Windows',
    [string]$Godot,
    [string]$WindowsTemplate,
    [string]$AndroidTemplate,
    [switch]$TestSigning
)

$ErrorActionPreference = 'Stop'
$repoRoot = Split-Path $PSScriptRoot -Parent
$projectRoot = Join-Path $repoRoot 'src/Game.Godot'
$presetPath = Join-Path $projectRoot 'export_presets.cfg'
$solutionPath = Join-Path $projectRoot 'engine-free-rpg.sln'
$exportRoot = Join-Path $repoRoot 'export'
$configPath = Join-Path $PSScriptRoot 'ExportGame.local.psd1'
$localConfig = if (Test-Path -LiteralPath $configPath) { Import-PowerShellDataFile -LiteralPath $configPath } else { @{} }
foreach ($key in $localConfig.Keys) {
    if ($key -notin @('Godot', 'WindowsTemplate', 'AndroidTemplate')) { throw "Unknown export configuration key: $key" }
    if ($localConfig[$key] -isnot [string]) { throw "Export configuration value must be a string: $key" }
}
if (-not $PSBoundParameters.ContainsKey('Godot')) { $Godot = $localConfig.Godot }
if (-not $PSBoundParameters.ContainsKey('WindowsTemplate')) { $WindowsTemplate = $localConfig.WindowsTemplate }
if (-not $PSBoundParameters.ContainsKey('AndroidTemplate')) { $AndroidTemplate = $localConfig.AndroidTemplate }
if (-not $Godot) { $Godot = 'Godot_v4.7.2-stable_mono_win64.exe' }
if (($Target -eq 'Windows' -and -not $WindowsTemplate) -or ($Target -eq 'Android' -and -not $AndroidTemplate)) {
    throw "Configure the $Target template in $configPath (see ExportGame.example.psd1), or pass -${Target}Template."
}
if ($TestSigning -and $Target -ne 'Android') { throw '-TestSigning only applies to Android.' }
$godotCommand = (Get-Command $Godot -ErrorAction Stop).Source
$presetBytes = [IO.File]::ReadAllBytes($presetPath)
$presetText = [IO.File]::ReadAllText($presetPath)
$createdSolution = $false
$signingVariables = @('GODOT_ANDROID_KEYSTORE_RELEASE_PATH', 'GODOT_ANDROID_KEYSTORE_RELEASE_USER', 'GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD')
$savedEnvironment = @{}
foreach ($name in $signingVariables) { $savedEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process') }

function Set-PresetOption([string]$Section, [string]$Key, [string]$Value) {
    $sectionPattern = '(?ms)(^\[' + [regex]::Escape($Section) + '\]\r?\n)(.*?)(?=^\[|\z)'
    $keyPattern = '(?m)^' + [regex]::Escape($Key) + '=.*$'
    if (-not [regex]::IsMatch($script:presetText, $sectionPattern)) { throw "Missing preset section: $Section" }
    $script:presetText = [regex]::Replace($script:presetText, $sectionPattern, [System.Text.RegularExpressions.MatchEvaluator]{ param($match)
        if (-not [regex]::IsMatch($match.Groups[2].Value, $keyPattern)) { throw "Missing preset option: $Key" }
        $body = [regex]::Replace($match.Groups[2].Value, $keyPattern, [System.Text.RegularExpressions.MatchEvaluator]{ param($option) "$Key=$Value" })
        $match.Groups[1].Value + $body
    })
}

try {
    New-Item -ItemType Directory -Force $exportRoot | Out-Null
    $extraArguments = @()
    switch ($Target) {
        'Windows' {
            $templatePath = (Resolve-Path -LiteralPath $WindowsTemplate).Path.Replace('\', '/')
            Set-PresetOption 'preset.0.options' 'custom_template/release' ('"' + $templatePath + '"')
            $presetName = 'Windows Base'
            $outputPath = Join-Path $exportRoot 'windows/金庸群侠传XR.exe'
        }
        'Android' {
            $templatePath = (Resolve-Path -LiteralPath $AndroidTemplate).Path.Replace('\', '/')
            $buildDirectory = (Join-Path $exportRoot 'android-build').Replace('\', '/')
            Set-PresetOption 'preset.2.options' 'gradle_build/android_source_template' ('"' + $templatePath + '"')
            Set-PresetOption 'preset.2.options' 'gradle_build/gradle_build_directory' ('"' + $buildDirectory + '"')
            Set-PresetOption 'preset.2.options' 'package/signed' 'true'
            $extraArguments = @('--install-android-build-template')
            $presetName = 'Android Base'
            $outputPath = Join-Path $exportRoot 'android/JYXR.apk'
            if ($TestSigning) {
                $keyPath = Join-Path $exportRoot 'android/jyxr-test.keystore'
                New-Item -ItemType Directory -Force (Split-Path $keyPath -Parent) | Out-Null
                if (-not (Test-Path -LiteralPath $keyPath)) {
                    & keytool -genkeypair -keystore $keyPath -storepass android -keypass android -alias androiddebugkey -dname 'CN=JYXR Test,O=JYXR,C=CN' -keyalg RSA -keysize 2048 -validity 10000 -noprompt
                    if ($LASTEXITCODE -ne 0) { throw 'Test keystore generation failed.' }
                }
                $env:GODOT_ANDROID_KEYSTORE_RELEASE_PATH = $keyPath
                $env:GODOT_ANDROID_KEYSTORE_RELEASE_USER = 'androiddebugkey'
                $env:GODOT_ANDROID_KEYSTORE_RELEASE_PASSWORD = 'android'
            }
            foreach ($name in $signingVariables) {
                if (-not [Environment]::GetEnvironmentVariable($name, 'Process')) {
                    throw "Set $name for release signing, or explicitly use -TestSigning."
                }
            }
            if (-not (Test-Path -LiteralPath $env:GODOT_ANDROID_KEYSTORE_RELEASE_PATH -PathType Leaf)) { throw 'Release keystore does not exist.' }
        }
        'Shared' {
            $presetName = 'Shared Resources PCK'
            $outputPath = Join-Path $exportRoot 'shared/base.pck'
        }
    }
    New-Item -ItemType Directory -Force (Split-Path $outputPath -Parent) | Out-Null
    if ($Target -ne 'Shared' -and -not (Test-Path -LiteralPath $solutionPath)) {
        $createdSolution = $true
        & dotnet new sln --name engine-free-rpg --format sln --output $projectRoot
        if ($LASTEXITCODE -ne 0) { throw 'Temporary solution creation failed.' }
        & dotnet sln $solutionPath add (Join-Path $projectRoot 'engine-free-rpg.csproj')
        if ($LASTEXITCODE -ne 0) { throw 'Adding the host project to the temporary solution failed.' }
    }
    [IO.File]::WriteAllText($presetPath, $presetText, [Text.UTF8Encoding]::new($false))
    $logPath = Join-Path $exportRoot ($Target.ToLowerInvariant() + '-export.log')
    $exportMode = if ($Target -eq 'Shared') { '--export-pack' } else { '--export-release' }
    $startedAt = [DateTime]::UtcNow
    $errorLogPath = Join-Path $exportRoot ($Target.ToLowerInvariant() + '-export.stderr.log')
    $arguments = @('--headless', '--path', $projectRoot) + $extraArguments + @($exportMode, $presetName, $outputPath)
    $quotedArguments = ($arguments | ForEach-Object { '"' + $_ + '"' }) -join ' '
    $process = Start-Process -FilePath $godotCommand -ArgumentList $quotedArguments -WindowStyle Hidden -PassThru -RedirectStandardOutput $logPath -RedirectStandardError $errorLogPath
    # Wait for the exporter itself, not its persistent Gradle daemon descendants.
    $process.WaitForExit()
    $exitCode = $process.ExitCode
    [IO.File]::AppendAllText($logPath, [IO.File]::ReadAllText($errorLogPath))
    $logText = [IO.File]::ReadAllText($logPath)
    # Godot 4.7.2 may report this editor shutdown error even after a successful export.
    $exportErrors = @($logText -split '\r?\n' | Where-Object { $_ -match '^ERROR:' -and $_ -notmatch '^ERROR: EditorSettings not instantiated yet when getting setting "export/android/(shutdown_adb_on_exit|android_sdk_path)"\.' })
    if ($exitCode -ne 0 -or $exportErrors.Count -gt 0) { throw "Export failed. See $logPath`n$($exportErrors -join "`n")" }
    if (-not (Test-Path -LiteralPath $outputPath) -or (Get-Item -LiteralPath $outputPath).LastWriteTimeUtc -lt $startedAt) {
        throw "Export did not produce a fresh artifact: $outputPath"
    }
    Write-Host "Export complete: $outputPath"
    Write-Host "Log: $logPath"
}
finally {
    [IO.File]::WriteAllBytes($presetPath, $presetBytes)
    if ($createdSolution -and (Test-Path -LiteralPath $solutionPath)) { Remove-Item -LiteralPath $solutionPath }
    foreach ($name in $signingVariables) { [Environment]::SetEnvironmentVariable($name, $savedEnvironment[$name], 'Process') }
}
