# Renders every candidate look in docs/frames/rung-candidates/ through the real
# match, at the framing the game is played at -- and, for the ones a magnified
# frame says anything useful about, a second time with the camera closer.
#
# WHY THIS IS NOT capture-effect-candidates.ps1. That one moves an effect's
# colour, size and duration, which are numbers in MatchTuning. These move what a
# row is HOLDING -- its props, the thing standing beside it, and where its shots
# leave from -- which is bound in MatchSceneBuilder's own table. The two are the
# same shape of question asked of two different halves of the art, and they take
# the same route: a candidate file, applied to a copy of the wired art for the
# length of one run, with nothing in the file the game ships from moving. See
# UnitArtFile.cs and capture-match-frames.ps1's -Art.
#
# WHAT THESE ARE FOR. docs/roster.md signs four rungs with a look the shipped
# bindings do not deliver, and each was reported rather than fixed because each
# fix is a number or a look a player sees -- issue #281:
#
#   the Mortar     "a heavier turret_base" against a collection with one turret
#   the Artificer  a turret AND a crate, against one beside socket
#   the Elder      colour and no prop, against a page that says tier 2 is both
#   the Bishop     a magic bolt leaving the head of a blunt weapon
#
# THE ELDER IS NOT HERE, AND THAT IS DELIBERATE. His question is WHICH PROP, and
# the space is every prop his own pack ships that is not already another line's
# identity -- forty-five of them. Forty-five batchmode runs to compare props
# that occupy a few dozen pixels each is the wrong instrument; the sheet
# docs/frames/roster/elder-prop-sheet.png puts all forty-six tiles up at once,
# and its -Width 28 pass is what says whether an off-hand prop reads at play
# size at all. If that pass says it does, a short bracket belongs here.
#
# WHY TWICE. Issue #270 established that a sheet and the built player disagree --
# a slowed body reads plainly magnified and not at all at 1600x900, which is the
# size a person actually plays at. So the wide frame is the one that decides and
# the close one is there to say what the shape is made of. Both are 1600 pixels
# across; the magnification is the camera moving in rather than the picture
# getting bigger.
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
    [switch]$WideOnly,
    [switch]$CloseOnly,
    [switch]$BaselineOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

if (-not $CandidateDir) { $CandidateDir = Join-Path $repoRoot 'docs/frames/rung-candidates' }
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'docs/frames/rung-candidates' }

# THE ONE CONTEXT, and it is a defense rather than a wave. None of the four
# rungs is on the recorded board -- content/defense.txt stands four archers and
# two mages, computed by a bot's rule -- so a frame of the record can never show
# one. docs/frames/underserved-rungs.txt stands all five rows this map is about,
# including the tier-1 Engineer, who is not a question but is what the Mortar's
# turret has to be heavier THAN.
$defense = 'docs/frames/underserved-rungs.txt'

# THE TICKS. Two of these questions are about a thing standing still and two are
# about a thing in flight, so they do not want the same moment:
#
#   a prop      any tick with the tower on screen shows it; 300 and 700 are the
#               two docs/frames/four-lines.txt already uses, so a reader can put
#               these beside a frame of the same board without a candidate on.
#   an anchor   needs a shot actually in the air. The Bishop is hitscan and the
#               Artificer is a 45-tick projectile, so a shell is visible for far
#               longer than a bolt. capture-match-frames.ps1 logs a per-tick
#               line with the live shell count, so the tick to name is read off
#               a cheap narrow run rather than found by opening pictures --
#               which is how the ticks below were arrived at, and they are
#               re-read whenever the defense moves.
$propTicks = '300,700'
$shotTicks = '300,320,340,700'

# candidate -> which ticks, and how far the close frame stands back. A Close of
# zero draws no close frame at all.
$plan = [ordered]@{
    # ---------------------------------------------------------------
    # The Mortar: what "a heavier turret_base" can mean
    # ---------------------------------------------------------------
    #
    # A LADDER DERIVED FROM THE TILE. turret_base is 1.00 x 1.13 on the ground
    # at *1, on tiles 2.0 m across the flats, so *2.00 is where the prop exactly
    # fills its tile and anything larger overhangs the corridor. Nobody picked a
    # value; the tile picked the ceiling and the four are that range in steps.
    #
    # THE CLOSE FRAME EARNS ITS PLACE HERE, unlike on most of #280's bracket:
    # what is being compared is the SIZE of a solid object standing on the
    # ground, which is the one thing magnification shows honestly.
    'mortar-turret-1.25' = @{ Ticks = $propTicks; Close = 16 }
    'mortar-turret-1.50' = @{ Ticks = $propTicks; Close = 16 }
    'mortar-turret-1.75' = @{ Ticks = $propTicks; Close = 16 }
    'mortar-turret-2.00' = @{ Ticks = $propTicks; Close = 16 }

    # ---------------------------------------------------------------
    # The Artificer: a second prop, or the other prop
    # ---------------------------------------------------------------
    #
    # The first is a still-life and takes the prop ticks. The second moves where
    # the shell leaves from, so it takes the shot ticks -- a crate is 0.46 m
    # tall against the turret's 0.77 m muzzle, and the only way to see that is a
    # shell leaving one.
    'artificer-turret-and-crate' = @{ Ticks = $propTicks; Close = 16 }
    'artificer-crate-only'       = @{ Ticks = $shotTicks; Close = 16 }

    # ---------------------------------------------------------------
    # The Bishop: where the tome goes, and what the bolt leaves
    # ---------------------------------------------------------------
    #
    # A HITSCAN BOLT LIVES A HANDFUL OF TICKS, which is why all four take the
    # shot ticks rather than the prop ones even though two of them are also
    # about where a prop sits. A frame with no bolt in it cannot say which of
    # these four answers the thing they were all written about.
    'bishop-tome-off-hand'          = @{ Ticks = $shotTicks; Close = 16 }
    'bishop-tome-off-hand-anchored' = @{ Ticks = $shotTicks; Close = 16 }
    'bishop-tome-beside'            = @{ Ticks = $shotTicks; Close = 16 }
    'bishop-mace-off-hand'          = @{ Ticks = $shotTicks; Close = 16 }
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$capture = Join-Path $PSScriptRoot 'capture-match-frames.ps1'
$logFile = Join-Path $repoRoot 'capture-rung-candidates.log'
$failed = @()

# THE BASELINE FIRST, AND IT IS NOT A CANDIDATE. The same five rows on the same
# board with nothing moved, at both framings and both tick sets. Every candidate
# below is read against it, and without it a reader cannot tell a candidate that
# changed something from one whose file was misspelt -- which is the failure
# this whole route is built to avoid. Its frames are named after the defense
# rather than after a candidate, which is capture-match-frames.ps1's own rule.
$baselines = @(
    @{ Suffix = 'played'; Distance = 0;  Width = 1600; Ticks = $shotTicks },
    @{ Suffix = 'close';  Distance = 16; Width = 1600; Ticks = $shotTicks }
)

foreach ($framing in $baselines) {
    if ($WideOnly -and $framing.Suffix -eq 'close') { continue }
    if ($CloseOnly -and $framing.Suffix -eq 'played') { continue }

    $into = Join-Path $OutDir $framing.Suffix
    New-Item -ItemType Directory -Force -Path $into | Out-Null

    Write-Host ""
    Write-Host "=== baseline ($($framing.Suffix), distance $($framing.Distance)) ===" -ForegroundColor Cyan

    & $capture -Unity $Unity -OutDir $into -Defense (Join-Path $repoRoot $defense) `
        -Ticks $framing.Ticks -Width $framing.Width -Distance $framing.Distance -LogFile $logFile

    if ($LASTEXITCODE -ne 0) {
        Write-Host "  FAILED with $LASTEXITCODE" -ForegroundColor Red
        $failed += "baseline/$($framing.Suffix)"
    }
}

if (-not $BaselineOnly) {
    foreach ($name in $plan.Keys) {
        if ($Only -and ($Only -notcontains $name)) { continue }

        $file = Join-Path $CandidateDir "$name.txt"

        if (-not (Test-Path $file)) { throw "No candidate art at $file" }

        $entry = $plan[$name]

        $framings = @()
        if (-not $CloseOnly) { $framings += @{ Suffix = 'played'; Distance = 0; Width = 1600 } }
        if ((-not $WideOnly) -and $entry.Close -gt 0) {
            $framings += @{ Suffix = 'close'; Distance = $entry.Close; Width = 1600 }
        }

        foreach ($framing in $framings) {
            $into = Join-Path $OutDir $framing.Suffix
            New-Item -ItemType Directory -Force -Path $into | Out-Null

            Write-Host ""
            Write-Host "=== $name ($($framing.Suffix), distance $($framing.Distance)) ===" -ForegroundColor Cyan

            & $capture -Unity $Unity -OutDir $into -Art $file `
                -Defense (Join-Path $repoRoot $defense) `
                -Ticks $entry.Ticks -Width $framing.Width -Distance $framing.Distance `
                -LogFile $logFile

            if ($LASTEXITCODE -ne 0) {
                Write-Host "  FAILED with $LASTEXITCODE" -ForegroundColor Red
                $failed += "$name/$($framing.Suffix)"
            }
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
