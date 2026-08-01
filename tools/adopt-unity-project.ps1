<#
.SYNOPSIS
    Moves a Unity-Hub-created project into this repo so that client/ IS the
    Unity project root.

.DESCRIPTION
    Issue #15 decided that client/ is the Unity project root itself --
    client/Assets, client/Packages, client/ProjectSettings -- not a folder
    containing a project. That is what Part III's diagram means, and it is the
    relative path issue #5's research actually verified: a sibling sim/
    resolves as ../../sim from Packages/.

    Unity Hub will not create a project into client/ directly, because the
    folder already exists (it holds the .gitignore that has to be in place
    BEFORE the first Editor launch). So the sequence is: create the project
    somewhere scratch, then run this to adopt it.

    Doing the move by hand is easy to get subtly wrong -- one extra level of
    nesting and every relative path in the architecture gains a hop, which is
    not obvious until much later.

.EXAMPLE
    ./tools/adopt-unity-project.ps1 -From C:\Users\salte\UnityProjects\client
#>
param(
    [Parameter(Mandatory = $true)][string]$From,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'

$RepoRoot = (Get-Item $PSScriptRoot).Parent.FullName
$Client   = Join-Path $RepoRoot 'client'

if (-not (Test-Path $From)) { throw "Source project not found: $From" }
$Src = (Get-Item $From).FullName

# --- Is the source actually a Unity project root? -------------------------
$required = @('Assets', 'ProjectSettings')
$missing  = $required | Where-Object { -not (Test-Path (Join-Path $Src $_)) }
if ($missing) {
    $nested = Get-ChildItem $Src -Directory |
        Where-Object { Test-Path (Join-Path $_.FullName 'ProjectSettings') } |
        Select-Object -First 1
    $hint = if ($nested) {
        "`nDid you mean -From `"$($nested.FullName)`"? Unity Hub nests the project " +
        "inside a folder named after it, which is the exact trap this script exists for."
    } else { "" }
    throw "$Src is not a Unity project root (missing: $($missing -join ', '))." + $hint
}

# --- Is the destination safe to move into? --------------------------------
if (-not (Test-Path $Client)) { throw "No client/ directory at $Client. Is this the right repo?" }

$occupied = Get-ChildItem $Client -Force | Where-Object { $_.Name -ne '.gitignore' }
if ($occupied -and -not $Force) {
    throw ("client/ already contains: $($occupied.Name -join ', ')`n" +
           "Expected it to hold nothing but .gitignore. Re-run with -Force only if " +
           "you are certain you want to merge into it.")
}

if (-not (Test-Path (Join-Path $Client '.gitignore'))) {
    throw ("client/.gitignore is missing. It must be in place BEFORE Unity's files " +
           "arrive, or client/Library -- gigabytes of machine-local cache -- lands in git. " +
           "Merge the git-hygiene PR first.")
}

# --- Move --------------------------------------------------------------------
Write-Host "Adopting $Src" -ForegroundColor Cyan
Write-Host "     into $Client"
Write-Host ""

foreach ($item in Get-ChildItem $Src -Force) {
    $dest = Join-Path $Client $item.Name
    if (Test-Path $dest) {
        Write-Warning "skipped $($item.Name) -- already exists at destination"
        continue
    }
    Move-Item -LiteralPath $item.FullName -Destination $dest
    Write-Host "  moved $($item.Name)"
}

$leftover = Get-ChildItem $Src -Force -ErrorAction SilentlyContinue
if (-not $leftover) {
    Remove-Item $Src -Force
    Write-Host "  removed now-empty $Src"
}

# --- Verify -------------------------------------------------------------------
Write-Host ""
$checks = [ordered]@{
    'client/Assets'                       = Join-Path $Client 'Assets'
    'client/Packages/manifest.json'       = Join-Path $Client 'Packages/manifest.json'
    'client/Packages/packages-lock.json'  = Join-Path $Client 'Packages/packages-lock.json'
    'client/ProjectSettings/ProjectVersion.txt' = Join-Path $Client 'ProjectSettings/ProjectVersion.txt'
}
$ok = $true
foreach ($k in $checks.Keys) {
    if (Test-Path $checks[$k]) { Write-Host "  OK    $k" -ForegroundColor Green }
    else { Write-Host "  MISS  $k" -ForegroundColor Yellow; $ok = $false }
}

$pv = Join-Path $Client 'ProjectSettings/ProjectVersion.txt'
if (Test-Path $pv) {
    Write-Host ""
    Write-Host "Editor version recorded in the project:" -ForegroundColor Cyan
    Get-Content $pv | ForEach-Object { Write-Host "  $_" }
}

# --- The check that actually matters -----------------------------------------
Write-Host ""
Push-Location $RepoRoot
try {
    $lib = git status --porcelain --untracked-files=all -- client/Library 2>$null
    if ($lib) {
        Write-Host "PROBLEM: client/Library/ is visible to git." -ForegroundColor Red
        Write-Host "Do not commit. client/.gitignore is not doing its job."
    } else {
        Write-Host "client/Library/ is correctly invisible to git." -ForegroundColor Green
    }
    $n = (git status --porcelain --untracked-files=all -- client | Measure-Object).Count
    Write-Host "$n path(s) under client/ are now visible to git."
    Write-Host ""
    Write-Host "Review with:  git status --short client" -ForegroundColor Cyan
}
finally { Pop-Location }

if (-not $ok) {
    Write-Host ""
    Write-Warning "Some expected files were missing. If packages-lock.json is absent, open the project in Unity once and it will be generated."
}
