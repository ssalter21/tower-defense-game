# Renders the board as it ships, bare, from the match camera and from a plan
# view, and a check beside them: the corridor's steepest cell cropped out of
# each frame at twice the size, so whether a change of level is visible is
# answered by a picture rather than by a claim.
#
# THE BOARD IS THE ONE THE GAME BUILDS. The real MatchRoot draws the real
# floor -- the skin, the contours, the road pieces, the dressing the asset
# ships -- from content/map.txt read through the simulation's own parser, with
# nothing standing on it and nobody walking it. The cells, the corridor and
# the level of every cell come from the map and nothing here touches any of
# them.
#
# THE SECOND CAMERA IS A PLAN. tools/render-map.ps1 draws the board as a
# top-down SVG and has no Unity camera to borrow, so its view is taken here as
# an orthographic camera looking straight down, which is the angle at which
# height is invisible and the contour has to carry the whole of the reading.
#
# -batchmode -executeMethod, so it needs no editor session and nobody at a
# keyboard -- and therefore requires the editor to be CLOSED, because batchmode
# needs the project lock.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$OutDir,
    [int]$Width = 1600,
    [string]$LogFile = "$PSScriptRoot\..\capture-board.log"
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'client'

if (-not (Test-Path $Unity)) { throw "Unity Editor not found at: $Unity" }

# Forced absolute, because the editor resolves a relative path against the Unity
# project and not the repository root.
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'docs/frames/board' }
if (-not [System.IO.Path]::IsPathRooted($OutDir)) { $OutDir = Join-Path (Get-Location).Path $OutDir }
New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

. (Join-Path $PSScriptRoot '_rendered-from.ps1')

$before = Get-PictureWriteTimes $OutDir

$unityArgs = @(
    '-batchmode', '-quit'
    '-projectPath', "`"$project`""
    '-executeMethod', 'View.Editor.BoardFrameCapture.Run'
    '-boardOut', "`"$OutDir`""
    '-boardWidth', $Width
    '-logFile', "`"$LogFile`""
)

# Start-Process plus an explicit WaitForExit is what actually blocks on a
# GUI-subsystem executable and what actually yields its exit code. `& $Unity`
# returns in milliseconds and reports whatever ran before it.
Write-Host "capturing the board from $project into $OutDir"
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
