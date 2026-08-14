<#
.SYNOPSIS
    Writes the one PanelSettings asset the chrome is cloned from.

.DESCRIPTION
    Assets/Resources/RuntimePanelSettings.asset -- the base every runtime panel
    in this project is instantiated from, and the reason a player build has the
    text engine's ICU data at all. Unity attaches that data to a PanelSettings
    in the editor and to nothing that is created at runtime, so a build with no
    such asset in it measures every string as nothing: labels come back zero by
    zero, every bar of the HUD collapses, and Player.log fills with
    UITKTextHandle null references.

    Written by the editor rather than by hand, like every other generated file
    here. What it carries is a reference into the engine's built-in resources; a
    YAML file typed out from memory would name a file id nobody can check.

    -batchmode -executeMethod, so it needs no editor session, no bridge and
    nobody at a keyboard -- and therefore requires the editor to be CLOSED,
    because batchmode needs the project lock.

.EXAMPLE
    ./tools/build-panel-settings.ps1
    Rewrite the asset and report where it landed.
#>
param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$LogFile = "$PSScriptRoot\..\build-panel-settings.log"
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'client'
$written = Join-Path $project 'Assets/Resources/RuntimePanelSettings.asset'

if (-not (Test-Path $Unity)) { throw "Unity Editor not found at: $Unity" }

$unityArgs = @(
    '-batchmode', '-quit'
    '-projectPath', "`"$project`""
    '-executeMethod', 'View.Editor.PanelSettingsAsset.Run'
    '-logFile', "`"$LogFile`""
)

Write-Host "writing the chrome's panel settings into $project"
$proc = Start-Process -FilePath $Unity -ArgumentList ($unityArgs -join ' ') -PassThru
$null = $proc.Handle
$proc.WaitForExit()

Write-Host "editor exited with $($proc.ExitCode)"

if ($proc.ExitCode -ne 0) {
    Write-Host "see $LogFile" -ForegroundColor Red
    exit $proc.ExitCode
}

# The second half of the check, for the same reason build-player.ps1 has one: a
# batchmode editor will happily exit zero having written nothing at all.
if (-not (Test-Path -LiteralPath $written -PathType Leaf)) {
    Write-Host ""
    Write-Host "The editor exited cleanly and did not write $written." -ForegroundColor Red
    Write-Host "see $LogFile"
    exit 1
}

Write-Host ""
Write-Host "wrote:" -ForegroundColor Green
Write-Host "  $written"
Write-Host "  generated -- commit it, and its .meta, beside the change that caused them"

exit 0
