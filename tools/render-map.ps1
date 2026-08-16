<#
.SYNOPSIS
    Draws content/map.txt as a picture, so that editing the file and seeing the
    board are one loop.

.DESCRIPTION
    The shell end of simcli's draw-map verb. It builds the command-line project,
    parses the map, prints the shape and the tier census, and writes the board
    as scalable vector graphics -- one hexagon per cell, at the offsets the odd-r
    grid actually puts them, tinted and lettered by tier.

    THIS IS THE AUTHORING LOOP. The first real map is hand-drawn, with a fold and
    three tiers, and the file is the drawing surface: odd rows are indented in it
    so that what is typed already looks like the board it produces. What this adds
    is the other half -- looking at the result -- without an agent in the middle.

    THE PICTURE IS OF THE PARSED MAP. A file that will not load produces the
    loader's own refusal and no picture at all, so "is this a map yet" is
    answered by the same corridor assertion the simulation runs and not by a
    second reader that would eventually disagree with it.

    THE OUTPUT IS NOT COMMITTED. It is a view of a committed file rather than a
    generated artefact anything is checked against, which is why it lands in
    scratch space by default and why nothing in content/ regenerates it. Name
    -Out to put it somewhere you want to keep.

    NOTHING HERE NEEDS AN EDITOR. No project open, no plug-in installed, no
    socket to a running Unity, which is the third working agreement in AGENTS.md.

    The runner references the COMMITTED simulation assembly rather than the
    simulation project, exactly as run-headless-match.ps1 does, so a source
    change committed without its rebuild goes red here rather than being papered
    over by MSBuild rebuilding sim/ on the way past.

.EXAMPLE
    ./tools/render-map.ps1
    Draws content/map.txt into scratch space and prints where it went.

.EXAMPLE
    ./tools/render-map.ps1 -Show
    The same, and opens it in whatever draws an .svg on this machine.

.EXAMPLE
    ./tools/render-map.ps1 -Map drafts/folded.txt -Out drafts/folded.svg
    A draft board somewhere else. Both are parameters, so looking at one costs
    an argument rather than an edit.
#>
param(
    # The map to draw. A parameter so that a draft costs an argument.
    [string]$Map,

    # Where the picture goes. Scratch space by default: this is a view of a
    # committed file, not an artefact anything is checked against.
    [string]$Out,

    # Open it once it is written. Off by default, because a shell that opens a
    # window is a shell an overnight agent cannot run.
    [switch]$Show
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$content = Join-Path $repoRoot 'content'

if (-not $Map) { $Map = Join-Path $content 'map.txt' }
if (-not $Out) { $Out = Join-Path ([System.IO.Path]::GetTempPath()) 'map.svg' }

# Built into scratch space rather than into the project's own bin/, so that a
# run of this script cannot leave the working tree dirtier than it found it.
$build = Join-Path ([System.IO.Path]::GetTempPath()) 'simcli-build-map'
$program = Join-Path $build 'Sim.Cli.dll'

. (Join-Path $PSScriptRoot '_shared.ps1')

& dotnet build (Join-Path $repoRoot 'simcli') --configuration Debug --nologo --output $build | Out-Host

if ($LASTEXITCODE -ne 0) {
    throw "Building simcli failed (exit $LASTEXITCODE)."
}

Invoke-SimCli @(
    'draw-map',
    '--map', $Map,
    '--out', $Out)

if ($Show) {
    Start-Process $Out
}

exit 0
