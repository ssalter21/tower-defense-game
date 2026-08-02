# Runs the client's tests headlessly, on either test platform.
#
# This is the implementation. `run-playmode-tests.ps1` and
# `run-editmode-tests.ps1` are one-line wrappers around it, so both platforms
# get identical hardening and neither can quietly grow its own version of it.
#
# Requires the Unity Editor to be CLOSED -- batchmode needs the project lock.
# Deliberately does NOT pass -nographics: the renderer must attach, so the tests
# exercise the same path a real play session does. That matters for EditMode
# runs too here, because the edit-mode tests open the match scene.
#
# Two things this script does that the obvious version does not, both because
# the obvious version's exit code means less than it looks like it means:
#
#   1. It WAITS for the editor to exit. `& $Unity ...` does not. Unity.exe is a
#      GUI-subsystem executable, and PowerShell's call operator only blocks on
#      console-subsystem ones -- so the naive form returns in ~0.03 s, leaves
#      $LASTEXITCODE untouched from whatever ran before it, and reports success
#      while the editor is still importing. Two invocations in a row then race
#      each other for the project lock. Start-Process -PassThru plus an explicit
#      WaitForExit() is what actually blocks and what actually yields the
#      editor's exit code. (The .Handle read is not decoration: a Process object
#      that never cached its handle cannot report ExitCode afterwards.)
#
#   2. It fails if the run changed the working tree, and NAMES the files. A
#      Unity run can rewrite tracked project files -- observed here: dirtying
#      one field in client/Packages/packages-lock.json and running the tests
#      got the file rewritten mid-run -- and a run that silently edits the repo
#      it is testing is worse than a run that fails.
#      Note this is an assertion, not a cleanup: it deliberately does not revert
#      afterwards, because a check that repairs what it finds is a check that
#      can never fail, and would have gone green through every regression it
#      exists to catch. Work in progress is fine; dirt that was already there
#      when the run started is not the run's doing and is left alone.
#
# Exit codes: whatever Unity returned, or 9 if Unity was happy but the run
# touched the tree, or 1 if the editor produced no test report at all.

param(
    [ValidateSet('PlayMode', 'EditMode')]
    [string]$Platform = 'PlayMode',
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$Results,
    [string]$LogFile
)

$ErrorActionPreference = 'Stop'

# Per-platform defaults, so two runs cannot overwrite each other's report and a
# crashed EditMode run cannot be read as the last PlayMode one.
$slug = $Platform.ToLowerInvariant()
if (-not $Results) { $Results = "$PSScriptRoot\..\$slug-results.xml" }
if (-not $LogFile) { $LogFile = "$PSScriptRoot\..\$slug.log" }

$project  = (Resolve-Path "$PSScriptRoot\..\client").Path
$repoRoot = (Resolve-Path "$PSScriptRoot\..").Path

if (-not (Test-Path $Unity)) { throw "Unity Editor not found at: $Unity" }

# --- Snapshot the tree BEFORE the run ----------------------------------------
# Pre-existing dirt is the developer's business, not this script's -- you have
# to be able to run the tests on work in progress. So the comparison is
# before-vs-after, not against clean.
#
# The snapshot carries a content hash per path, not just the porcelain status
# line. Without it the interesting case slips through: a file that was ALREADY
# modified before the run and that the editor then rewrites to something else
# reads as ` M path` both times. Same line, different file. The hash is what
# makes "the run changed this" visible.
function Get-TreeState {
    $lines = @(git -C $repoRoot status --porcelain --untracked-files=all 2>$null)
    foreach ($line in $lines) {
        if ($line.Length -lt 4) { continue }
        $status = $line.Substring(0, 2)
        $path   = $line.Substring(3).Trim('"')
        if ($path -match ' -> ') { $path = ($path -split ' -> ')[-1] }   # renames
        $full = Join-Path $repoRoot $path
        $hash = if (Test-Path -LiteralPath $full -PathType Leaf) {
            (Get-FileHash -LiteralPath $full -Algorithm SHA256).Hash.Substring(0, 12)
        } else { 'absent' }
        "$status $path [$hash]"
    }
}
$before     = @(Get-TreeState)
$beforePath = @{}
foreach ($b in $before) { $beforePath[($b -replace '^.. (.*) \[.*\]$', '$1')] = $true }

# A stale results file from a previous run is how a crashed run reports the
# previous run's passing numbers. Delete it so absence is detectable.
if (Test-Path $Results) { Remove-Item $Results -Force }

# --- Run, and actually wait for it -------------------------------------------
# One verbatim string rather than an array: Start-Process re-quotes array
# elements by its own rules, and a project path containing a space is exactly
# the input that turns into a baffling "project not found" hours later.
$unityArgs = '-batchmode -projectPath "{0}" -runTests -testPlatform {1} -testResults "{2}" -logFile "{3}"' -f `
    $project, $Platform, $Results, $LogFile

Write-Host "running $Platform tests in $project"
$proc = Start-Process -FilePath $Unity -ArgumentList $unityArgs -PassThru
$null = $proc.Handle
$proc.WaitForExit()
$code = $proc.ExitCode
Write-Host "editor exited with $code"

# --- Did it actually report anything? ----------------------------------------
if (-not (Test-Path $Results)) {
    Write-Host "no results file at $Results -- the editor produced no test report." -ForegroundColor Red
    Write-Host "see $LogFile"
    if ($code -eq 0) { $code = 1 }
} else {
    $run = ([xml](Get-Content $Results)).'test-run'
    Write-Host "tests: $($run.total)  passed: $($run.passed)  failed: $($run.failed)  result: $($run.result)"
}

# --- Snapshot the tree AFTER the run -----------------------------------------
$after = @(Get-TreeState)
$afterPath = @{}
foreach ($a in $after) { $afterPath[($a -replace '^.. (.*) \[.*\]$', '$1')] = $true }

# Deliberately symmetric: a run that quietly REVERTED an edit gets reported too,
# not just one that added dirt. "It put things back" is the shape of a check
# that can never fail, and it is indistinguishable from the editor eating your
# work. Either direction means the run wrote to the repo it was only supposed
# to read, and that is the thing worth knowing.
# The sentinel is load-bearing: Compare-Object refuses an empty -ReferenceObject,
# and a clean checkout -- CI, a fresh clone -- is exactly when both sides are
# empty. Without it the script dies on the tidiest tree there is.
$touched = @{}
foreach ($d in (Compare-Object -ReferenceObject (@('~clean~') + $before) `
                               -DifferenceObject (@('~clean~') + $after))) {
    $path = $d.InputObject -replace '^.. (.*) \[.*\]$', '$1'
    $touched[$path] = $d.InputObject.Substring(0, 2)
}

if ($touched.Count) {
    Write-Host ""
    Write-Host "FAIL: the run changed the working tree. Files it touched:" -ForegroundColor Red
    foreach ($path in ($touched.Keys | Sort-Object)) {
        $why = if (-not $beforePath.ContainsKey($path)) { 'new -- the run created or dirtied it' }
               elseif (-not $afterPath.ContainsKey($path)) { 'reverted by the run' }
               else { 'rewritten by the run' }
        Write-Host ("  {0} {1}   ({2})" -f $touched[$path], $path, $why) -ForegroundColor Red
    }
    Write-Host ""
    Write-Host "Nothing has been reverted for you -- decide what these are before re-running."
    if ($code -eq 0) { $code = 9 }
} else {
    Write-Host "working tree unchanged by the run."
}

exit $code
