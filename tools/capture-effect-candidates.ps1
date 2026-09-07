# Renders every candidate effect look in docs/frames/effect-candidates/ through
# the real match, twice: once at the framing the game is played at and once with
# the camera three times closer.
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
    if (-not $WideOnly) { $framings += @{ Suffix = 'close'; Distance = $entry.Close; Width = 900 } }

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
