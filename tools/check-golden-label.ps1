<#
.SYNOPSIS
    Fails if this branch changes a golden artefact and its pull request does
    not carry the `regenerated-deliberately` label.

.DESCRIPTION
    The golden artefacts are the files a run is compared against: the trace,
    the landmark table, the historical results under content/golden/, the
    sweep, the run outcome, and the replay and command list the whole thing is
    played from. Every other gate step asks whether the simulation still
    produces them. None of them can ask whether they were supposed to move,
    because a change to the rules plus a regeneration produces a green gate on
    all six matrix rows and nothing marks it -- and a gate that could go red on
    a deliberate content change would be a gate nobody could ever satisfy.

    So this asks nothing about the regeneration itself. It requires that a
    person said, in the one place a person can say it, that they read the diff:
    the label is the sentence and the pull request is where it goes. The
    reasoning is on the gate step in .github/workflows/build-gate.yml.

    The lookup is by head branch, because the gate runs `on: push` and a push
    knows its branch and not its pull request. A branch with no open pull
    request yet is refused: there is nowhere to put the label, and pushing the
    regeneration before opening the pull request is exactly the order in which
    it goes unread. `main` is skipped, since `main` only moves by merge and the
    label was already required on the branch that merged.

    The comparison is against the merge base with origin/main, not against its
    tip, so a golden file that moved on main after this branch was cut is not
    read as this branch touching it. That needs real history: a shallow
    checkout has no merge base, which is why the gate's checkout asks for all
    of it.

    A branch cut from another branch that has already moved a golden inherits
    that diff and is asked for a label of its own. That is the direction this
    errs in deliberately -- an attention gate is worth more as a loud false
    positive than as a quiet miss -- and -BaseRef is how the same question gets
    asked against the branch it was really cut from.

.EXAMPLE
    ./tools/check-golden-label.ps1
    Exit code 0 if nothing golden moved, or if it moved and the branch's open
    pull request is labelled. 1 otherwise.

.EXAMPLE
    ./tools/check-golden-label.ps1 -Branch effort/first-queue
    Name the pushed branch when the local one is called something else -- an
    agent worktree's copy of an effort branch, say.

.EXAMPLE
    ./tools/check-golden-label.ps1 -BaseRef origin/effort/first-queue
    Ask only what this branch moved on top of the branch it was cut from.
#>
param(
    [string]$Branch,
    [string]$BaseRef = 'origin/main'
)

$ErrorActionPreference = 'Stop'

$label = 'regenerated-deliberately'
$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

# The artefacts a run is verified against. Directories are listed as
# directories, so a golden result added under content/golden/ is covered on
# the day it is written rather than on the day somebody remembers this file.
$goldenPaths = @(
    'content/golden-trace.txt'
    'content/landmarks.txt'
    'content/golden/'
    'content/sweep.csv'
    'content/run-outcome.txt'
    'content/match.replay'
    'content/run.commands'
)

if (-not $Branch) {
    # GITHUB_REF_NAME is the pushed branch on a runner. Off a runner the
    # checkout's own branch is the best guess, and -Branch overrides it.
    $Branch = if ($env:GITHUB_REF_NAME) { $env:GITHUB_REF_NAME }
              else { (& git -C $repoRoot rev-parse --abbrev-ref HEAD) }
}

if (-not $Branch -or $Branch -eq 'HEAD') {
    Write-Host "This checkout is not on a branch, so there is no pull request to look for." -ForegroundColor Red
    Write-Host "Name the pushed branch: ./tools/check-golden-label.ps1 -Branch <branch>"
    exit 1
}

if ($Branch -eq 'main') {
    Write-Host "On main, which only moves by merge -- the label was required on the branch that merged." -ForegroundColor Green
    exit 0
}

& git -C $repoRoot rev-parse --verify --quiet "$BaseRef^{commit}" > $null
if ($LASTEXITCODE -ne 0) {
    Write-Host "$BaseRef is not in this checkout, so there is nothing to compare against." -ForegroundColor Red
    Write-Host "Fetch it (git fetch origin main) and run this again."
    exit 1
}

$touched = (& git -C $repoRoot diff --name-only "$BaseRef...HEAD" -- @goldenPaths) |
           Where-Object { $_ }

if ($LASTEXITCODE -ne 0) {
    throw "git diff against $BaseRef failed in $repoRoot (exit $LASTEXITCODE)."
}

if (-not $touched) {
    Write-Host "No golden artefact changed on $Branch against $BaseRef." -ForegroundColor Green
    Write-Host "Watched: $($goldenPaths -join ', ')"
    exit 0
}

Write-Host "Golden artefacts changed on $Branch against ${BaseRef}:" -ForegroundColor Yellow
foreach ($path in $touched) { Write-Host "  $path" }
Write-Host ""

# gh's own diagnostics stay on stderr and go to the console. Folding them into
# this capture would put an ErrorRecord in among the JSON, and any notice gh
# felt like printing would come back as a parse failure on a branch that is
# perfectly well labelled.
$prJson = & gh pr list --head $Branch --state open --json number,labels

if ($LASTEXITCODE -ne 0) {
    Write-Host "Could not ask GitHub which pull request has head branch '$Branch' (gh exit $LASTEXITCODE); its reason is above." -ForegroundColor Red
    exit 1
}

$pulls = $prJson | ConvertFrom-Json

if (-not $pulls) {
    Write-Host "No open pull request has head branch '$Branch', so there is nowhere to put the label." -ForegroundColor Red
    Write-Host "Open the pull request for this branch, read the diff above, and label it '$label'."
    Write-Host "Then re-run this job: the gate runs on push, and labelling does not start it again."
    exit 1
}

$labelled = @($pulls | Where-Object { $_.labels.name -contains $label })

if (-not $labelled) {
    $unlabelled = ($pulls | ForEach-Object { "#$($_.number)" }) -join ', '
    Write-Host "$unlabelled does not carry the label '$label', and a regenerated golden artefact is a diff a person reads." -ForegroundColor Red
    Write-Host "Read the files above, then: gh pr edit $($pulls[0].number) --add-label $label"
    Write-Host "Then re-run this job: the gate runs on push, and labelling does not start it again."
    exit 1
}

$read = ($labelled | ForEach-Object { "#$($_.number)" }) -join ', '
Write-Host "$read carries '$label', so somebody has read this." -ForegroundColor Green
exit 0
