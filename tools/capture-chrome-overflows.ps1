# Renders the shipped chrome at the two late-round states the roster's
# forty-four rows stopped it fitting -- a wave with every creep in it, and a
# Bishop's ladder offering a capstone for a token -- through the real chrome
# at 1600x900, which is the size the built player runs at.
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
#            opens on nothing, which is the moment #282 said nothing on screen
#            explained. The header's token field now does.
#
# THIS WAS capture-chrome-candidates.ps1 until #285 signed. Issue #282 rendered
# candidate layouts for these states as classes under
# client/Assets/Editor/ChromeCandidates/, named in the spec by type; the
# sitting signed one of each on 11 September 2026 (docs/decision-log.md) and
# the classes came out with the sheets they drew. What the spec draws now is
# the chrome as shipped, at the rounds the candidates were compared at.
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
    -LogFile (Join-Path $repoRoot 'capture-chrome-overflows.log')

exit $LASTEXITCODE
