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
# WIDE ONLY, AND THAT WAS MEASURED RATHER THAN CHOSEN. Every candidate here was
# drawn at a close distance of 16 first, the way capture-effect-candidates.ps1
# draws its bracket, and the close pass answers NOTHING on this defense:
#
#   * all four Mortar close frames came back BYTE-IDENTICAL TO EACH OTHER at
#     every tick, on a candidate whose whole content is the turret's size; and
#   * the close frame is 90% empty grass.
#
# The reason is the rig rather than the distance. OrbitCameraRig frames the WHOLE
# board and -Distance moves the camera in along that same heading, so a closer
# camera crops toward the middle of the corridor -- and the five rows this
# defense stands are in the top-left CORNER of the board. The Mortar and the
# Bishop fall outside the close frame entirely, so their candidates cannot
# differ in it. A magnified picture that cannot show the thing being decided is
# worse than no picture, which is the call #280 made about the shell.
#
# THE MAGNIFIED VIEW OF THESE FOUR QUESTIONS IS A SHEET, and that is the right
# instrument anyway: a prop is a solid object standing still, which is what
# capture-armed-roster.ps1 frames one of per tile. docs/frames/roster carries
# one sheet per question at -Width 700 and the same set again at -Width 28,
# which is the size a body gets at 1600x900. So the pair issue #270 asks for is
# the sheet and the played frame, and not two cameras on one board.
#
# What would make a close frame work is pointing the camera AT a cell rather
# than standing it closer to the board's centre, which MatchFrameCapture has no
# argument for. That is a tool change and not this ticket's.
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
#   a prop      any tick with the tower on screen and bodies walking past it.
#   an anchor   needs a shot actually in the air.
#
# THESE FOUR NUMBERS WERE MEASURED AND THE FIRST FOUR TRIED WERE ALL WRONG.
# capture-match-frames.ps1 logs a per-tick line carrying the live creep count,
# the live shell count and a running total per effect, so the ticks are read off
# a cheap narrow run rather than found by opening pictures. What that run said
# about the obvious guesses -- 300 and 700, which docs/frames/four-lines.txt
# uses -- is that both are useless here:
#
#   tick 300   3 creeps, ZERO shells in the air
#   tick 700   ZERO creeps and zero shells: five long-range towers have cleared
#              the board and the frame is an empty corridor
#
# FIVE TOWERS THAT ALL REACH FOUR HEXES KILL THE WAVE FASTER THAN THE TWELVE
# SHORT-RANGE ROWS four-lines.txt STANDS, which is why its ticks do not carry
# over. Anything past about 420 on this defense is an empty board.
#
# AND A HITSCAN BOLT LIVES FOUR TICKS, NOT FIVE. The two Bishop candidates that
# differ ONLY in where the bolt leaves from were rendered at every tick from 311
# to 326: they are pixel-identical from 315 onward and differ at 311, 312, 313
# and 314. So a bolt fired in that window is on screen for four ticks and the
# window is the only place either candidate says anything at all. 311 and 313
# are the two kept.
$propTicks = '200,320'
$shotTicks = '311,313,320,340'

# candidate -> which ticks it is drawn at. There is no second framing here and
# no switch for one: see WIDE ONLY above.
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
    'mortar-turret-1.25' = @{ Ticks = $propTicks }
    'mortar-turret-1.50' = @{ Ticks = $propTicks }
    'mortar-turret-1.75' = @{ Ticks = $propTicks }
    'mortar-turret-2.00' = @{ Ticks = $propTicks }

    # ---------------------------------------------------------------
    # The Artificer: a second prop, or the other prop
    # ---------------------------------------------------------------
    #
    # The first is a still-life and takes the prop ticks. The second moves where
    # the shell leaves from, so it takes the shot ticks -- a crate is 0.46 m
    # tall against the turret's 0.77 m muzzle, and the only way to see that is a
    # shell leaving one.
    'artificer-turret-and-crate' = @{ Ticks = $propTicks }
    'artificer-crate-only'       = @{ Ticks = $shotTicks }

    # ---------------------------------------------------------------
    # The Bishop: where the tome goes, and what the bolt leaves
    # ---------------------------------------------------------------
    #
    # A HITSCAN BOLT LIVES A HANDFUL OF TICKS, which is why all four take the
    # shot ticks rather than the prop ones even though two of them are also
    # about where a prop sits. A frame with no bolt in it cannot say which of
    # these four answers the thing they were all written about.
    'bishop-tome-off-hand'          = @{ Ticks = $shotTicks }
    'bishop-tome-off-hand-anchored' = @{ Ticks = $shotTicks }
    'bishop-tome-beside'            = @{ Ticks = $shotTicks }
    'bishop-mace-off-hand'          = @{ Ticks = $shotTicks }
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
# DEDUPED, AND IT HAS TO BE. The two tick sets share 320, and asking the capture
# for one tick twice makes it write the NEXT one instead -- the first run of this
# left a stray underserved-rungs-tick-0321.png that no candidate had a partner
# for, because the view is already past 320 when the second request arrives.
$baselineTicks =
    (($propTicks + ',' + $shotTicks) -split ',' | Sort-Object { [int]$_ } -Unique) -join ','

$played = Join-Path $OutDir 'played'
New-Item -ItemType Directory -Force -Path $played | Out-Null

Write-Host ""
Write-Host "=== baseline ===" -ForegroundColor Cyan

& $capture -Unity $Unity -OutDir $played -Defense (Join-Path $repoRoot $defense) `
    -Ticks $baselineTicks -Width 1600 -LogFile $logFile

if ($LASTEXITCODE -ne 0) {
    Write-Host "  FAILED with $LASTEXITCODE" -ForegroundColor Red
    $failed += 'baseline'
}

if (-not $BaselineOnly) {
    foreach ($name in $plan.Keys) {
        if ($Only -and ($Only -notcontains $name)) { continue }

        $file = Join-Path $CandidateDir "$name.txt"

        if (-not (Test-Path $file)) { throw "No candidate art at $file" }

        Write-Host ""
        Write-Host "=== $name ===" -ForegroundColor Cyan

        & $capture -Unity $Unity -OutDir $played -Art $file `
            -Defense (Join-Path $repoRoot $defense) `
            -Ticks $plan[$name].Ticks -Width 1600 -LogFile $logFile

        if ($LASTEXITCODE -ne 0) {
            Write-Host "  FAILED with $LASTEXITCODE" -ForegroundColor Red
            $failed += $name
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
