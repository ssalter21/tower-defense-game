<#
.SYNOPSIS
    Cuts the double-clickable build.

.DESCRIPTION
    The artefact the walking-skeleton slice ends in: a Windows player somebody
    who has never cloned this repository can unzip and run. Everything else in
    this project is looked at through an editor, a test runner or a batchmode
    capture, all of which need the project; the sit-down in docs/sit-down.md is
    run against this instead.

    -batchmode -executeMethod, so it needs no editor session, no bridge and
    nobody at a keyboard -- and therefore requires the editor to be CLOSED,
    because batchmode needs the project lock.

    THE EXIT CODE IS NOT ENOUGH ON ITS OWN, which is why View.Editor.PlayerBuild
    reads the build report and throws. Unity reports a failed build by handing
    back a report rather than by failing, and `-batchmode -quit` will happily
    exit zero having produced nothing. This script additionally refuses to
    report success unless the executable is actually on disk afterwards: a green
    run and no .exe is the failure that sends somebody looking for a file nobody
    wrote.

    The build lands in client/Builds/, which client/.gitignore already ignores.
    It is a hundred megabytes of engine output that can be made again from the
    commit -- the opposite of the committed simulation plug-in beside it, which
    is committed precisely because nobody can make it again from this repository
    alone.

.EXAMPLE
    ./tools/build-player.ps1
    Build into client/Builds/Windows/ and print where the executable is.

.EXAMPLE
    ./tools/build-player.ps1 -OutDir D:\sitdown
    Build somewhere else -- for handing to a machine that never cloned this.
#>
param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$OutDir,
    [string]$LogFile = "$PSScriptRoot\..\build-player.log"
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$project = Join-Path $repoRoot 'client'
$executableName = 'TowerDefense.exe'

if (-not (Test-Path $Unity)) { throw "Unity Editor not found at: $Unity" }
if (-not $OutDir) { $OutDir = Join-Path $project 'Builds/Windows' }

$null = New-Item -ItemType Directory -Force -Path $OutDir

# Resolved to an absolute path before it crosses into the editor. A relative one
# would be resolved against the editor's working directory, which is not this
# shell's, and the build would land somewhere nobody looked.
$OutDir = (Resolve-Path $OutDir).Path

$unityArgs = @(
    '-batchmode', '-quit'
    '-projectPath', "`"$project`""
    '-executeMethod', 'View.Editor.PlayerBuild.Run'
    '-playerBuildOut', "`"$OutDir`""
    '-logFile', "`"$LogFile`""
)

# Start-Process plus an explicit WaitForExit is what actually blocks on a
# GUI-subsystem executable and what actually yields its exit code. `& $Unity`
# returns in milliseconds and reports whatever ran before it.
Write-Host "building the player from $project into $OutDir"
$proc = Start-Process -FilePath $Unity -ArgumentList ($unityArgs -join ' ') -PassThru
$null = $proc.Handle
$proc.WaitForExit()

Write-Host "editor exited with $($proc.ExitCode)"

if ($proc.ExitCode -ne 0) {
    Write-Host "see $LogFile" -ForegroundColor Red
    exit $proc.ExitCode
}

# The second half of the check. A build that produced nothing can still exit
# zero, and a report nobody reads is a report that says whatever you hoped.
$executable = Join-Path $OutDir $executableName

if (-not (Test-Path -LiteralPath $executable -PathType Leaf)) {
    Write-Host ""
    Write-Host "The editor exited cleanly and there is no $executableName in $OutDir." -ForegroundColor Red
    Write-Host "see $LogFile"
    exit 1
}

$size = (Get-ChildItem -LiteralPath $OutDir -Recurse -File | Measure-Object -Property Length -Sum).Sum

Write-Host ""
Write-Host "double-click this:" -ForegroundColor Green
Write-Host "  $executable"
Write-Host ("  {0:N0} MB in {1}" -f ($size / 1MB), $OutDir)

exit 0
