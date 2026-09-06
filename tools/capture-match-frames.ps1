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
#
# -Defense names a defense to stand on the recorded board instead of the one the
# record carries. It is for photographing a row the recorded match does not
# stand: those six towers are two types, computed by a bot's rule, so a frame of
# the record can only ever show those two firing. The map, the wave and the seed
# still come out of the record. The loader still refuses a tower off the grid,
# one inside the corridor and one whose range cannot reach the route, so a melee
# row has to be put next to the corridor rather than where an archer stood.
# Frames from such a run are named after the defense, for the reason -Units'
# are named after the table.

# -Wave names a wave to send down the recorded board instead of the one the
# record carries. It is for photographing creep rows the recorded wave does not
# send: that wave releases Minions and Skeleton Scouts, and neither carries an
# aura or a pool, so the rows that do never walk onto the board whatever roster
# or defense is standing. The rows it sends are the shipped rows with their own
# authored auras, which is the whole difference from -Units -- a fixture roster
# goes stale the moment content/units.txt moves and a fixture wave does not. The
# map, the defense and the seed still come out of the record. Frames from such a
# run are named after the wave.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$OutDir,
    [string]$Ticks,
    [string]$Units,
    [string]$Defense,
    [string]$Wave,
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

. (Join-Path $PSScriptRoot '_rendered-from.ps1')

# What is already sitting there, so the frames this run writes can be told from
# the ones it did not touch. A run captures the ticks it was asked for and says
# nothing about the others.
$before = Get-PictureWriteTimes $OutDir

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

if ($Defense) {
    $defensePath = (Resolve-Path $Defense).Path
    $unityArgs += @('-matchFrameDefense', "`"$defensePath`"")
}

if ($Wave) {
    $wavePath = (Resolve-Path $Wave).Path
    $unityArgs += @('-matchFrameWave', "`"$wavePath`"")
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

Update-RenderedFrom $OutDir (Get-WrittenPictures $OutDir $before) (Get-DrawnContentStamp $repoRoot)

Get-ChildItem -LiteralPath $OutDir -Filter '*.png' | ForEach-Object {
    Write-Host ("  {0}  {1:N0} bytes" -f $_.Name, $_.Length)
}

exit 0
