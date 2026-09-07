# Renders every candidate effect look in docs/frames/effect-candidates/ through
# the real match, at the framing the game is played at -- and, for the ones a
# magnified frame says anything useful about, a second time with the camera
# three times closer.
#
# WHY TWICE. Issue #270 established that a sheet and the built player disagree --
# a slowed body reads plainly magnified and not at all at 1600x900, which is the
# size a person actually plays at. So the wide frame is the one that decides and
# the close one is there to say what the shape is made of. Both are 1600 pixels
# across; the magnification is the camera moving in rather than the picture
# getting bigger, because a 4,800-pixel PNG of the same composition is the same
# picture in more bytes and this repository has a five-megabyte ceiling on one.
#
# NOTHING HERE DECIDES ANYTHING. Every value a candidate names is declared a
# placeholder in MatchTuning's own header, and AGENTS.md rule 6 puts anything a
# player sees on the human side of the line. This renders the alternatives and
# stops.
#
# THE CONTEXT PER CANDIDATE IS IN THIS FILE AND NOT IN THE CANDIDATE. Which wave
# walks and which defense stands changes the simulation, so it changes which
# tick a pulse lands on; a candidate file says what the effect looks like and
# says nothing about which match shows it. The table below is the second half.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$CandidateDir,
    [string]$OutDir,
    [string[]]$Only,
    [switch]$WideOnly,
    [switch]$CloseOnly
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

if (-not $CandidateDir) { $CandidateDir = Join-Path $repoRoot 'docs/frames/effect-candidates' }
if (-not $OutDir) { $OutDir = Join-Path $repoRoot 'docs/frames/effect-candidates' }

# The two contexts. A candidate that needs a Shield Wall to slow something needs
# a defense that stands one, and a candidate that needs a Necromancer's pool
# needs a wave that sends one -- neither is in the recorded match.
$contexts = @{
    # The recorded defense -- Archers and Mages, so a splash lands and leaves a
    # blast circle -- with the six aura-carrying creep rows walking at it.
    'auras' = @{
        Wave    = 'docs/frames/creep-auras.txt'
        Defense = $null
    }
    # Twelve rows of the Knight, Barbarian, Paladin and Engineer lines, so a
    # Shield Wall slows and a Blessing reaches, with the same wave walking.
    'lines' = @{
        Wave    = 'docs/frames/creep-auras.txt'
        Defense = 'docs/frames/four-lines.txt'
    }
    # The same defense against a wave that sends a Skeleton Mage and two rows
    # that author nothing. NOTHING USES IT AS THIS IS WRITTEN: it was cut for
    # the four speed candidates, which asked whether a hastened body could be
    # told from a slowed one, and that question died with the wash. Kept
    # because the fixture wave beside it is committed and a later bracket about
    # two auras of opposite sign will want exactly this pair.
    'speed' = @{
        Wave    = 'docs/frames/speed-pair.txt'
        Defense = 'docs/frames/four-lines.txt'
    }
    # The six rows of the Archer and Rogue lines against the recorded wave, so
    # the Fan of Knives is on the board and throwing. Nothing else on the
    # roster throws a knife.
    'pierce' = @{
        Wave    = $null
        Defense = 'docs/frames/pierce-lines.txt'
    }
    # The nine rows of the Mage, Cleric and Druid lines against the recorded
    # wave. THREE OF THE FOUR QUESTIONS ARE ON THIS ONE BOARD: every rung of the
    # Cleric and Druid lines fires the bolt, the Mage fires the ballistic shell,
    # and the Consecration lays its light -- so tick 313 carries a bolt in
    # flight, two shells in the air and the light on the ground at once.
    'magic' = @{
        Wave    = $null
        Defense = 'docs/frames/magic-lines.txt'
    }
    # Two Consecrations standing on rims and one aura standing nowhere near
    # one, with the six aura-carrying creep rows walking past them. The only
    # context in which the overhang #280 is about is visible at all.
    'rim' = @{
        Wave    = 'docs/frames/creep-auras.txt'
        Defense = 'docs/frames/rim-emitters.txt'
    }
}

# candidate -> which context it is photographed in, which ticks, and how far the
# close frame stands back.
$plan = [ordered]@{
    # Three frames of one bracket. Every aura is a flat translucent circle now
    # -- Sam signed the shape on 7 Sep 2026, see docs/decision-log.md -- so the
    # only thing left standing on nobody's signature is how see-through it is.
    #
    # THE AURAS CONTEXT IS THE ONE THAT SHOWS IT. Tick 272 is the Necromancer,
    # the Witch and the Skeleton Mage all pulsing within a few ticks of each
    # other on a corridor with bodies walking down it, so a frame carries two
    # circles overlapping and a body standing under one. That is the case the
    # alpha is judged on; a single circle on empty floor reads at any value.
    'aura-alpha-light'   = @{ Context = 'auras'; Ticks = '94,272'; Close = 20 }
    'aura-alpha-shipped' = @{ Context = 'auras'; Ticks = '94,272'; Close = 20 }
    'aura-alpha-heavy'   = @{ Context = 'auras'; Ticks = '94,272'; Close = 20 }

    # ---------------------------------------------------------------
    # Issue #280: the four things that do not read at 1600x900
    # ---------------------------------------------------------------
    #
    # EVERY ONE OF THESE WAS FOUND BY PHOTOGRAPHING THE BUILT PLAYER rather than
    # a sheet, so the wide frame is the deliverable and the close one is
    # supporting. A Close of 0 means no close frame is drawn at all: for the two
    # questions about a circle on the ground, the close camera crops out the
    # rim, which is the only thing those frames are for.
    #
    # THE TICKS ARE NOT GUESSES. capture-match-frames.ps1 logs a per-tick line
    # carrying the live shell count and the running totals for every effect, so
    # a tick with a knife actually in flight is found by reading the log of a
    # cheap narrow run rather than by opening pictures. Recorded here so nobody
    # has to do it twice:
    #
    #   pierce  the Fan of Knives throws at 599, 607, 615 and 623, and a knife
    #           lives 6 ticks -- so 617 and 624 each carry three in the air.
    #   magic   the Consecration pulses at 301 and 331; bolts leave at 311 and
    #           342 and live 5 ticks; shells are in the air from 295 to 343.
    #   rim     both rim Consecrations pulse together at 271 and 301, and the
    #           Blessing that is the control pulses with them.

    # The knife: a size ladder, then the shipped size against a dark blade, so
    # the sitting can tell a size answer from a contrast one.
    'knife-as-shipped' = @{ Context = 'pierce'; Ticks = '617,624'; Close = 14 }
    'knife-half-again' = @{ Context = 'pierce'; Ticks = '617,624'; Close = 14 }
    'knife-double'     = @{ Context = 'pierce'; Ticks = '617,624'; Close = 14 }
    'knife-dark'       = @{ Context = 'pierce'; Ticks = '617,624'; Close = 14 }

    # The bolt: the same ladder asked of the other shape, on the board where six
    # rows fire it.
    'bolt-as-shipped'  = @{ Context = 'magic'; Ticks = '313,343'; Close = 14 }
    'bolt-half-again'  = @{ Context = 'magic'; Ticks = '313,343'; Close = 14 }
    'bolt-double'      = @{ Context = 'magic'; Ticks = '313,343'; Close = 14 }
    'bolt-dark'        = @{ Context = 'magic'; Ticks = '313,343'; Close = 14 }

    # The shell: three colours at the shipped size and the shipped colour at
    # nearly twice the size, over the floor it actually crosses.
    #
    # WIDE ONLY, AND THAT WAS MEASURED RATHER THAN CHOSEN. These were drawn at
    # a close distance of 14 first, and at that framing shell-pale and
    # shell-warm are byte-identical to shell-as-shipped at both ticks -- zero
    # pixels different, on a colour that goes from near-black to near-white. A
    # shell at the shipped radius is behind a hex from that camera, and the only
    # close frame that moved at all was shell-bigger, whose larger sphere pokes
    # out past the terrain the smaller one hides behind. A magnified picture
    # that cannot show the thing being decided is worse than no picture, so
    # there is none.
    'shell-as-shipped' = @{ Context = 'magic'; Ticks = '313,320'; Close = 0 }
    'shell-pale'       = @{ Context = 'magic'; Ticks = '313,320'; Close = 0 }
    'shell-warm'       = @{ Context = 'magic'; Ticks = '313,320'; Close = 0 }
    'shell-bigger'     = @{ Context = 'magic'; Ticks = '313,320'; Close = 0 }

    # Where a ground effect stops -- SIGNED 7 Sep 2026, clipped. The bracket is
    # kept as the record of how it was decided, which is the same call the alpha
    # bracket above sits on. Wide only, and it has to be: the whole question is
    # what happens at the rim, and the close camera crops the rim out of the
    # picture.
    'reach-clipped'    = @{ Context = 'rim'; Ticks = '272,305'; Close = 0 }
    'reach-unclipped'  = @{ Context = 'rim'; Ticks = '272,305'; Close = 0 }
    'reach-shrunk'     = @{ Context = 'rim'; Ticks = '272,305'; Close = 0 }

    # THE CONSECRATION'S TWO QUESTIONS ARE GONE FROM HERE, BOTH ANSWERED. Sam
    # rejected the duty-cycle bracket's premise outright on 7 Sep 2026 -- the
    # light is always on now, so there is no cycle left to photograph -- and
    # took two hexes for the radius, which moved content/units.txt rather than
    # MatchTuning. Neither has a road not taken worth keeping a file for: one is
    # a constant that matches the aura's own period and the other is a unit row.
    # docs/decision-log.md carries both. The two fixture unit tables that asked
    # the radius went with them.
}

New-Item -ItemType Directory -Force -Path $OutDir | Out-Null

$capture = Join-Path $PSScriptRoot 'capture-match-frames.ps1'
$failed = @()

foreach ($name in $plan.Keys) {
    if ($Only -and ($Only -notcontains $name)) { continue }

    $file = Join-Path $CandidateDir "$name.txt"

    if (-not (Test-Path $file)) { throw "No candidate look at $file" }

    $entry = $plan[$name]
    $context = $contexts[$entry.Context]

    $framings = @()
    if (-not $CloseOnly) { $framings += @{ Suffix = 'played'; Distance = 0; Width = 1600 } }
    # A Close of zero is a candidate that has no close frame at all, rather than
    # one drawn from the default distance. The two questions about a circle on
    # the ground are the whole reason for it: what they ask is what happens
    # where the board ends, and a camera three times closer has the rim outside
    # the picture.
    if ((-not $WideOnly) -and $entry.Close -gt 0) {
        $framings += @{ Suffix = 'close'; Distance = $entry.Close; Width = 900 }
    }

    foreach ($framing in $framings) {
        $into = Join-Path $OutDir $framing.Suffix
        New-Item -ItemType Directory -Force -Path $into | Out-Null

        $arguments = @{
            Unity    = $Unity
            OutDir   = $into
            Effects  = $file
            Width    = $framing.Width
            Distance = $framing.Distance
            LogFile  = Join-Path $repoRoot "capture-effect-candidates.log"
        }

        if ($context.Wave) { $arguments.Wave = (Join-Path $repoRoot $context.Wave) }
        if ($context.Defense) { $arguments.Defense = (Join-Path $repoRoot $context.Defense) }
        if ($entry.Ticks) { $arguments.Ticks = $entry.Ticks }

        # A candidate may name a unit table of its own, on the entry rather than
        # on the context: a table here is not a board to photograph a look
        # against, it is half of the candidate, for a question that is a
        # simulation number and cannot be asked with a look constant. Nothing
        # uses it as this is written -- the Consecration's radius was the one
        # such question and it was answered on 7 Sep 2026 -- and it is kept
        # because the next aura whose reach is argued about will want exactly
        # this, and because capture-match-frames.ps1 already takes -Units.
        if ($entry.Units) { $arguments.Units = (Join-Path $repoRoot $entry.Units) }

        Write-Host ""
        Write-Host "=== $name ($($framing.Suffix), distance $($framing.Distance)) ===" -ForegroundColor Cyan

        & $capture @arguments

        if ($LASTEXITCODE -ne 0) {
            Write-Host "  FAILED with $LASTEXITCODE" -ForegroundColor Red
            $failed += "$name/$($framing.Suffix)"
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
