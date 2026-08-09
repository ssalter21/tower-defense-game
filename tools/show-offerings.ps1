<#
.SYNOPSIS
    Prints every wave's public menu for one run seed.

.DESCRIPTION
    The shell end of simcli's offerings verb. It builds the command-line
    project and prints, wave by wave, the ordinary options and the game
    changers a run on that seed is offered.

    THIS IS WHAT A COMMAND SCRIPT IS WRITTEN FROM. A build row in
    content/commands.txt takes a kind and an id off the menu standing in front
    of that wave, so the menus have to be readable before the script that names
    them exists. The seed here and the -RunSeed of run-headless-match.ps1 are
    the same number: a different one is a different set of menus, and every take
    in the script then names an option nobody was offered.

    NOTHING HERE NEEDS AN EDITOR. No project open, no plug-in installed, no
    socket to a running Unity, which is the third working agreement in
    AGENTS.md.

    The runner references the COMMITTED simulation assembly rather than the
    simulation project, exactly as run-headless-match.ps1 does, so a source
    change committed without its rebuild goes red here rather than being
    papered over by MSBuild rebuilding sim/ on the way past.

.EXAMPLE
    ./tools/show-offerings.ps1
    The menus of the committed run, on the seed content/run.commands carries.

.EXAMPLE
    ./tools/show-offerings.ps1 -Seed 12345 -Waves 20
    Another run, twenty waves long.

.EXAMPLE
    ./tools/show-offerings.ps1 -ContentFile @{ schedule = 'maps/second-schedule.txt' }
    Another shape. Any of the seven content files can be pointed somewhere else
    by the option the runner declares for it, so this is an argument rather than
    an edit.
#>
param(
    # The seed every offering, filling and field in the run is derived from.
    # The committed command stream's own, so that running this with no argument
    # prints the menus content/commands.txt was written against.
    [ulong]$Seed = 20260807,

    # N and K. Ten and ten are this map's answers and both are expected to move.
    [int]$Waves = 10,
    [int]$FieldSize = 10,

    # Where the seven content files live. The runner takes them out of it by the
    # names it declares, so this script names no file of its own.
    [string]$Content,

    # One or more of the seven somewhere else, keyed by the runner's own option
    # names: @{ schedule = 'maps/second-schedule.txt' }.
    [hashtable]$ContentFile = @{}
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

if (-not $Content) { $Content = Join-Path $repoRoot 'content' }

# Built into scratch space rather than into the project's own bin/, so that a
# run of this script cannot leave the working tree dirtier than it found it.
$build = Join-Path ([System.IO.Path]::GetTempPath()) 'simcli-build-offerings'
$program = Join-Path $build 'Sim.Cli.dll'

. (Join-Path $PSScriptRoot '_shared.ps1')

& dotnet build (Join-Path $repoRoot 'simcli') --configuration Debug --nologo --output $build | Out-Host

if ($LASTEXITCODE -ne 0) {
    throw "Building simcli failed (exit $LASTEXITCODE)."
}

$number = [System.Globalization.CultureInfo]::InvariantCulture

Invoke-SimCli (@('offerings') + (Get-ContentArguments $Content $ContentFile) + @(
    '--seed', $Seed.ToString($number),
    '--waves', $Waves.ToString($number),
    '--field-size', $FieldSize.ToString($number)))

exit 0
