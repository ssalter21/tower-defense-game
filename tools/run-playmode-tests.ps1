# Runs the client's PlayMode tests headlessly.
#
# The entry point everything points at; the implementation, and all of the
# hardening it carries, is in run-unity-tests.ps1 -- which the EditMode runner
# shares, so neither platform can grow its own subtly different version of
# "wait for the editor" or "fail if the run dirtied the tree".
#
# Requires the Unity Editor to be CLOSED -- batchmode needs the project lock.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$Results,
    [string]$LogFile
)

$ErrorActionPreference = 'Stop'

$forward = @{ Platform = 'PlayMode'; Unity = $Unity }
if ($Results) { $forward.Results = $Results }
if ($LogFile) { $forward.LogFile = $LogFile }

& "$PSScriptRoot\run-unity-tests.ps1" @forward
exit $LASTEXITCODE
