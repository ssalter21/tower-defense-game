# Runs the client's EditMode tests headlessly.
#
# EditMode is where anything about the PROJECT rather than about a play session
# is asserted: what the scene asset contains, what the committed materials are
# coloured, whether the generated streaming copy still matches the authored
# content. None of those needs the game to be running, and several of them
# cannot be checked while it is -- the scene's root count is a fact about a file
# on disk, and by the time PlayMode has loaded it the test runner has added
# roots of its own.
#
# Same implementation, and therefore the same hardening, as the PlayMode
# runner: it waits for the editor rather than racing it, and it fails if the
# run rewrote the working tree.
#
# Requires the Unity Editor to be CLOSED -- batchmode needs the project lock.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$Results,
    [string]$LogFile
)

$ErrorActionPreference = 'Stop'

$forward = @{ Platform = 'EditMode'; Unity = $Unity }
if ($Results) { $forward.Results = $Results }
if ($LogFile) { $forward.LogFile = $LogFile }

& "$PSScriptRoot\run-unity-tests.ps1" @forward
exit $LASTEXITCODE
