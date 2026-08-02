# Regenerates the one scene, and the two plain materials it references.
#
# The scene is a GENERATED file: one empty object carrying MatchRoot, and
# nothing else. Everything that decides what the playfield looks like is in
# client/Assets/View/SceneFraming.cs, in C#, where a diff is readable and a
# merge is possible. Running this and committing what it writes is the whole
# workflow; hand-editing the .unity YAML is not a workflow at all.
#
# -batchmode -executeMethod, so it needs no editor session, no bridge and
# nobody at a keyboard -- and therefore requires the editor to be CLOSED,
# because batchmode needs the project lock.
#
# Exit code is the editor's. A throw inside the builder makes that non-zero.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$LogFile = "$PSScriptRoot\..\build-match-scene.log"
)

$ErrorActionPreference = 'Stop'

$project = (Resolve-Path "$PSScriptRoot\..\client").Path

if (-not (Test-Path $Unity)) { throw "Unity Editor not found at: $Unity" }

# Same reason as the test runner: Start-Process plus an explicit WaitForExit is
# what actually blocks on a GUI-subsystem executable and what actually yields
# its exit code. `& $Unity ...` returns in milliseconds and reports whatever
# ran before it.
$unityArgs = '-batchmode -quit -projectPath "{0}" -executeMethod View.Editor.MatchSceneBuilder.Rebuild -logFile "{1}"' -f `
    $project, $LogFile

Write-Host "rebuilding the match scene in $project"
$proc = Start-Process -FilePath $Unity -ArgumentList $unityArgs -PassThru
$null = $proc.Handle
$proc.WaitForExit()

Write-Host "editor exited with $($proc.ExitCode)"
if ($proc.ExitCode -ne 0) { Write-Host "see $LogFile" -ForegroundColor Red }

exit $proc.ExitCode
