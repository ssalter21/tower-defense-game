<#
.SYNOPSIS
    Renders candidate chrome to PNGs, so a layout can be chosen by looking at
    it.

.DESCRIPTION
    The same argument as capture-art-previews.ps1, one seam over. Art is never
    picked from a filename here because "Idle_A" and "Skeletons_Idle" are the
    same string to everyone and two different poses to nobody. A layout has the
    identical failure: "the purse goes in the header" and "the purse goes over
    the palette" are two sentences that agree with each other and two screens
    that do not.

    It takes a JSON spec naming shots -- a moment of a run, and optionally a
    candidate layout to run against it -- and writes one PNG per shot plus a
    manifest.json into the spec's outDir. The chrome is the real chrome over
    the real board, with the prices, names and purse the content files say,
    because a mockup with invented numbers on it is a picture of a game this
    project does not ship.

    THE SHEETS ARE DOCUMENTATION, NOT AN ORACLE, which is the call
    docs/frames/README.md already makes for match frames. What catches broken
    chrome is Tests.PlayMode/ChromeLayoutTests.

    -batchmode -executeMethod, so it needs no editor session, no bridge and
    nobody at a keyboard -- and therefore requires the editor to be CLOSED,
    because batchmode needs the project lock.

    It runs the editor WITHOUT -quit, and that is deliberate: a runtime panel
    never lays out in an edit-mode batchmode editor -- a bar built there
    resolves to NaN by NaN and renders zero pixels -- so the capture enters
    play mode and exits the editor itself when the last sheet is written.

.PARAMETER Spec
    Path to the JSON spec. See UiPreviewCapture.cs for its shape.

.EXAMPLE
    ./tools/capture-ui-previews.ps1 -Spec C:\scratch\ui-spec.json
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Spec,

    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$LogFile = "$PSScriptRoot\..\capture-ui-previews.log"
)

$ErrorActionPreference = 'Stop'

$project = (Resolve-Path "$PSScriptRoot\..\client").Path
$specPath = (Resolve-Path $Spec).Path

if (-not (Test-Path $Unity)) { throw "Unity Editor not found at: $Unity" }

# Start-Process plus an explicit WaitForExit is what actually blocks on a
# GUI-subsystem executable and what actually yields its exit code. `& $Unity`
# returns in milliseconds and reports whatever ran before it.
$unityArgs = '-batchmode -projectPath "{0}" -executeMethod View.Editor.UiPreviewCapture.Run -uiPreviewSpec "{1}" -logFile "{2}"' -f `
    $project, $specPath, $LogFile

Write-Host "capturing ui previews from $specPath"
$proc = Start-Process -FilePath $Unity -ArgumentList $unityArgs -PassThru
$null = $proc.Handle
$proc.WaitForExit()

Write-Host "editor exited with $($proc.ExitCode)"
if ($proc.ExitCode -ne 0) { Write-Host "see $LogFile" -ForegroundColor Red }

exit $proc.ExitCode
