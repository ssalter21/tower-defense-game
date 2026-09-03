<#
.SYNOPSIS
    Fills a folder of stored rounds by playing the scripted players over a
    handful of seeds.

.DESCRIPTION
    A run's opponents are stored rounds read out of a folder (ADR-0057). A
    fresh clone has none, so every round of every run in it is the canned
    field standing in -- which is a working run and not the loop the folder
    exists for. This plays the scripted players over a few seeds and stores
    what they did, so that a checkout somebody has just cloned has opponents
    to fight.

    NOTHING IT WRITES IS COMMITTED. The folder is a runtime artefact and it is
    ignored by construction rather than by anybody remembering: see
    client/.gitignore, which covers Assets/StreamingAssets/content/pool/ and
    its .meta. Committing one would ship an arbitrary snapshot of somebody's
    afternoon as though it were authored, and every clone would be scored
    against it forever.

    EACH RUN DRAWS FROM THE FOLDER IT IS FILLING. simcli's store-run reads the
    pool before it plays, so the second run of this script meets the first
    one's rounds and the twelfth meets everybody's. What comes out is a
    population that has played against itself rather than twelve runs that each
    fought the canned field alone.

    THE PLAYER IS even-share, AND IT IS THE ONLY ONE THAT PRODUCES STORABLE
    ROUNDS. A stored round is a wall AND a wave, and neither half may be empty:
    a defense record with no towers and a wave record with no orders are both
    refused where they are read. So all-in, which builds nothing, plays a
    perfectly good run whose every round has an empty wall and stores none of
    them; and CoverThenUpgradeBot, which is the wall inside even-share, composes
    no wave at all and stores none of them either. even-share splits every purse
    between the board and the wave, so both halves of its rounds are there.

    WHAT VARIES INSTEAD IS THE CREEP. The runs are played over the seeds and
    over every walker in content/units.txt, which is what makes the population
    a spread rather than one player written down repeatedly. The ids are read
    out of the roster rather than listed here, so a creep added to the table is
    a creep this seeds with.

    THE POOL'S MEMBERS ARE BOT-PLAYED AND INHERIT EVERY CAVEAT ABOUT THE BOT.
    What lands here is what a scripted player does, which is not what a person
    does. It is a population rather than a good one.

.PARAMETER Pool
    Where the rounds go. Defaults to the client's streaming pool, which is
    where a player reads them from.

.PARAMETER Seeds
    How many seeds to play each creep on. Every seed is one run per walker in
    the roster, and every run is ten rounds at most.

.PARAMETER FirstSeed
    The seed the first run is played on. The rest count up from it.

.EXAMPLE
    ./tools/seed-pool.ps1
    Fills client/Assets/StreamingAssets/content/pool with one run per walker
    over two seeds.

.EXAMPLE
    ./tools/seed-pool.ps1 -Pool /tmp/pool -Seeds 10
    Fills a folder of your own, more deeply.
#>
param(
    [string]$Pool,
    [int]$Seeds = 2,
    [uint64]$FirstSeed = 20260903
)

$ErrorActionPreference = 'Stop'

. (Join-Path $PSScriptRoot '_shared.ps1')

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$content  = Join-Path $repoRoot 'content'

if (-not $Pool) {
    $Pool = Join-Path $repoRoot 'client/Assets/StreamingAssets/content/pool'
}

if ($Seeds -lt 1) {
    throw "-Seeds is how many seeds each creep is played on, and $Seeds is not one of them."
}

# The one scripted player whose rounds are storable. See the description above
# for why all-in and CoverThenUpgradeBot are not beside it.
$player = 'even-share'

# The walkers, read out of the roster rather than listed here, so a creep added
# to content/units.txt is a creep this seeds with.
$creeps = @(
    Get-Content (Join-Path $content 'units.txt') |
        Where-Object { $_ -match '^\s*unit\s+(?<id>\d+)\s+\S+\s+moving\b' } |
        ForEach-Object { $Matches['id'] })

if ($creeps.Count -eq 0) {
    throw "content/units.txt has no walker in it, so there is no wave for a scripted player to compose."
}

$build = Join-Path ([System.IO.Path]::GetTempPath()) 'simcli-seed-pool'

Write-Host "Building simcli into $build"

& dotnet build (Join-Path $repoRoot 'simcli') --configuration Debug --nologo --output $build | Out-Null
if ($LASTEXITCODE -ne 0) { throw "Building simcli failed (exit $LASTEXITCODE)." }

$runner = Join-Path $build 'Sim.Cli.dll'
if (-not (Test-Path $runner)) { throw "simcli built but $runner is not there." }

New-Item -ItemType Directory -Force $Pool | Out-Null

$before = @(Get-ChildItem -Path $Pool -Filter '*.round' -ErrorAction SilentlyContinue).Count

foreach ($creep in $creeps) {
    for ($index = 0; $index -lt $Seeds; $index++) {
        $seed = $FirstSeed + [uint64]$index

        Write-Host ""
        Write-Host "$player on creep $creep, seed $seed" -ForegroundColor Cyan

        $arguments = @(
            'store-run',
            '--pool', $Pool,
            '--seed', $seed.ToString([System.Globalization.CultureInfo]::InvariantCulture),
            '--policy', $player,
            '--creep', $creep) + (Get-ContentArguments -Directory $content)

        & dotnet $runner $arguments

        if ($LASTEXITCODE -ne 0) {
            throw "store-run refused (creep $creep, seed $seed, exit $LASTEXITCODE); its reason is above."
        }
    }
}

$after = @(Get-ChildItem -Path $Pool -Filter '*.round').Count

Write-Host ""
Write-Host "$Pool holds $after stored rounds ($($after - $before) added)." -ForegroundColor Green
Write-Host "Nothing here is committed. See client/.gitignore."

exit 0
