# Renders the committed board once per scenery preset, so a dressing can be
# chosen by looking at pictures instead of by reading numbers.
#
# EVERY FRAME IS THE SAME BOARD. The map, the corridor and the tier of every
# cell come from content/map.txt and no preset touches any of it, so anything
# that differs between two of these pictures is dressing and nothing that
# differs is the playfield. The match's result, its landmark table and its
# per-tick hash are identical under all of them.
#
# TWO ANGLES EACH, and the low one is the point: a ledge judged only from the
# shipped overhead pitch is judged from the angle least able to show it.
#
# -batchmode -executeMethod, so it needs no editor session and nobody at a
# keyboard -- and therefore requires the editor to be CLOSED, because batchmode
# needs the project lock.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$OutDir,
    [string]$Names,
    [int]$Width = 1600,
    [string]$LogFile = "$PSScriptRoot\..\capture-prototypes.log"
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'client'

if (-not (Test-Path $Unity)) { throw "Unity Editor not found at: $Unity" }
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'docs/prototypes/scenery' }

$unityArgs = @(
    '-batchmode', '-quit'
    '-projectPath', "`"$project`""
    '-executeMethod', 'View.Editor.PrototypeCapture.Run'
    '-prototypeOut', "`"$OutDir`""
    '-prototypeWidth', $Width
    '-logFile', "`"$LogFile`""
)

if ($Names) { $unityArgs += @('-prototypeNames', $Names) }

# Start-Process plus an explicit WaitForExit is what actually blocks on a
# GUI-subsystem executable and what actually yields its exit code. `& $Unity`
# returns in milliseconds and reports whatever ran before it.
Write-Host "capturing scenery prototypes from $project into $OutDir"
$proc = Start-Process -FilePath $Unity -ArgumentList ($unityArgs -join ' ') -PassThru
$null = $proc.Handle
$proc.WaitForExit()

Write-Host "editor exited with $($proc.ExitCode)"
