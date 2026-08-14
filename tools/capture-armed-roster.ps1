# Renders every unit in the roster holding what it holds, one PNG each.
#
# WHY THIS IS NOT capture-match-frames.ps1. That one photographs the recorded
# match, and the recorded match is one defense: content/defense.txt puts four
# archers and two mages on the board and nothing else. The Soldier, the
# Skeleton and the Skeleton Warrior are units the game can draw that no frame
# of that match will ever contain -- which is how the Soldier's sword went
# unreviewed. Changing the defense to get a photograph would re-freeze every
# committed golden for the sake of a picture.
#
# THE PNGs ARE DOCUMENTATION, NOT AN ORACLE. Nothing compares them to anything
# and nothing fails if they change. What catches a broken view is the
# assertions in ImportedArtTests and MatchViewTests. What this is for is
# letting a human see what a unit is holding.
#
# It draws through the real TowerView and CreepView with the art the scene is
# wired from, so what comes out is what the match draws.
#
# NOTHING IT WRITES IS COMMITTED. The default output is docs/frames/roster,
# which docs/frames/.gitignore already excludes, so running this leaves no
# untracked PNGs for the build gate's tree-clean step to trip over. Regenerate
# it when you want to look; it is not documentation anybody has to keep.
#
# -batchmode -executeMethod, so it needs no editor session, no bridge and
# nobody at a keyboard -- and therefore requires the editor to be CLOSED,
# because batchmode needs the project lock.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$OutDir,
    [int]$Width = 700,
    [string]$LogFile = "$PSScriptRoot\..\capture-armed-roster.log"
)

$ErrorActionPreference = 'Stop'

$project = (Resolve-Path "$PSScriptRoot\..\client").Path

if (-not (Test-Path $Unity)) { throw "Unity Editor not found at: $Unity" }

$unityArgs = @(
    '-batchmode', '-quit',
    '-projectPath', "`"$project`"",
    '-executeMethod', 'View.Editor.ArmedRosterCapture.Run',
    '-logFile', "`"$LogFile`"",
    '-rosterWidth', $Width
)

if ($OutDir) { $unityArgs += @('-rosterOutDir', "`"$OutDir`"") }

Write-Host "drawing the armed roster from $project"

# Start-Process plus an explicit WaitForExit is what actually blocks on a
# GUI-subsystem executable and what actually yields its exit code. `& $Unity`
# returns in milliseconds and reports whatever ran before it.
$proc = Start-Process -FilePath $Unity -ArgumentList ($unityArgs -join ' ') -PassThru
$null = $proc.Handle
$proc.WaitForExit()

Write-Host "editor exited with $($proc.ExitCode)"

if ($proc.ExitCode -ne 0) {
    Write-Host "see $LogFile" -ForegroundColor Red
    exit $proc.ExitCode
}

Select-String -Path $LogFile -Pattern '^\[roster\]' | ForEach-Object { "  " + $_.Line.Trim() }
