# Renders the board today and the two board-smoothing candidates of issue #329,
# each candidate bare and under each legibility aid, from the match camera and
# from a plan view, and stitches each camera's nine frames into one sheet.
#
# EVERY FRAME IS THE SAME BOARD. The cells, the corridor and the level of every
# cell come from content/map.txt and nothing here touches any of it, so anything
# that differs between two frames is the ground's surface and nothing that
# differs is the playfield. The trace, the landmark table and the golden do not
# move.
#
# THE RENDER IS CHECKED AGAINST ITSELF. Beside each sheet goes check-<camera>.png,
# a grid of crops around the corridor's steepest cell, one per frame at twice the
# size, so whether the level change is visible in every frame is answered by a
# picture. smoothing.txt names the cell and holds the cost of each candidate,
# counted off the meshes rather than typed.
#
# -batchmode -executeMethod, so it needs no editor session and nobody at a
# keyboard -- and therefore requires the editor to be CLOSED, because batchmode
# needs the project lock.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$OutDir,
    [int]$Width = 1600,
    [string]$LogFile = "$PSScriptRoot\..\capture-smoothing-sheet.log"
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'client'

if (-not (Test-Path $Unity)) { throw "Unity Editor not found at: $Unity" }

# Forced absolute, because the editor resolves a relative path against the Unity
# project and not the repository root.
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'docs/prototypes/smoothing' }
if (-not [System.IO.Path]::IsPathRooted($OutDir)) { $OutDir = Join-Path (Get-Location).Path $OutDir }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

. (Join-Path $PSScriptRoot '_rendered-from.ps1')

$before = Get-PictureWriteTimes $OutDir

$unityArgs = @(
    '-batchmode', '-quit'
    '-projectPath', "`"$project`""
    '-executeMethod', 'View.Editor.SmoothingSheetCapture.Run'
    '-smoothingOut', "`"$OutDir`""
    '-smoothingWidth', $Width
    '-logFile', "`"$LogFile`""
)

# Start-Process plus an explicit WaitForExit is what actually blocks on a
# GUI-subsystem executable and what actually yields its exit code. `& $Unity`
# returns in milliseconds and reports whatever ran before it.
Write-Host "capturing the smoothing sheet from $project into $OutDir"
$proc = Start-Process -FilePath $Unity -ArgumentList ($unityArgs -join ' ') -PassThru
$null = $proc.Handle
$proc.WaitForExit()

Write-Host "editor exited with $($proc.ExitCode)"

if ($proc.ExitCode -ne 0) {
    Write-Host "see $LogFile" -ForegroundColor Red
    exit $proc.ExitCode
}

Update-RenderedFrom $OutDir (Get-WrittenPictures $OutDir $before) (Get-DrawnContentStamp $repoRoot)

Get-ChildItem -LiteralPath $OutDir -Filter '*.png' | ForEach-Object {
    Write-Host ("  {0}  {1:N0} bytes" -f $_.Name, $_.Length)
}

exit 0
