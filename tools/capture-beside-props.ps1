# Renders the three capstones that stand a prop beside them with a tower on the
# tile the prop stands on, and every candidate for what the prop should do
# about it, through the real match at the framing the game is played at.
# Issue #282, question 4.
#
# THE SAME ROUTE AS capture-rung-candidates.ps1: a defense that stands the
# rows in question, a candidate art file applied to a copy of the wired art
# for the length of one run, and nothing the game ships from moving. What is
# new is the directive the candidates use -- `stand`, which moves WHERE a
# row's beside prop stands rather than what it is. UnitArtFile.cs reads it;
# BesideProp.Standing already carried an offset and nothing had ever set one.
#
# THE BOARD IS docs/frames/beside-props.txt, and its header says which cell each
# prop lands on and why two neighbours of every capstone are left free.
#
# THE TICKS ARE STILL-LIFE TICKS. A prop is a solid object on the ground, so
# any tick with the towers on screen will do; 200 and 320 are the two
# capture-rung-candidates.ps1 uses for the same kind of question, and the
# second has bodies walking past.
#
# WIDE AND CLOSE BOTH, AND THE CLOSE PASS IS ON TRIAL. #281 found that
# -Distance crops toward the middle of the corridor and its rows were in a
# corner, so its close frames were byte-identical. Two of the three clusters
# here are in the upper middle of the board; whether the close frame shows them
# is read off the pictures, and if it does not the wide frame is the one that
# decides -- which is the map's rule anyway.
#
# NOTHING HERE DECIDES ANYTHING. AGENTS.md rule 6 puts anything a player sees on
# the human side of the line. This renders the alternatives and stops.
#
# -batchmode -executeMethod, so the editor must be CLOSED. Never edit a file
# while a run is going: it forces a synchronous recompile and the run dies with
# exit 3 and no results.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$CandidateDir,
    [string]$OutDir,
    [string[]]$Only,
    [switch]$BaselineOnly,
    [switch]$WideOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

if (-not $CandidateDir) { $CandidateDir = Join-Path $repoRoot 'docs/frames/beside-props' }
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'docs/frames/beside-props' }

$defense = Join-Path $repoRoot 'docs/frames/beside-props.txt'
$ticks = '200,320'
$closeDistance = 22

$candidates = @('tucked-in', 'free-neighbour', 'no-prop')

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
    Write-Host "=== baseline ($($framing.Name)) ===" -ForegroundColor Cyan

    & $capture -Unity $Unity -OutDir $dir -Defense $defense -Ticks $ticks -Width 1600 `
        -Distance $framing.Distance -LogFile $logFile

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  FAILED with $LASTEXITCODE" -ForegroundColor Red
        $failed += "baseline ($($framing.Name))"
    }

    if ($BaselineOnly) { continue }

    foreach ($name in $candidates) {
        if ($Only -and ($Only -notcontains $name)) { continue }

        $file = Join-Path $CandidateDir "$name.txt"

        if (-not (Test-Path $file)) { throw "No candidate art at $file" }

        Write-Host ""
        Write-Host "=== $name ($($framing.Name)) ===" -ForegroundColor Cyan

        & $capture -Unity $Unity -OutDir $dir -Art $file -Defense $defense -Ticks $ticks -Width 1600 `
            -Distance $framing.Distance -LogFile $logFile

        if ($LASTEXITCODE -ne 0) {
            Write-Host "  FAILED with $LASTEXITCODE" -ForegroundColor Red
            $failed += "$name ($($framing.Name))"
        }
    }
}

Write-Host ""

if ($failed.Count -gt 0) {
    Write-Host ("these did not render: " + ($failed -join ', ')) -ForegroundColor Red
    exit 1
}

Write-Host "every candidate rendered" -ForegroundColor Green
exit 0
