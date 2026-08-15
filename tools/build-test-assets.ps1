<#
.SYNOPSIS
    Regenerates the assets the play-mode suite reads outside the editor.

.DESCRIPTION
    Two things, both written because the editor-only way of getting them does
    not exist anywhere else:

      Assets/Resources/MatchArt.asset   the models and clips, resolved
                                        from Tests.Fixtures.ChosenArt's paths,
                                        because AssetDatabase is editor-only

      Assets/Resources/TestClips/*.anim the sampling tests'' analytic oracle,
                                        because AnimationClip.SetCurve is
                                        editor-only on a non-legacy clip and
                                        silently leaves an empty clip in a
                                        player

    Without them the whole play-mode suite had to sit behind #if UNITY_EDITOR
    -- and a test class inside a dead #if compiles to nothing, yields no tests,
    and lets the run report green having asserted nothing.

    Everything it writes is committed, like every other generated file in this
    repository. Tests.EditMode.GeneratedProjectFilesTests fails when the
    manifest has drifted from the paths it came from; a drifted oracle clip
    fails its own tests by name.

    -batchmode -executeMethod, so it needs no editor session, no bridge and
    nobody at a keyboard -- and therefore requires the editor to be CLOSED,
    because batchmode needs the project lock.

.EXAMPLE
    ./tools/build-test-assets.ps1
    Rewrite the manifest and the oracle clips, and report where they landed.
#>
param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$LogFile = "$PSScriptRoot\..\build-test-assets.log"
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'client'
$written = @(
    'Assets/Resources/MatchArt.asset'
    'Assets/Resources/TestClips/OracleLinear.anim'
    'Assets/Resources/TestClips/OracleConstantZero.anim'
    'Assets/Resources/TestClips/OracleConstantTravel.anim'
) | ForEach-Object { Join-Path $project $_ }

if (-not (Test-Path $Unity)) { throw "Unity Editor not found at: $Unity" }

$unityArgs = @(
    '-batchmode', '-quit'
    '-projectPath', "`"$project`""
    '-executeMethod', 'Tests.Fixtures.GeneratedTestAssets.Run'
    '-logFile', "`"$LogFile`""
)

Write-Host "writing the generated test assets from $project"
$proc = Start-Process -FilePath $Unity -ArgumentList ($unityArgs -join ' ') -PassThru
$null = $proc.Handle
$proc.WaitForExit()

Write-Host "editor exited with $($proc.ExitCode)"

if ($proc.ExitCode -ne 0) {
    Write-Host "see $LogFile" -ForegroundColor Red
    exit $proc.ExitCode
}

# The second half of the check, for the same reason build-player.ps1 has one: a
# batchmode editor will happily exit zero having written nothing at all.
$missing = @($written | Where-Object { -not (Test-Path -LiteralPath $_ -PathType Leaf) })

if ($missing.Count -gt 0) {
    Write-Host ""
    Write-Host "The editor exited cleanly and did not write:" -ForegroundColor Red
    $missing | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
    Write-Host "see $LogFile"
    exit 1
}

Write-Host ""
Write-Host "wrote:" -ForegroundColor Green
$written | ForEach-Object { Write-Host "  $_" }
Write-Host "  all generated and committed -- commit them beside the change that caused them"

exit 0
