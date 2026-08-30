<#
.SYNOPSIS
    Asserts that the client's Unity tests are the ones written down here, and
    prints them. No editor involved.

.DESCRIPTION
    The build gate runs `dotnet test sim.tests` and no Unity test at all: both
    of the editor's test platforms need a licensed editor, and that gate is the
    one automation in this repository that deliberately needs none. So the block
    below is where a reader of a green check finds out what the check did not
    cover, and `tools/run-editmode-tests.ps1` and `tools/run-playmode-tests.ps1`
    are where it gets covered instead.

    A list like that written as prose rots the first time somebody adds a test
    file: the sentence stays, the number under it stops being true, and a green
    check goes back to meaning more than it says. So the block is compared
    against the tests on disk and this exits 1 when the two disagree. Adding an
    engine-side test turns the gate red once, and the way back to green is to
    write the new test into the block -- which sits under the sentence saying
    nothing in continuous integration will run it.

    WHAT THE NUMBERS COUNT, WHICH IS NOT WHAT THE EDITOR REPORTS. One test
    METHOD per `[Test]` or `[UnityTest]`, read out of the source text. A method
    whose parameters carry `[Values]` or `[ValueSource]` runs as one case per
    value, so the editor's own totals are at least these and can be larger --
    `RoadTilingMeshTests` is four methods and nine cases. Counting cases would
    mean resolving the arrays they come from, which is a C# compiler's job.
    Methods are what can be counted honestly by reading, so methods are the
    unit. An attribute that declares a test method WITHOUT `[Test]` is a
    different matter, because a method written that way is counted as nothing at
    all, and this refuses rather than undercounting if one ever appears.

    Scope is TRACKED files under client/Assets, so a scratch fixture no clone
    would get does not go red until it is staged.

.EXAMPLE
    ./tools/check-unity-test-inventory.ps1
    Exit code 0 if the tests on disk are the tests declared here, 1 otherwise.
#>

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

# The declared inventory: test methods, then path. These are the tests the
# build gate does not run.
$declaration = @'
  4  client/Assets/Tests/EditMode/BoardBakeTests.cs
  7  client/Assets/Tests/EditMode/BoardDraftTests.cs
  8  client/Assets/Tests/EditMode/BoardDressingTests.cs
  3  client/Assets/Tests/EditMode/BoardPreviewTests.cs
  5  client/Assets/Tests/EditMode/BoardSceneryTests.cs
 11  client/Assets/Tests/EditMode/EntityViewPoolTests.cs
  6  client/Assets/Tests/EditMode/GeneratedProjectFilesTests.cs
 10  client/Assets/Tests/EditMode/ImportedArtTests.cs
  4  client/Assets/Tests/EditMode/MatchContentTests.cs
  4  client/Assets/Tests/EditMode/RoadTilingMeshTests.cs
  3  client/Assets/Tests/EditMode/RosterNamesTests.cs
 10  client/Assets/Tests/EditMode/RoutePathTests.cs
  4  client/Assets/Tests/EditMode/SceneRootTests.cs
  4  client/Assets/Tests/PlayMode/BoardSceneryViewTests.cs
 17  client/Assets/Tests/PlayMode/BuildingTests.cs
 11  client/Assets/Tests/PlayMode/CameraRigTests.cs
  2  client/Assets/Tests/PlayMode/ChromeLayoutTests.cs
  8  client/Assets/Tests/PlayMode/HexFloorTests.cs
  5  client/Assets/Tests/PlayMode/HorizonTests.cs
  7  client/Assets/Tests/PlayMode/HexPickingTests.cs
  2  client/Assets/Tests/PlayMode/LocomotionTests.cs
 20  client/Assets/Tests/PlayMode/MatchViewTests.cs
  2  client/Assets/Tests/PlayMode/ParityRunTests.cs
  4  client/Assets/Tests/PlayMode/PlayableHeadPoisonTests.cs
 10  client/Assets/Tests/PlayMode/PlayablesSamplingTests.cs
  5  client/Assets/Tests/PlayMode/PlaybackTests.cs
  3  client/Assets/Tests/PlayMode/RealRigSamplingTests.cs
 14  client/Assets/Tests/PlayMode/RunLoopTests.cs
  2  client/Assets/Tests/PlayMode/SimPluginTests.cs
 21  client/Assets/Tests/PlayMode/WaveTests.cs
  4  client/Assets/Tests/PlayMode/WeaponSocketTests.cs
'@

# The runners this script sends a reader to, named once and checked, so a
# renamed runner is a red gate rather than a printed path that goes nowhere.
$editModeRunner = './tools/run-editmode-tests.ps1'
$playModeRunner = './tools/run-playmode-tests.ps1'
$bothRunner     = './tools/run-unity-tests.ps1'

foreach ($runner in @($editModeRunner, $playModeRunner, $bothRunner)) {
    $full = [System.IO.Path]::Combine($repoRoot, ($runner -replace '^\./', ''))
    if (-not [System.IO.File]::Exists($full)) {
        Write-Host "This script sends readers to $runner, and there is no such file." -ForegroundColor Red
        Write-Host "Point it at whatever the runner is called now, or the tests it names have nowhere to run."
        exit 1
    }
}

# Which runner covers a file, decided by the folder the editor's two test
# platforms are split along. Empty for a test file kept anywhere else, whose
# platform is a property of its assembly definition rather than of its path.
function Get-Runner([string]$path) {
    if ($path -match '/EditMode/') { return $editModeRunner }
    if ($path -match '/PlayMode/') { return $playModeRunner }
    return ''
}

$declared = [ordered]@{}
foreach ($line in ($declaration -split "`n")) {
    $entry = [regex]::Match($line, '^\s*(\d+)\s+(\S+)\s*$')
    if ($entry.Success) { $declared[$entry.Groups[2].Value] = [int]$entry.Groups[1].Value }
}

# -z and a NUL split, because git quotes non-ASCII paths in its default output,
# and a quoted path is a path that does not exist.
$tracked = (& git -C $repoRoot ls-files -z -- 'client/Assets') -split "`0" | Where-Object { $_ -like '*.cs' }

if ($LASTEXITCODE -ne 0) {
    throw "git ls-files failed in $repoRoot (exit $LASTEXITCODE)."
}

if ($tracked.Count -eq 0) {
    throw "git ls-files reported no C# under client/Assets. A check that inspects nothing is not a check."
}

# System.IO rather than Get-Content, for the reason check-file-sizes.ps1 gives:
# PowerShell's file provider has hidden-file and wildcard semantics that differ
# between Windows and the Linux runner, and System.IO has none of them.
#
# The refusal below is matched ANYWHERE in the file rather than at the head of a
# line. NUnit attributes are written both ways -- above a method and inline on
# one of its parameters -- and an anchored pattern sees only the first, which is
# how one sits in the tree unnoticed by the check meant to notice it. Every
# offender is collected before anything is reported, because the second one is
# no less interesting than the first.
$onDisk = [ordered]@{}
$uncountable = foreach ($path in ($tracked | Sort-Object)) {
    $text = [System.IO.File]::ReadAllText([System.IO.Path]::Combine($repoRoot, $path))

    $hidden = [regex]::Matches($text, '\[(?:TestCase|TestCaseSource|Theory)\b')
    if ($hidden.Count -gt 0) { "  $path uses $($hidden.Count) of them" }

    $count = [regex]::Matches($text, '(?m)^\s*\[(?:Test|UnityTest)\s*[\](,]').Count
    if ($count -gt 0) { $onDisk[$path] = $count }
}

if ($uncountable) {
    Write-Host "A test method is declared here without [Test] or [UnityTest]:" -ForegroundColor Red
    $uncountable | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    Write-Host ""
    Write-Host "This script counts one test method per [Test] or [UnityTest], so a method declared any"
    Write-Host "other way is counted as none, and the inventory it prints is short by however many there"
    Write-Host "are. Teach it to see them before the number it publishes goes back to being decorative."
    exit 1
}

# --- Report -------------------------------------------------------------------
# Printed on the runs that PASS and not only the ones that fail. The list is the
# point: a green gate that never says what it skipped is the thing this check
# exists to stop.
foreach ($runner in @($onDisk.Keys | ForEach-Object { Get-Runner $_ } | Select-Object -Unique)) {
    $files = @($onDisk.Keys | Where-Object { (Get-Runner $_) -eq $runner })
    $tests = ($files | ForEach-Object { $onDisk[$_] } | Measure-Object -Sum).Sum

    $where = if ($runner) {
        "Run them with $runner, editor closed"
    } else {
        "Outside Tests/EditMode and Tests/PlayMode, so which platform runs them is their assembly definition's business"
    }

    Write-Host ""
    Write-Host "Not run by the build gate. ${where}:"
    foreach ($file in $files) {
        Write-Host ("  {0,3}  {1}" -f $onDisk[$file], $file)
    }
    Write-Host ("  {0,3} test methods in {1} files" -f $tests, $files.Count) -ForegroundColor Yellow
}

# --- Compare against the declaration ------------------------------------------
$paths = @($declared.Keys) + @($onDisk.Keys) | Sort-Object -Unique
$drift = foreach ($path in $paths) {
    if (-not $onDisk.Contains($path)) {
        "  declared, but not on disk: $path ($($declared[$path]) test methods)"
    }
    elseif (-not $declared.Contains($path)) {
        "  on disk, but not declared: $path ($($onDisk[$path]) test methods)"
    }
    elseif ($declared[$path] -ne $onDisk[$path]) {
        "  declared $($declared[$path]) test methods, found $($onDisk[$path]): $path"
    }
}

Write-Host ""

if ($drift) {
    Write-Host "The Unity tests on disk are not the ones this script declares:" -ForegroundColor Red
    $drift | ForEach-Object { Write-Host $_ -ForegroundColor Red }
    Write-Host ""
    Write-Host "No automation runs these tests, so the declaration is the only place a green" -ForegroundColor Yellow
    Write-Host "build gate admits they exist. Replace the block in this script with:"
    Write-Host ""
    foreach ($path in $onDisk.Keys) {
        Write-Host ("{0,3}  {1}" -f $onDisk[$path], $path)
    }
    exit 1
}

$total = ($onDisk.Values | Measure-Object -Sum).Sum
Write-Host "$total Unity test methods in $($onDisk.Count) files, all declared, none of them run here." -ForegroundColor Green
Write-Host "Either platform also runs from $bothRunner -Platform EditMode or -Platform PlayMode."
exit 0
