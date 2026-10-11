# Renders the wrapper -- the main menu the build opens on, the settings screen,
# and a run played to its end with the way back to the menu -- through the
# real screens at 1600x900, which is the size the built player runs at.
#
# The spec is docs/chrome/wrapper/spec.json and this passes it through
# capture-ui-previews.ps1. Its three states are menu, settings and over:
#
#   menu      a MatchRoot built and left idle with the menu open over it,
#             which is what Match.unity does on load.
#   settings  the same, with the settings screen opened from the menu.
#   over      a run the simulation's scripted player plays round after round
#             until it ends, in a scratch folder of its own so the rounds it
#             stores join no pool another capture reads.
#
# Every word on the three screens is a placeholder, marked on the screen
# itself as not signed. The sitting that signs them is #319.
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

if (-not $Spec) { $Spec = Join-Path $repoRoot 'docs/chrome/wrapper/spec.json' }

& (Join-Path $PSScriptRoot 'capture-ui-previews.ps1') -Unity $Unity -Spec $Spec `
    -LogFile (Join-Path $repoRoot 'capture-wrapper.log')

exit $LASTEXITCODE
