# Renders the candidate chrome for the three overflows issue #282 is about --
# the wave bar, the offer's longest rung, and a standing capstone-token count --
# through the real chrome at 1600x900, which is the size the built player runs
# at and the size all three break at.
#
# WHY THIS IS NOT JUST capture-ui-previews.ps1. It is: the spec is
# docs/chrome/overflows/spec.json and this passes it through. What this file
# adds is the one place that says what that spec is FOR and why its shots are
# taken at wave 9 and wave 2 rather than at the opening round:
#
#   wave 9   is the first round at which a run has been granted all three
#            capstone tokens, and the round #270 photographed with every creep
#            the roster has in the bar. The rounds before it are played by the
#            simulation's own scripted player (CoverThenUpgradeBot) on the tower
#            side, and on the wave side by sending one of every creep the purse
#            can still cover, cheapest first -- see UiPreviewCapture.PlayTo.
#   wave 2   is a round with a purse deep enough to stand a Cleric and climb it
#            to a Bishop, and no token granted yet -- so the Bishop's ladder
#            opens on nothing, which is the moment the ticket says nothing on
#            screen explains.
#
# THE CANDIDATES ARE CLASSES, NOT FILES, because a layout is code: they live in
# client/Assets/Editor/ChromeCandidates/ and are named in the spec by type.
# They come out of the project when the sitting has signed one.
#
# NOTHING HERE DECIDES ANYTHING. AGENTS.md rule 6 puts anything a player sees on
# the human side of the line. This renders the alternatives and stops.
#
# -batchmode -executeMethod, so the editor must be CLOSED. Never edit a file
# while a run is going: it forces a synchronous recompile and the run dies with
# exit 3 and no results.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$Spec
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

if (-not $Spec) { $Spec = Join-Path $repoRoot 'docs/chrome/overflows/spec.json' }

& (Join-Path $PSScriptRoot 'capture-ui-previews.ps1') -Unity $Unity -Spec $Spec `
    -LogFile (Join-Path $repoRoot 'capture-chrome-candidates.log')

exit $LASTEXITCODE
