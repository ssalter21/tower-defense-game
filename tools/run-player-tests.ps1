<#
.SYNOPSIS
    Runs the PlayMode suite in a standalone player -- with UNITY_EDITOR undefined.

.DESCRIPTION
    THE POINT IS THE TEST COUNT, not only the pass/fail. Every fixture in
    Tests/PlayMode used to sit behind #if UNITY_EDITOR, because loading the art
    meant AssetDatabase. Built for anything but the editor, those classes
    compiled to nothing: the suite yielded ZERO tests and the run reported
    green. Tests.PlayMode.asmdef declares includePlatforms: [], so the assembly
    was always going to be built for a player -- it just had nothing in it.

    Running in the editor cannot catch that, because in the editor the #if is
    true. This is the run that can: it builds a player, which is the one place
    UNITY_EDITOR is undefined, and it FAILS IF THE SUITE REPORTS FEWER THAN
    -MinimumTests tests. A green run of nothing is exactly the failure this
    exists to make impossible.

    THE FLOOR TRACKS THE SUITE. It sits just under the number of tests there
    are, so that a fixture disappearing from a player build is caught and not
    only a suite emptied to zero -- which is what a floor far below the count
    would catch and nothing else. Raise it when the suite grows; lowering it is
    a decision about what the run stops guarding, not a formality.

    Requires the Unity Editor to be CLOSED -- batchmode needs the project lock.

.EXAMPLE
    ./tools/run-player-tests.ps1
    Build a Windows player carrying the tests, run them, and report the count.
#>
param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$Results = "$PSScriptRoot\..\player-tests.xml",
    [string]$LogFile = "$PSScriptRoot\..\player-tests.log",
    [int]$MinimumTests = 125
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'client'

if (-not (Test-Path $Unity)) { throw "Unity Editor not found at: $Unity" }

if (Test-Path -LiteralPath $Results) { Remove-Item -LiteralPath $Results -Force }

# The test framework edits PlayerSettings on its way to a test build -- it turns
# off the splash screen, turns on run-in-background, makes the window resizable
# -- and does not put them back. Those are tracked bytes, so a run that left
# them edited would hand the next person a diff nobody asked for and teach them
# to stop reading `git status`. Captured here and written back below; the editor
# has exited by then, so nothing is racing for the file.
$settings = Join-Path $project 'ProjectSettings/ProjectSettings.asset'
$settingsBefore = [System.IO.File]::ReadAllBytes($settings)

$unityArgs = @(
    '-batchmode'
    '-runTests'
    '-projectPath', "`"$project`""
    '-testPlatform', 'StandaloneWindows64'
    '-testResults', "`"$Results`""
    '-logFile', "`"$LogFile`""
)

Write-Host "building a player carrying the PlayMode suite, and running it"
$proc = Start-Process -FilePath $Unity -ArgumentList ($unityArgs -join ' ') -PassThru
$null = $proc.Handle
$proc.WaitForExit()

Write-Host "editor exited with $($proc.ExitCode)"

if (-not [System.Linq.Enumerable]::SequenceEqual($settingsBefore, [System.IO.File]::ReadAllBytes($settings))) {
    [System.IO.File]::WriteAllBytes($settings, $settingsBefore)
    Write-Host "put ProjectSettings.asset back the way the test build found it"
}

if (-not (Test-Path -LiteralPath $Results)) {
    Write-Host ""
    Write-Host "No results at $Results -- the run produced nothing at all." -ForegroundColor Red
    Write-Host "see $LogFile"
    exit 1
}

[xml]$xml = Get-Content -Raw -LiteralPath $Results
$run = $xml.'test-run'
$total = [int]$run.total
$failed = [int]$run.failed
$passed = [int]$run.passed
$skipped = [int]$run.skipped

Write-Host ""
Write-Host "with UNITY_EDITOR undefined: $total tests, $passed passed, $failed failed, $skipped skipped"

# The count check comes FIRST and is separate from the failure check, because
# the failure this script exists for reports zero of both.
if ($total -lt $MinimumTests) {
    Write-Host ""
    Write-Host "Only $total tests ran outside the editor; expected at least $MinimumTests." -ForegroundColor Red
    Write-Host "A play-mode suite that compiles to nothing outside the editor reports green" -ForegroundColor Red
    Write-Host "having asserted nothing. That is what this number is here to catch." -ForegroundColor Red
    exit 1
}

if ($failed -gt 0 -or $proc.ExitCode -ne 0) {
    Write-Host "see $Results and $LogFile" -ForegroundColor Red
    exit 1
}

Write-Host "green, on $total tests that a #if would have deleted." -ForegroundColor Green
exit 0
