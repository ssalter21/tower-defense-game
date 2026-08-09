<#
.SYNOPSIS
    Plays a population of runs per creep and writes the balance report.

.DESCRIPTION
    The shell end of simcli's sweep verb. It builds the command-line project,
    plays every creep in the roster over a population of seeds, and writes what
    they came to as a comma-separated file -- win rate, cost efficiency, and
    both of those binned by how many ingredients a run ended up holding.

    THE HARNESS COMPUTES AND THE COMMAND LINE WRITES. The simulation cannot
    open a file -- System.IO is a banned namespace there and the build gate
    scans the compiled image for it -- so the sweep returns rows and the shell
    end of it is where a CSV comes from. That split is the whole reason this is
    a mode and a file rather than a project.

    NOTHING HERE NEEDS AN EDITOR. No project open, no plug-in installed, no
    socket to a running Unity, which is the third working agreement in
    AGENTS.md.

    The runner references the COMMITTED simulation assembly rather than the
    simulation project, exactly as run-headless-match.ps1 does, so a source
    change committed without its rebuild goes red here rather than being
    papered over by MSBuild rebuilding sim/ on the way past.

    THE FIELD IS content/field.txt AND NOT content/wave.txt. A round is
    resolved against a field of K opponents drawn from a population of other
    players' rounds; there is no such population until runs are stored, so the
    canned pair standing in for it is that wave behind the committed defense.
    It is a build phase's output -- a hundred gold, everything on tick zero --
    because the skeleton's authored match is forty creeps and three hundred and
    eighty gold, which no purse in this economy can compose. That file's own
    header carries the measurements.

    ONE COMMITTED ARTEFACT FALLS OUT OF THIS: content/sweep.csv, the report a
    real sweep produced at the committed shape. -Verify proves the committed
    copy is still what a sweep produces; -Regenerate makes it so again after a
    deliberate content change. -Verify writes into scratch space and compares,
    rather than writing over the committed file and finding it equal, which
    would be a check that cannot fail.

    THE COVERAGE ROWS ARE PART OF THE REPORT AND NOT A HEADER. -Runs samples
    the seed space and -MostCreeps takes a prefix of the roster, and both land
    in the file as rows saying what was covered -- so a sweep somebody
    truncated does not read like a complete one three months later.

.EXAMPLE
    ./tools/run-sweep.ps1
    Play the committed sweep and print the report to the shell.

.EXAMPLE
    ./tools/run-sweep.ps1 -Out artefacts/sweep.csv -Runs 64
    A wider sample, written where you asked for it.

.EXAMPLE
    ./tools/run-sweep.ps1 -Verify
    Exit 0 if a sweep at the committed shape still produces content/sweep.csv,
    and 1 naming the first difference if it does not.

.EXAMPLE
    ./tools/run-sweep.ps1 -Regenerate
    Rewrite content/sweep.csv from a real sweep. The thing to do after a
    deliberate content or rules change.

.EXAMPLE
    ./tools/run-sweep.ps1 -Map maps/second.txt -Runs 64 -Out artefacts/second.csv
    Score another board. Every one of the seven content files is a parameter, so
    pointing the harness somewhere else is an argument rather than an edit.
#>
param(
    [string]$Out,
    [switch]$Verify,
    [switch]$Regenerate,

    # The seed every run of the sweep derives its own seed from. A different
    # number is a different population of runs and therefore a different report,
    # which is why the committed one is pinned here and printed into the file.
    [ulong]$Seed = 20260807,

    # How many seeds each creep is played on, or zero for the library's own
    # default -- SweepPlan.DefaultRunsPerCreep, which is the number the committed
    # report was produced at. Whatever it comes to is reported in the file as the
    # sample it is.
    [int]$Runs = 0,

    # N and K. Ten and ten are this map's answers and both are expected to move.
    [int]$Waves = 10,
    [int]$FieldSize = 10,

    # The seven content files. All seven are parameters so that pointing the
    # sweep at another map to score it, or at another matrix, costs an argument
    # here rather than a retrofit across every call site.
    [string]$Map,
    [string]$Units,
    [string]$Upgrades,
    [string]$Rules,
    [string]$Schedule,
    [string]$Defense,
    [string]$Field
)

$ErrorActionPreference = 'Stop'

if ($Verify -and $Regenerate) {
    throw "-Verify and -Regenerate are opposites: one checks the committed file, the other rewrites it."
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$content = Join-Path $repoRoot 'content'

if (-not $Map)      { $Map = Join-Path $content 'map.txt' }
if (-not $Units)    { $Units = Join-Path $content 'units.txt' }
if (-not $Upgrades) { $Upgrades = Join-Path $content 'upgrades.txt' }
if (-not $Rules)    { $Rules = Join-Path $content 'ruleset.txt' }
if (-not $Schedule) { $Schedule = Join-Path $content 'schedule.txt' }
if (-not $Defense)  { $Defense = Join-Path $content 'defense.txt' }
if (-not $Field)    { $Field = Join-Path $content 'field.txt' }

$committed = Join-Path $content 'sweep.csv'

# Built into scratch space rather than into the project's own bin/, so that a
# run of this script cannot leave the working tree dirtier than it found it.
$build = Join-Path ([System.IO.Path]::GetTempPath()) 'simcli-build-sweep'
$program = Join-Path $build 'Sim.Cli.dll'

. (Join-Path $PSScriptRoot '_shared.ps1')

& dotnet build (Join-Path $repoRoot 'simcli') --configuration Debug --nologo --output $build | Out-Host

if ($LASTEXITCODE -ne 0) {
    throw "Building simcli failed (exit $LASTEXITCODE)."
}

$number = [System.Globalization.CultureInfo]::InvariantCulture

# The whole invocation, written once. A verb reading a different roster than
# the one the report names is a sweep that refuses for a reason that has
# nothing to do with what was being checked.
$sweepArguments = @(
    'sweep',
    '--map', $Map,
    '--units', $Units,
    '--upgrades', $Upgrades,
    '--rules', $Rules,
    '--schedule', $Schedule,
    '--defense', $Defense,
    '--field', $Field,
    '--seed', $Seed.ToString($number),
    '--waves', $Waves.ToString($number),
    '--field-size', $FieldSize.ToString($number),

    # A sweep wants N rounds of data out of every row rather than a short one
    # wherever a build failed, which is the whole reason death is a flag.
    '--no-death')

# --runs is left off entirely where nobody asked for one, so the number in force
# is the library's and there is no second copy of it here to drift.
if ($Runs -gt 0) {
    $sweepArguments += @('--runs', $Runs.ToString($number))
}

if ($Regenerate) {
    Invoke-SimCli ($sweepArguments + @('--out', $committed))
    exit 0
}

if (-not $Verify) {
    if ($Out) {
        Invoke-SimCli ($sweepArguments + @('--out', $Out))
    }
    else {
        Invoke-SimCli $sweepArguments
    }

    exit 0
}

$scratch = Join-Path ([System.IO.Path]::GetTempPath()) 'simcli-sweep-verify'

if (Test-Path $scratch) {
    Remove-Item $scratch -Recurse -Force
}

$fresh = Join-Path $scratch 'sweep.csv'

Invoke-SimCli ($sweepArguments + @('--out', $fresh))

$same = Test-SameText "content/sweep.csv" `
    ([System.IO.File]::ReadAllText($committed)) `
    ([System.IO.File]::ReadAllText($fresh))

if ($same) {
    exit 0
}

Write-Host ""
Write-Host "If the content or the rules changed on purpose, regenerate it:"
Write-Host "  ./tools/run-sweep.ps1 -Regenerate"
exit 1
