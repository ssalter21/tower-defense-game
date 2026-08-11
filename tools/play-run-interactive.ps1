<#
.SYNOPSIS
    Plays a run at a prompt, one round at a time, and writes down what you
    decided.

.DESCRIPTION
    The shell end of simcli's play verb. It builds the command-line project and
    hands you each round of a run on the committed content: the map, the
    standing towers, this wave's menu, what you may build and what you may
    send, then a prompt. The words are take, place, upgrade, send, undo, map,
    menu, costs, done and quit; a refusal is printed and the round carries on.

    WHAT IT WRITES IS A COMMAND SCRIPT, in the content/commands.txt grammar
    record-run compiles -- so a run somebody played can be replayed, committed
    and diffed rather than being a thing that happened once at a prompt. The
    verb plays that script into a run built fresh on the same seed and shape
    and holds every round against what you were shown before writing anything,
    so a session that disagrees writes nothing and exits non-zero.

    -Out DEFAULTS INTO SCRATCH SPACE and not into the repository. A session is
    an experiment and content/ holds the run this project committed; the verb
    prints the full path it wrote to. The name carries the seed and the time of
    day, so playing one seed twice keeps both sessions.

    -Transcript reads the decisions from a file instead of from you, which is
    how an interactive verb is exercised from a cold shell -- and how a session
    somebody kept is played again.

    NOTHING HERE NEEDS AN EDITOR. No project open, no plug-in installed, no
    socket to a running Unity, which is the third working agreement in
    AGENTS.md.

    The runner references the COMMITTED simulation assembly rather than the
    simulation project, exactly as run-headless-match.ps1 does, so a source
    change committed without its rebuild goes red here rather than being
    papered over by MSBuild rebuilding sim/ on the way past.

.EXAMPLE
    ./tools/play-run-interactive.ps1
    Play the committed seed's ten waves at the prompt, and write the script
    into scratch space.

.EXAMPLE
    ./tools/play-run-interactive.ps1 -Seed 20260811 -Waves 3 -Out drafts/short.txt
    A three-wave run on another seed, written where you asked for it.

.EXAMPLE
    ./tools/play-run-interactive.ps1 -Transcript drafts/short.txt.typed -Out drafts/again.txt
    Play a session's words again with nobody at the keyboard. The same words
    either way, which is what makes a played run repeatable.
#>
param(
    # The seed the run is drawn from. Every menu in the run moves with it, so a
    # session is only repeatable against the seed it was played on. The default
    # is the one content/commands.txt was written against.
    [ulong]$Seed = 20260807,

    # Where the command script this session compiles to is written. The verb
    # requires it, because a run played at a prompt and not written down is an
    # experiment nobody can repeat.
    [string]$Out,

    # Decisions from a file instead of from the terminal.
    [string]$Transcript,

    # N and K, or zero for the runner's own defaults. Left off entirely where
    # nobody asked, so the numbers in force are the library's and there is no
    # second copy of them here to drift.
    [int]$Waves = 0,
    [int]$FieldSize = 0,

    # Keep playing after health reaches zero, which is what a harness wants and
    # a player almost never does. Here because the verb takes it and a shape
    # this script cannot reach is a shape nobody can play.
    [switch]$NoDeath,

    # Where the seven content files live. The runner takes them out of it by
    # the names it declares, so playing a whole other set of content is one
    # argument and this script names no file.
    [string]$Content,

    # One or more of the seven somewhere else: @{ map = 'maps/second.txt' }.
    # Keyed by the runner's own option names, so playing another board costs an
    # argument here rather than a retrofit across every call site.
    [hashtable]$ContentFile = @{}
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

# One culture for every number this script writes down, the name of a session
# file included: a calendar whose year is not the Gregorian one would otherwise
# name two sessions played minutes apart in two different numbering systems.
$number = [System.Globalization.CultureInfo]::InvariantCulture

if (-not $Content) { $Content = Join-Path $repoRoot 'content' }

if (-not $Out) {
    $sessions = Join-Path ([System.IO.Path]::GetTempPath()) 'simcli-played'
    $stamp = (Get-Date).ToString('yyyyMMdd-HHmmss', $number)
    $Out = Join-Path $sessions ("played-{0}-{1}.txt" -f $Seed.ToString($number), $stamp)
}

# Built into scratch space rather than into the project's own bin/, so that a
# run of this script cannot leave the working tree dirtier than it found it.
$build = Join-Path ([System.IO.Path]::GetTempPath()) 'simcli-build-play'
$program = Join-Path $build 'Sim.Cli.dll'

. (Join-Path $PSScriptRoot '_shared.ps1')

& dotnet build (Join-Path $repoRoot 'simcli') --configuration Debug --nologo --output $build | Out-Host

if ($LASTEXITCODE -ne 0) {
    throw "Building simcli failed (exit $LASTEXITCODE)."
}

$playArguments = @('play') + (Get-ContentArguments $Content $ContentFile) + @(
    '--seed', $Seed.ToString($number),
    '--out', $Out)

if ($Waves -gt 0)     { $playArguments += @('--waves', $Waves.ToString($number)) }
if ($FieldSize -gt 0) { $playArguments += @('--field-size', $FieldSize.ToString($number)) }
if ($NoDeath)         { $playArguments += @('--no-death') }
if ($Transcript)      { $playArguments += @('--transcript', $Transcript) }

# -Interactive, because the prompt this verb prints has no newline after it and
# a pipeline would hold it until one arrived. See _shared.ps1.
Invoke-SimCli $playArguments -Interactive

exit 0
