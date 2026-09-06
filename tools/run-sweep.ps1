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

    THE FIELD IS content/field.txt AND NOT content/wave.txt, and this script no
    longer has an opinion about that: it hands over the content DIRECTORY and
    the runner takes the seven files out of it by the names it declares. A round
    is resolved against a field of K opponents drawn from a population of other
    players' rounds; there is no such population until runs are stored, so the
    canned pair standing in for it is field.txt behind the committed defense,
    which that opponent OPENS behind and then builds on out of a purse of its
    own, by the same half-purse rule a run's own scripted player builds by.
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

    A ROW IS A CREEP AGAINST ONE WALL, AND THE WALL IS A COLUMN. Every creep is
    scored against a wall of each attack type the roster has a tower for, so
    the committed report is five creeps by three walls and fifteen rows. The
    reason is the damage matrix: it is authored so that no attack type is
    globally better, so a wall of one type hard-counters one armour class and
    barely troubles another -- pierce takes 140% off a swift body where magic
    takes 140% off an armoured one. Swept against a single wall the roster
    reported a landslide and two zeros, and which creep got which was a fact
    about what the defending bot happened to buy rather than about any creep.
    Two rows are comparable when their wall column matches and not otherwise.
    See #242 and docs/research/a-sweep-row-measures-the-walls-attack-type.md.

    A RESTRICTED WALL OPENS ON NOTHING, AND content/defense.txt IS STILL THE
    OPPONENTS' SEED EVERYWHERE ELSE. That file is four archers and two mages,
    so a wall asked for pierce that opened behind it would carry a creep's
    counter in its seed -- measured, the armoured Minion reported zero against
    both pierce and impact until the seed came out. Opening every restricted
    wall empty makes the three columns equal by construction: same purse, same
    rounds, one difference.

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
    ./tools/run-sweep.ps1 -Policy all-in -PerRun -Out artefacts/all-in.csv
    The same roster under the player that builds nothing, with a row for every
    run behind the folded ones. Two of these under the two players are what
    answers a question no single report can: what the defensive half of a round
    is worth.

.EXAMPLE
    ./tools/run-sweep.ps1 -Walls magic -Out artefacts/magic.csv
    One column of the committed report on its own: every creep against a wall
    of mages and nothing else.

.EXAMPLE
    ./tools/run-sweep.ps1 -Walls any -Out artefacts/mixed.csv
    The report as it was before #242 -- one wall, whatever the defending bot
    buys unrestricted, which is the wall a run actually meets. Read it beside
    the committed one rather than instead of it: it says what a roster does
    against a real opponent and cannot say which creep is which.

.EXAMPLE
    ./tools/run-sweep.ps1 -ContentFile @{ map = 'maps/second.txt' } -Runs 64 -Out artefacts/second.csv
    Score another board. Any of the seven content files can be pointed somewhere
    else by the option the runner declares for it, so this is an argument rather
    than an edit -- and the other six stay where they were.
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

    # Where the seven content files live. The runner takes them out of it by the
    # names it declares, so pointing the sweep at a whole other set of content
    # is one argument and this script names no file.
    [string]$Content,

    # One or more of the seven somewhere else: @{ map = 'maps/second.txt' }.
    # Keyed by the runner's own option names, so scoring another board costs an
    # argument here rather than a retrofit across every call site.
    [hashtable]$ContentFile = @{},

    # Which scripted player builds and sends. 'even-share' splits every purse
    # between the board and the wave, and 'all-in' builds nothing and sends the
    # lot; the two reports side by side are what says what the defensive half of
    # a round is worth. The name is printed into the file as a parameter row, so
    # two reports that differ only in this cannot be mistaken for each other.
    [string]$Policy,

    # Keep a row for every run under the folded rows, which is the distribution
    # the fold is a summary of. Off by default because the row count is the
    # roster times the sample, and a sweep that wanted the fold alone should not
    # carry that.
    [switch]$PerRun,

    # What the OPPONENTS' towers are made of, and every creep is scored against
    # every one of them: 'pierce,impact,magic' in any combination, or 'any' for
    # whatever the defending bot buys unrestricted. Left off, it is every attack
    # type the roster has a tower for -- which is what the committed report is.
    #
    # ONE WALL CANNOT PRICE A ROSTER, which is why this is not a switch that
    # defaults to off. The matrix is authored so that none of the three attack
    # types is globally better, so a report swept against one wall shows a
    # landslide and a zero and which creep gets which is a fact about the
    # defending bot. Measured in
    # docs/research/a-sweep-row-measures-the-walls-attack-type.md.
    [string]$Walls
)

$ErrorActionPreference = 'Stop'

if ($Verify -and $Regenerate) {
    throw "-Verify and -Regenerate are opposites: one checks the committed file, the other rewrites it."
}

# The committed artefact is one shape: the folded report, under the default
# player. Either of these would rewrite it into a file -Verify then reads as a
# difference in the content, which is a red gate about the wrong thing.
if (($PerRun -or $Policy -or $Walls) -and ($Verify -or $Regenerate)) {
    throw "-PerRun, -Policy and -Walls describe a sweep of your own, and content/sweep.csv is the committed one. Write it somewhere else with -Out."
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

if (-not $Content) { $Content = Join-Path $repoRoot 'content' }

# The report this repository commits, which is the authored content's own and
# does not move when somebody sweeps another set of it.
$committed = Join-Path $repoRoot 'content/sweep.csv'

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
$sweepArguments = @('sweep') + (Get-ContentArguments $Content $ContentFile) + @(
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

# Both are left off entirely where nobody asked, for the same reason --runs is:
# the default player and the folded report are the runner's answers, and a
# second copy of either here is a second place for them to drift.
if ($Policy) {
    $sweepArguments += @('--policy', $Policy)
}

if ($PerRun) {
    $sweepArguments += '--per-run'
}

# Left off entirely where nobody asked, for the reason --runs and --policy are:
# which walls a roster has towers for is the runner's answer, derived from the
# roster it read, and a list spelled out here would be a second copy of it free
# to name a wall this content cannot build.
if ($Walls) {
    $sweepArguments += @('--walls', $Walls)
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
