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
    # blast ring -- with the six aura-carrying creep rows walking at it.
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
    # The same defense, against a wave that sends a Skeleton Mage and two rows
    # that author nothing. THE WITCH IS THE REASON IT IS NOT THE WAVE ABOVE: she
    # armours every friend within two hexes, and a body carrying a speed
    # modifier and an armour one is drawn as neither -- so against creep-auras
    # nearly every hastened body is in the both-modifiers bucket and a candidate
    # that moves the haste colour changes nothing. Measured: three of the four
    # speed candidates came out byte-identical to the shipped look that way.
    'speed' = @{
        Wave    = 'docs/frames/speed-pair.txt'
        Defense = 'docs/frames/four-lines.txt'
    }
}

# candidate -> which context it is photographed in, which ticks, and how far the
# close frame stands back.
$plan = [ordered]@{
    'auras-as-shipped'            = @{ Context = 'auras'; Ticks = '94,272'; Close = 20 }
    'haste-under-body'            = @{ Context = 'auras'; Ticks = '272';    Close = 24 }
    'haste-ring-at-reach'         = @{ Context = 'auras'; Ticks = '272';    Close = 24 }
    'ward-light-on-ground'        = @{ Context = 'auras'; Ticks = '272';    Close = 24 }
    'ward-ring-at-reach'          = @{ Context = 'auras'; Ticks = '272';    Close = 24 }
    'hex-cage'                    = @{ Context = 'auras'; Ticks = '272';    Close = 24 }
    'hex-under-body'              = @{ Context = 'auras'; Ticks = '272';    Close = 24 }
    'frost-halo'                  = @{ Context = 'auras'; Ticks = '94,272';     Close = 24 }
    'frost-ring-at-reach'         = @{ Context = 'auras'; Ticks = '94,272';     Close = 24 }

    'ring-as-shipped'             = @{ Context = 'auras'; Ticks = '272';    Close = 20 }
    'ring-warm'                   = @{ Context = 'auras'; Ticks = '272';    Close = 20 }
    'ring-heavy'                  = @{ Context = 'auras'; Ticks = '272';    Close = 20 }

    'tower-marks-on'              = @{ Context = 'auras'; Ticks = '94,272';     Close = 24 }
    'both-modifiers-third-colour' = @{ Context = 'auras'; Ticks = '272';    Close = 24 }
    'bar-crossed'                 = @{ Context = 'auras'; Ticks = '272';    Close = 24 }
    'bar-clamped'                 = @{ Context = 'auras'; Ticks = '272';    Close = 24 }

    'blessing-halo-shipped'       = @{ Context = 'lines'; Ticks = '274';               Close = 20 }
    'blessing-at-the-feet'        = @{ Context = 'lines'; Ticks = '274';               Close = 20 }
    'blessing-collar'             = @{ Context = 'lines'; Ticks = '274';               Close = 20 }
    'blessing-held-halo'          = @{ Context = 'lines'; Ticks = '274';               Close = 20 }

    'speed-one-colour'            = @{ Context = 'speed'; Ticks = '240';               Close = 18 }
    'speed-warm-haste'            = @{ Context = 'speed'; Ticks = '240';               Close = 18 }
    'speed-green-haste'           = @{ Context = 'speed'; Ticks = '240';               Close = 18 }
    'speed-both-moved'            = @{ Context = 'speed'; Ticks = '240';               Close = 18 }
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
