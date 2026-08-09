<#
.SYNOPSIS
    Copies the authored content files into the client's streaming assets.

.DESCRIPTION
    The authored content lives in content/ at the repository root, where the
    simulation's tests, the headless command line and a human editing a map all
    reach it by a relative path. A double-clickable build has no such path: the
    player is a folder somebody unzipped, and ../../content/map.txt is not
    there and is not going to be.

    Assets/StreamingAssets/ is the folder Unity copies into the player
    verbatim, so that is where the content has to be. This script puts it
    there, and the copy is COMMITTED -- exactly like a lockfile. Same reasoning
    as every other generated file in this repository: a fresh clone has to be
    the same project as the one it was cloned from, and a build that regenerates
    content at build time is a build whose content nobody has ever looked at.

    The authored original is the source of truth in both directions: this script
    only ever writes from content/ into StreamingAssets/, never back. An
    edit-mode test (Tests.EditMode/StreamingContentTests) fails if the two have
    drifted, so forgetting to run this is a red test rather than a mystery.

    Files are added to the list below by the ticket that first needs one inside
    the player. The floor needs the map; nothing in the client reads the rest
    yet, and shipping content nobody reads is shipping content nobody checks.

.EXAMPLE
    ./tools/sync-streaming-content.ps1
    Rewrites the copies. Commit whatever it changed, in the same commit as the
    content edit that caused it.

.EXAMPLE
    ./tools/sync-streaming-content.ps1 -Check
    Writes nothing; exits 1 and names the files if any copy is stale. This is
    the shell-side form of the edit-mode test, for a gate that has no engine.
#>
param(
    [switch]$Check
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$source   = Join-Path $repoRoot 'content'
$target   = Join-Path $repoRoot 'client/Assets/StreamingAssets/content'

# The content a running player has to be able to read. See the note above about
# how this list grows.
#
# The floor needed only the map. Drawing the MATCH needs the other three: the
# type table, the defense the towers are built from and the wave that walks the
# corridor. The list is mirrored by StreamingContent.MatchFileNames, and an
# edit-mode test fails if the two disagree -- a file in one and not the other is
# either content that ships and is never read, or content that is read and does
# not ship, and the second presents as an empty playfield in a build that worked
# perfectly in the editor. See the note on upgrades.txt below for a third case
# this list has since grown.
#
# match.replay joined the list when the player stopped deriving its match from
# the four text files and started playing the RECORD. The seed is in there and
# nowhere else on the view side, which is what makes the tick numbers in
# docs/sit-down.md mean something in the build: they came from a real run of
# these exact bytes. A player carrying its own seed instead had one eleven ticks
# out from the committed landmark table, and nothing on screen looked wrong.
#
# The tick loop resolves every landing through ruleset.txt -- the damage matrix,
# the armour expression and the floor -- so a player without it draws its floor
# and throws on the first hit.
#
# upgrades.txt is an HONEST THIRD CASE, and it is worth naming because it is
# neither of the two the paragraph above catches. Nothing on the view side reads
# an upgrade edge. But the ladder is folded into the unit table's content hash,
# and that hash is what the replay gate compares the shipped record's stamped one
# against -- so a player without this file rebuilds the wrong hash and
# match.replay is refused. It ships because a hash covers it.
$files = @(
    'map.txt',
    'units.txt',
    'upgrades.txt',
    'ruleset.txt',
    'defense.txt',
    'wave.txt',
    'match.replay'
)

if (-not (Test-Path $source)) { throw "No authored content at $source." }

if (-not $Check -and -not (Test-Path $target)) {
    $null = New-Item -ItemType Directory -Path $target -Force
}

# SHA256 over bytes rather than a text comparison, because the interesting
# drift includes line endings: git can be configured to rewrite them, and a
# copy that differs only in CRLF is still a copy the parser sees differently.
function Get-Sha([string]$path) {
    if (-not (Test-Path -LiteralPath $path -PathType Leaf)) { return $null }
    return (Get-FileHash -LiteralPath $path -Algorithm SHA256).Hash
}

$stale = @()

foreach ($file in $files) {
    $from = Join-Path $source $file
    $to   = Join-Path $target $file

    if (-not (Test-Path -LiteralPath $from -PathType Leaf)) {
        throw "$from is in the sync list but does not exist."
    }

    if ((Get-Sha $from) -eq (Get-Sha $to)) {
        Write-Host "  up to date  $file"
        continue
    }

    if ($Check) {
        $stale += $file
        continue
    }

    Copy-Item -LiteralPath $from -Destination $to -Force
    Write-Host "  written     $file" -ForegroundColor Yellow
}

# A file that was dropped from the list, or renamed, would otherwise sit in the
# player forever. Reported rather than deleted: this script writes what the
# list says and nothing else, and a script that deletes files it did not write
# is a script somebody will one day point at the wrong folder.
if (Test-Path $target) {
    foreach ($extra in Get-ChildItem -LiteralPath $target -File) {
        if ($extra.Extension -eq '.meta') { continue }
        if ($files -notcontains $extra.Name) {
            Write-Host "  STRAY       $($extra.Name) -- not in the sync list. Delete it, or add it." -ForegroundColor Red
            $stale += $extra.Name
        }
    }
}

if ($stale.Count) {
    Write-Host ""
    Write-Host "The streaming copy is out of date:" -ForegroundColor Red
    foreach ($file in ($stale | Sort-Object -Unique)) { Write-Host "  $file" }
    Write-Host ""
    Write-Host "Run ./tools/sync-streaming-content.ps1 and commit what it writes."
    exit 1
}

Write-Host "streaming content is in sync with content/." -ForegroundColor Green
exit 0
