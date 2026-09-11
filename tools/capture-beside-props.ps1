# Renders the three capstones that stand a prop beside them with a tower on the
# tile the prop asks for, through the real match at the framing the game is
# played at -- so the rule that moves a prop to a free neighbour
# (View/BesideStanding.cs) is seen doing it on a board built to make it.
# Issue #282, question 4, signed by #284 on 11 September 2026.
#
# THE BOARD IS docs/frames/beside-props.txt, and its header says which cell each
# prop asks for, why a Mage stands on it, and why two neighbours of every
# capstone are left free: with none free the rule has nowhere to go and stands
# the prop inside its tower's own hex, which is the fallback and not the point.
#
# THIS RENDERED CANDIDATES UNTIL #284 SIGNED. Three art files -- tucked-in,
# free-neighbour, no-prop -- spelled the alternatives through the `stand`
# directive, and the sitting chose the free neighbour over the other two. The
# files came out with the choice; the directive stays, because moving where a
# row's prop stands is a thing an art file may still want to say. What this
# draws now is the baseline, which is the rule as built.
#
# THE TICKS ARE STILL-LIFE TICKS. A prop is a solid object on the ground, so
# any tick with the towers on screen will do; 200 and 320 are the two
# capture-rung-candidates.ps1 uses for the same kind of question, and the
# second has bodies walking past.
#
# WIDE AND CLOSE BOTH. The wide frame is the framing the game is played at and
# the one that decides; a prop moving one tile changes about a tenth of one
# per cent of it, which is why the close pass (-Distance 22) is kept: the three
# clusters stand mid-board, inside the crop, with the prop legible.
#
# -batchmode -executeMethod, so the editor must be CLOSED. Never edit a file
# while a run is going: it forces a synchronous recompile and the run dies with
# exit 3 and no results.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$OutDir,
    [switch]$WideOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'docs/frames/beside-props' }

$defense = Join-Path $repoRoot 'docs/frames/beside-props.txt'
$ticks = '200,320'
$closeDistance = 22

$capture = Join-Path $PSScriptRoot 'capture-match-frames.ps1'
$logFile = Join-Path $repoRoot 'capture-beside-props.log'
$failed = @()

$played = Join-Path $OutDir 'played'
New-Item -ItemType Directory -Force -Path $played | Out-Null

$framings = @(@{ Name = 'wide'; Distance = 0 })
if (-not $WideOnly) { $framings += @{ Name = 'close'; Distance = $closeDistance } }

foreach ($framing in $framings) {
    $dir = Join-Path $played $framing.Name
    New-Item -ItemType Directory -Force -Path $dir | Out-Null

    Write-Host ""
    Write-Host "=== beside-props ($($framing.Name)) ===" -ForegroundColor Cyan

    & $capture -Unity $Unity -OutDir $dir -Defense $defense -Ticks $ticks -Width 1600 `
        -Distance $framing.Distance -LogFile $logFile

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  FAILED with $LASTEXITCODE" -ForegroundColor Red
        $failed += "beside-props ($($framing.Name))"
    }
}

Write-Host ""

if ($failed.Count -gt 0) {
    Write-Host ("these did not render: " + ($failed -join ', ')) -ForegroundColor Red
    exit 1
}

Write-Host "every framing rendered" -ForegroundColor Green
exit 0
