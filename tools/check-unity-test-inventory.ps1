<#
.SYNOPSIS
    Asserts that the client's Unity tests are the ones written down here, and
    prints them. No editor involved.

.DESCRIPTION
    The build gate runs `dotnet test sim.tests` and no Unity test at all: those
    need a licensed editor, and that gate is the one automation in this
    repository that deliberately needs none. So the block below is where a
    reader of a green check finds out what the check did not cover, and
    `tools/run-editmode-tests.ps1` and `tools/run-playmode-tests.ps1` are where
    it gets covered instead.

    A list like that written as prose rots the first time somebody adds a test
    file: the sentence stays, the number under it stops being true, and a green
    check goes back to meaning more than it says. So the block is compared
    against the tests on disk and this exits 1 when the two disagree. Adding an
    engine-side test turns the gate red once, and the way back to green is to
    write the new test into the block -- which sits under the sentence saying
    nothing in continuous integration will run it.

    Reading is the whole mechanism. A test is counted by its `[Test]` or
    `[UnityTest]` attribute in the source text, which is the number the editor's
    own runner reports because nothing here is parameterised. Scope is TRACKED
    files under client/Assets, so a scratch fixture no clone would get does not
    go red until it is staged.

.EXAMPLE
    ./tools/check-unity-test-inventory.ps1
    Exit code 0 if the tests on disk are the tests declared here, 1 otherwise.
#>

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

# The declared inventory: test count, then path. These are the tests the build
# gate does not run.
$declaration = @'
  4  client/Assets/Tests/EditMode/BoardBakeTests.cs
  7  client/Assets/Tests/EditMode/BoardDraftTests.cs
  8  client/Assets/Tests/EditMode/BoardDressingTests.cs
  3  client/Assets/Tests/EditMode/BoardPreviewTests.cs
  5  client/Assets/Tests/EditMode/BoardSceneryTests.cs
 11  client/Assets/Tests/EditMode/EntityViewPoolTests.cs
  5  client/Assets/Tests/EditMode/GeneratedProjectFilesTests.cs
 10  client/Assets/Tests/EditMode/ImportedArtTests.cs
  4  client/Assets/Tests/EditMode/MatchContentTests.cs
  3  client/Assets/Tests/EditMode/RoadTilingMeshTests.cs
  3  client/Assets/Tests/EditMode/RosterNamesTests.cs
 10  client/Assets/Tests/EditMode/RoutePathTests.cs
  4  client/Assets/Tests/EditMode/SceneRootTests.cs
  4  client/Assets/Tests/PlayMode/BoardSceneryViewTests.cs
 17  client/Assets/Tests/PlayMode/BuildingTests.cs
 11  client/Assets/Tests/PlayMode/CameraRigTests.cs
  2  client/Assets/Tests/PlayMode/ChromeLayoutTests.cs
  8  client/Assets/Tests/PlayMode/HexFloorTests.cs
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

# Which runner covers a file, decided by the folder the editor's two test
# platforms are split along.
function Get-Runner([string]$path) {
    if ($path -match '/EditMode/') { return './tools/run-editmode-tests.ps1' }
    if ($path -match '/PlayMode/') { return './tools/run-playmode-tests.ps1' }
    return 'no runner in tools/ covers this path'
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
# One test is one [Test] or [UnityTest] at the head of a line. Attributes that
# expand into several cases -- [TestCase], [Values] -- would put this count and
# the editor's out of step, so their absence is asserted rather than assumed.
$onDisk = [ordered]@{}
foreach ($path in ($tracked | Sort-Object)) {
    $text = [System.IO.File]::ReadAllText([System.IO.Path]::Combine($repoRoot, $path))

    $expanding = [regex]::Matches($text, '(?m)^\s*\[(?:TestCase|TestCaseSource|Values|ValueSource|Combinatorial|Theory)\b')
    if ($expanding.Count -gt 0) {
        Write-Host "$path uses $($expanding.Count) case-expanding attribute(s)." -ForegroundColor Red
        Write-Host "This script counts one test per [Test] or [UnityTest], which no longer matches what the"
        Write-Host "editor reports. Teach it to expand them, or every number it prints is the wrong number."
        exit 1
    }

    $count = [regex]::Matches($text, '(?m)^\s*\[(?:Test|UnityTest)\s*[\](,]').Count
    if ($count -gt 0) { $onDisk[$path] = $count }
}

# --- Report -------------------------------------------------------------------
# Printed on the runs that PASS and not only the ones that fail. The list is the
# point: a green gate that never says what it skipped is the thing this check
# exists to stop.
$runners = @($onDisk.Keys | ForEach-Object { Get-Runner $_ } | Select-Object -Unique)

foreach ($runner in $runners) {
    $files = @($onDisk.Keys | Where-Object { (Get-Runner $_) -eq $runner })
    $tests = ($files | ForEach-Object { $onDisk[$_] } | Measure-Object -Sum).Sum

    Write-Host ""
    Write-Host "Not run by the build gate. Run them with $runner, editor closed:"
    foreach ($file in $files) {
        Write-Host ("  {0,3}  {1}" -f $onDisk[$file], $file)
    }
    Write-Host ("  {0,3} tests in {1} files" -f $tests, $files.Count) -ForegroundColor Yellow
}

# --- Compare against the declaration ------------------------------------------
$paths = @($declared.Keys) + @($onDisk.Keys) | Sort-Object -Unique
$drift = foreach ($path in $paths) {
    if (-not $onDisk.Contains($path)) {
        "  declared, but not on disk: $path ($($declared[$path]) tests)"
    }
    elseif (-not $declared.Contains($path)) {
        "  on disk, but not declared: $path ($($onDisk[$path]) tests)"
    }
    elseif ($declared[$path] -ne $onDisk[$path]) {
        "  declared $($declared[$path]) tests, found $($onDisk[$path]): $path"
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
Write-Host "$total Unity tests in $($onDisk.Count) files, all declared, none of them run here." -ForegroundColor Green
exit 0
