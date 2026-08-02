<#
.SYNOPSIS
    The parity run: the engine's trace against the command line's, in one
    command and in the only order that proves anything.

.DESCRIPTION
    Two halves, and the claim is the pair of them:

      1. tools/run-headless-match.ps1 -Verify -- a fresh command-line run of
         content/match.replay still produces content/golden-trace.txt, line for
         line. This is the half that keeps the committed trace honest. It needs
         nothing but the .NET SDK.

      2. tools/run-playmode-tests.ps1 -- the engine-side suite, which contains
         ParityRunTests: the same record played inside Unity with the renderer
         attached, its rolling per-tick state hash checked against that same
         trace, one tick at a time.

    Together those say the engine's trace and the command line's are identical
    tick for tick. Either half on its own says much less: the first proves the
    command line agrees with a file, and the second proves the engine agrees
    with a file that might have gone stale. THE ORDER IS DELIBERATE -- the
    command-line half runs first, so a stale trace is reported as a stale trace
    rather than as an engine divergence three minutes later.

    THE EDITOR HAS TO BE CLOSED. Batchmode needs the project lock, and that is
    a feature rather than a cost: it makes every parity run exercise the static
    command-line path, so a path that rots turns this red instead of quiet.

    NOTHING HERE WRITES A TRACE OUT OF THE ENGINE. The comparison happens
    inside the play-mode test, against bytes it reads from content/ -- there is
    no file dropped for something else to diff, and no button anybody has to
    press to produce one.

.EXAMPLE
    ./tools/run-parity-run.ps1
    Run both halves and exit non-zero if either disagrees.
#>
param(
    # Deliberately without a default, and forwarded to the half that runs the
    # editor only when somebody actually passed one -- so this script adds no
    # new copy of the editor path. A copy here would be one more thing to edit
    # the day the editor version moves, and the copy that got missed would be
    # the one that still worked on the machine it was written on.
    #
    # Not to be read as "the path is declared once": it is not. Seven scripts
    # in tools/ carry the same hardcoded default, and check-project-settings.ps1
    # asserts the version string an eighth time. Collapsing those into one
    # shared launcher is worth doing and is not done here.
    [string]$Unity
)

$ErrorActionPreference = 'Stop'

Write-Host ""
Write-Host "1/2  the command line still produces the committed trace" -ForegroundColor Cyan

& "$PSScriptRoot\run-headless-match.ps1" -Verify

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "The committed trace is not what a command-line run produces, so there is nothing " -ForegroundColor Red -NoNewline
    Write-Host "for the engine to be compared against. Fix that before reading anything about parity." -ForegroundColor Red
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "2/2  the engine produces it too, with the renderer attached" -ForegroundColor Cyan

$forward = @{}
if ($Unity) { $forward.Unity = $Unity }

& "$PSScriptRoot\run-playmode-tests.ps1" @forward

exit $LASTEXITCODE
