<#
.SYNOPSIS
    Renders candidate art to PNG contact sheets so it can be chosen by looking
    at it.

.DESCRIPTION
    Art in this project is chosen by the developer, from something he can look
    at, and never from a filename. "Idle_A" and "Skeletons_Idle" are the same
    string to everyone and two different poses to nobody; "building_tower_A"
    and "building_tower_catapult" are two silhouettes that differ only at the
    six camera angles this game actually shows. This is the thing put in front
    of him so the choosing is a choice.

    It takes a JSON spec naming the candidates and writes one PNG per
    candidate, plus a manifest.json describing them, into the spec's outDir.
    Clips become a strip of poses sampled through the shipped
    SimDrivenAnimator; models become a six-frame turntable at the game's own
    snapped camera angles under the game's own sun.

    Candidates are named by asset path, and the paths are in the spec rather
    than in the tool, because a candidate is a scratch file staged into
    Assets/ for one run and deleted afterwards. Nothing unchosen is ever
    committed, so nothing unchosen may be hard-coded here.

    -batchmode -executeMethod, so it needs no editor session, no bridge and
    nobody at a keyboard -- and therefore requires the editor to be CLOSED,
    because batchmode needs the project lock.

    Exit code is the editor's. The capture throws if any candidate failed to
    render, because a sheet that silently did not appear is an option the
    developer never gets offered, and a missing option is invisible in a way a
    broken one is not.

.PARAMETER Spec
    Path to the JSON spec. See ArtPreviewCapture.cs for its shape.

.EXAMPLE
    ./tools/capture-art-previews.ps1 -Spec C:\scratch\preview-spec.json
#>
param(
    [Parameter(Mandatory = $true)]
    [string]$Spec,

    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$LogFile = "$PSScriptRoot\..\capture-art-previews.log"
)

$ErrorActionPreference = 'Stop'

$project = (Resolve-Path "$PSScriptRoot\..\client").Path
$specPath = (Resolve-Path $Spec).Path

if (-not (Test-Path $Unity)) { throw "Unity Editor not found at: $Unity" }

# Start-Process plus an explicit WaitForExit is what actually blocks on a
# GUI-subsystem executable and what actually yields its exit code. `& $Unity`
# returns in milliseconds and reports whatever ran before it.
$unityArgs = '-batchmode -quit -projectPath "{0}" -executeMethod View.Editor.ArtPreviewCapture.Run -artPreviewSpec "{1}" -logFile "{2}"' -f `
    $project, $specPath, $LogFile

Write-Host "capturing art previews from $specPath"
$proc = Start-Process -FilePath $Unity -ArgumentList $unityArgs -PassThru
$null = $proc.Handle
$proc.WaitForExit()

Write-Host "editor exited with $($proc.ExitCode)"
if ($proc.ExitCode -ne 0) { Write-Host "see $LogFile" -ForegroundColor Red }

exit $proc.ExitCode
