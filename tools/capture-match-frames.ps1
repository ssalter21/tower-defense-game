# Renders the match at chosen ticks to PNGs, so it can be looked at without
# anybody opening the editor.
#
# THE FRAMES ARE DOCUMENTATION, NOT AN ORACLE. Nothing compares them to
# anything and nothing fails if they change -- that call was made on this
# project after two frames whose bones were definitively swapped rendered
# pixel-identical, reproducibly. What catches a broken view is the assertions
# in MatchViewTests and the sit-down landmark table. What this is for is
# letting a human see the match at a named tick.
#
# It draws through the real MatchRoot, the real floor, the real camera rig
# pointed where -Yaw and -Distance say and the real MatchView stepping the real
# simulation, because a capture path that built its own approximation of the
# scene would be a picture of something this project does not ship.
#
# -batchmode -executeMethod, so it needs no editor session, no bridge and
# nobody at a keyboard -- and therefore requires the editor to be CLOSED,
# because batchmode needs the project lock.

# -Units names a unit table to play the recorded board, defense, wave and seed
# against instead of the shipped one. It is for photographing something no
# shipped row does: every row of content/units.txt authors no bubble, so nothing
# in the recorded match is ever slowed or shielded. Frames from such a run are
# named after the table rather than after the match, because the tick in a
# match-tick- filename is a claim about the run content/landmarks.txt was made
# from.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$OutDir,
    [string]$Ticks,
    [string]$Units,
    [float]$Yaw = 0,
    [float]$Distance = 0,
    [int]$Width = 1280,
    [string]$LogFile = "$PSScriptRoot\..\capture-match-frames.log"
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'client'

if (-not (Test-Path $Unity)) { throw "Unity Editor not found at: $Unity" }
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'docs/frames' }

$unityArgs = @(
    '-batchmode', '-quit'
    '-projectPath', "`"$project`""
    '-executeMethod', 'View.Editor.MatchFrameCapture.Run'
    '-matchFrameOut', "`"$OutDir`""
    '-matchFrameYaw', $Yaw.ToString([Globalization.CultureInfo]::InvariantCulture)
    '-matchFrameDistance', $Distance.ToString([Globalization.CultureInfo]::InvariantCulture)
    '-matchFrameWidth', $Width
    '-logFile', "`"$LogFile`""
)

if ($Ticks) { $unityArgs += @('-matchFrameTicks', $Ticks) }

if ($Units) {
    $unitsPath = (Resolve-Path $Units).Path
    $unityArgs += @('-matchFrameUnits', "`"$unitsPath`"")
}

# Start-Process plus an explicit WaitForExit is what actually blocks on a
# GUI-subsystem executable and what actually yields its exit code. `& $Unity`
# returns in milliseconds and reports whatever ran before it.
Write-Host "capturing match frames from $project into $OutDir"
$proc = Start-Process -FilePath $Unity -ArgumentList ($unityArgs -join ' ') -PassThru
$null = $proc.Handle
$proc.WaitForExit()

Write-Host "editor exited with $($proc.ExitCode)"

if ($proc.ExitCode -ne 0) {
    Write-Host "see $LogFile" -ForegroundColor Red
    exit $proc.ExitCode
}

Get-ChildItem -LiteralPath $OutDir -Filter '*.png' | ForEach-Object {
    Write-Host ("  {0}  {1:N0} bytes" -f $_.Name, $_.Length)
}

exit 0
