<#
.SYNOPSIS
    Prints which unit follows which, and what each tier costs.

.DESCRIPTION
    The shell end of simcli's ladder verb. It builds the command-line project
    and prints one line per upgrade edge -- the source, the target, and the
    price of the target off its own row -- then what a walk over the whole
    ladder had to say: its roots, its leaves, any upgrade that is not dearer
    than what it replaces, and any fault.

    IT ALWAYS EXITS ZERO, FAULTS INCLUDED. What fails a build on a fault is a
    test in sim.tests; this reads a roster and enforces nothing. The two
    postures are deliberately in different places, because a rule with two
    homes is a rule nobody can tell which of they have met.

    TWO FILES AND NOT SEVEN. A ladder is read against the roster and against
    nothing else: a tier's price is the cost column on its own row, so there is
    no map, no schedule, no defense and no wave in this.

    NOTHING HERE NEEDS AN EDITOR. No project open, no plug-in installed, no
    socket to a running Unity, which is the third working agreement in
    AGENTS.md.

    The runner references the COMMITTED simulation assembly rather than the
    simulation project, exactly as run-headless-match.ps1 does, so a source
    change committed without its rebuild goes red here rather than being
    papered over by MSBuild rebuilding sim/ on the way past.

.EXAMPLE
    ./tools/show-ladder.ps1
    The committed ladder: its edges, then its notes, then its faults. A ladder
    with no edges in it prints nothing at all and still exits zero.

.EXAMPLE
    ./tools/show-ladder.ps1 -Upgrades drafts/second-ladder.txt
    Another ladder against the committed roster. Both files are parameters, so
    looking at a draft costs an argument rather than an edit.
#>
param(
    # The two content files. Both are parameters so that pointing this at a
    # draft roster or a draft ladder costs an argument here rather than a
    # retrofit across every call site.
    [string]$Units,
    [string]$Upgrades
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$content = Join-Path $repoRoot 'content'

if (-not $Units)    { $Units = Join-Path $content 'units.txt' }
if (-not $Upgrades) { $Upgrades = Join-Path $content 'upgrades.txt' }

# Built into scratch space rather than into the project's own bin/, so that a
# run of this script cannot leave the working tree dirtier than it found it.
$build = Join-Path ([System.IO.Path]::GetTempPath()) 'simcli-build-ladder'
$program = Join-Path $build 'Sim.Cli.dll'

. (Join-Path $PSScriptRoot '_shared.ps1')

& dotnet build (Join-Path $repoRoot 'simcli') --configuration Debug --nologo --output $build | Out-Host

if ($LASTEXITCODE -ne 0) {
    throw "Building simcli failed (exit $LASTEXITCODE)."
}

Invoke-SimCli @(
    'ladder',
    '--units', $Units,
    '--upgrades', $Upgrades)

exit 0
