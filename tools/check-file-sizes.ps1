<#
.SYNOPSIS
    Fails if any tracked file is over five megabytes.

.DESCRIPTION
    This project has no large-file storage and is not getting any. Low-poly
    CC0 art is defined by not having huge textures, and a clone without the
    extension installed does not present as "you are missing a git extension",
    it presents as "the engine is broken" -- which is a bad first five minutes
    for a repository whose whole point is that a fresh clone is the same
    project as the one it came from.

    What replaces it is this: a tripwire, so that "add large-file storage
    later" is something the build TELLS me rather than something I remember.
    The failure mode being engineered out is the quiet one -- a nine-megabyte
    user guide riding along inside an art pack, noticed six months and two
    hundred clones later.

    Scope is TRACKED files. Build output, engine caches and agent worktrees are
    not the repository's business and are ignored by construction elsewhere;
    what matters is what a clone has to download.

.EXAMPLE
    ./tools/check-file-sizes.ps1
    Exit code 0 if every tracked file is under the limit, 1 otherwise.

.EXAMPLE
    ./tools/check-file-sizes.ps1 -LimitBytes 1MB
    Tighten the limit for a one-off audit without editing the file.
#>
param(
    [long]$LimitBytes = 5MB
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

# -z and a NUL split, because paths can contain anything, and because git
# quotes non-ASCII paths in its default output -- which would turn a real file
# into a path that does not exist and quietly skip it.
$tracked = (& git -C $repoRoot ls-files -z) -split "`0" | Where-Object { $_ }

if ($LASTEXITCODE -ne 0) {
    throw "git ls-files failed in $repoRoot (exit $LASTEXITCODE)."
}

if ($tracked.Count -eq 0) {
    throw "git ls-files reported no tracked files in $repoRoot. A check that inspects nothing is not a check."
}

# Sizes come from System.IO rather than Get-Item, and that is not a style
# preference. PowerShell's file provider treats a leading dot as "hidden" on
# Linux, so Get-Item without -Force finds .gitattributes on Windows and throws
# on it in continuous integration -- which is how the very first run of this
# gate went red. System.IO has no hidden-file, wildcard or provider semantics
# to differ across platforms.
$oversized = foreach ($path in $tracked) {
    $full = [System.IO.Path]::Combine($repoRoot, $path)
    if ([System.IO.File]::Exists($full)) {
        $length = [System.IO.FileInfo]::new($full).Length
        if ($length -gt $LimitBytes) {
            [pscustomobject]@{ Path = $path; Bytes = $length }
        }
    }
}

$limitMb = [math]::Round($LimitBytes / 1MB, 2)

if ($oversized) {
    Write-Host "Tracked files over $limitMb MB:" -ForegroundColor Red
    foreach ($file in $oversized | Sort-Object Bytes -Descending) {
        $mb = [math]::Round($file.Bytes / 1MB, 2)
        Write-Host ("  {0,8} MB  {1}" -f $mb, $file.Path)
    }
    Write-Host ""
    Write-Host "There is no large-file storage in this project and none is planned." -ForegroundColor Yellow
    Write-Host "Import selectively, or decide -- deliberately -- that this is the moment to add it."
    exit 1
}

Write-Host "$($tracked.Count) tracked files, none over $limitMb MB." -ForegroundColor Green
exit 0
