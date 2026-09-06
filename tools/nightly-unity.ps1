<#
.SYNOPSIS
    Runs the three Unity runners in turn and appends one line each to
    client/Logs/nightly.log.

.DESCRIPTION
    The Unity tests run nowhere but this machine with the editor closed, so
    nothing runs them unless somebody remembers. This is what the scheduled
    task registered by `register-nightly-unity.ps1` runs at 03:00; the log is
    the point of it, and it is where the morning after looks.

    THE LOCKFILE IS EVIDENCE, NOT PROOF, SO A SECOND WITNESS IS ASKED FOR.
    client/Temp/UnityLockfile is written by an editor that has the project open
    and removed when it closes cleanly -- but a batchmode run killed part-way
    leaves one behind, and a stale file looks exactly like a live one. Skipping
    on the file alone would mean one killed run buys silence every night after
    it, which is the weeks-under-a-green-suite this exists to end. So a running
    Unity.exe has to agree: with one, the night is genuinely skipped and exits
    0, because nothing was tested and nothing failed. Without one, the file is
    stale, and that is logged and exits non-zero -- a lock nobody holds needs a
    person, and saying so every morning is how it gets one.

    EACH RUNNER IS A CHILD PROCESS. They are scripts a shell runs on its own,
    and one that dies on a missing editor throws rather than returning a code
    -- in-process that error would take the other two runners with it. A child
    process turns any death into an exit code, which is what the log records.

    THE COUNTS COME FROM THE RESULTS FILE EACH RUNNER WRITES, and are read back
    only if the file is newer than the moment that runner started. Each runner
    deletes its report before it runs so that absence is detectable; the stamp
    covers the case where it died before getting that far, which would
    otherwise put a report from some previous night in tonight's line.

    PASS AND FAIL ARE THE RUNNER'S EXIT CODE, never re-derived from the counts,
    because the runners refuse for reasons no report carries: a test build that
    reported fewer tests than the floor, or a run that rewrote the working tree
    -- which the EditMode and PlayMode runners assert against and deliberately
    never repair. So a night where every test passed and Unity reimported a
    settings asset reads `FAIL  80 tests, 0 failed  exit 9`, and the runner's
    own log beside the results file names the files it touched. The count and
    the verdict disagreeing is the point of printing both.

.EXAMPLE
    ./tools/nightly-unity.ps1
    Exit code 0 if all three runners passed, or if an editor genuinely holds
    the project; 1 if any runner failed or the lockfile is stale.
#>

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$logPath = Join-Path $repoRoot 'client/Logs/nightly.log'
$lockFile = Join-Path $repoRoot 'client/Temp/UnityLockfile'

# client/Logs/ is ignored whole by client/.gitignore, and Unity does not create
# it until it runs. A nightly that could not write its own log would be a
# silent night.
$null = New-Item -ItemType Directory -Force -Path (Split-Path -Parent $logPath)

function Write-NightlyLine([string]$line) {
    $stamped = '{0}  {1}' -f (Get-Date -Format 'yyyy-MM-dd HH:mm:ss'), $line
    Add-Content -LiteralPath $logPath -Value $stamped
    Write-Host $stamped
}

if (Test-Path -LiteralPath $lockFile) {
    if (Get-Process -Name 'Unity' -ErrorAction SilentlyContinue) {
        Write-NightlyLine 'skipped   client/Temp/UnityLockfile, and a Unity is running -- an editor holds this project'
        exit 0
    }

    Write-NightlyLine 'stale     client/Temp/UnityLockfile, but no Unity is running -- delete it or nothing runs again'
    exit 1
}

# Runner, the name the log calls it, and the report it writes. The report paths
# are the runners' own defaults; nothing here passes them in, so a runner that
# moves its default moves this too.
$runners = @(
    @{ Name = 'editmode'; Script = 'run-editmode-tests.ps1'; Report = 'editmode-results.xml' }
    @{ Name = 'playmode'; Script = 'run-playmode-tests.ps1'; Report = 'playmode-results.xml' }
    @{ Name = 'player';   Script = 'run-player-tests.ps1';   Report = 'player-tests.xml' }
)

# The host this is running under, so the child is the same PowerShell the
# scheduled task started and there is no second one to find on PATH.
$powershell = (Get-Process -Id $PID).Path

# Reads the NUnit run element the test framework writes. Returns $null when
# there is no report from this run to read -- including when there is a file
# that will not parse, which is what a killed editor leaves. A throw here would
# take the runners after this one with it, and a night that reported nothing
# because it could not read one XML file is the worst of both.
function Read-Report([string]$path, [datetime]$after) {
    if (-not (Test-Path -LiteralPath $path)) { return $null }
    if ((Get-Item -LiteralPath $path).LastWriteTimeUtc -lt $after) { return $null }

    try {
        $run = ([xml](Get-Content -Raw -LiteralPath $path)).'test-run'
        return @{ Total = [int]$run.total; Failed = [int]$run.failed }
    } catch {
        return $null
    }
}

$failures = 0

foreach ($runner in $runners) {
    $startedUtc = (Get-Date).ToUniversalTime()

    & $powershell -NoProfile -NonInteractive -File (Join-Path $PSScriptRoot $runner.Script)
    $code = $LASTEXITCODE

    $report = Read-Report (Join-Path $repoRoot $runner.Report) $startedUtc
    $counts = 'no report'
    if ($report) { $counts = '{0} tests, {1} failed' -f $report.Total, $report.Failed }

    $verdict = 'PASS'
    if ($code -ne 0) {
        $verdict = 'FAIL'
        $failures++
    }

    Write-NightlyLine ('{0,-9} {1}  {2,-24} exit {3}' -f $runner.Name, $verdict, $counts, $code)
}

if ($failures -gt 0) { exit 1 }
exit 0
