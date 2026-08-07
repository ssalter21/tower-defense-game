<#
.SYNOPSIS
    Plays the committed replay bundle with nobody watching, and writes down
    what happened.

.DESCRIPTION
    The shell end of simcli, which is the headless runner. It builds the
    command-line project, plays content/match.replay to the end, and prints the
    result triple, the final rolling hash and the landmark table.

    NOTHING HERE NEEDS AN EDITOR. No project open, no plug-in installed, no
    socket to a running Unity -- which is the third working agreement in
    CLAUDE.md, and the reason a fresh clone, a continuous-integration runner and
    an overnight agent can all run this from nothing.

    The runner references the COMMITTED simulation assembly rather than the
    simulation project, exactly as the test project does. A source change
    committed without its rebuild therefore goes red here rather than being
    papered over by MSBuild rebuilding sim/ on the way past.

    -Simulation NAMES WHICH IMAGE IS PLAYED, and Committed is the default, so
    every ordinary invocation is the paragraph above. The other two build sim/
    from source at a named configuration and play that instead, which is what
    the determinism matrix's Release rows need: the committed image is a Debug
    build, deliberately and permanently, so a Release row that played it would
    be a check that cannot fail. A fresh image is built into scratch space and
    never into the committed plug-in folder -- overwriting the artefact under
    test is the same mistake in a different coat.

    Two committed files fall out of a run: content/golden-trace.txt, the
    rolling per-tick state hash, and content/landmarks.txt, the handful of ticks
    the sit-down checklist is written against. -Verify proves the committed
    copies are still what a run produces; -Regenerate makes them so again after
    a deliberate content change.

    IT ALSO REPLAYS EVERY HISTORICAL FORMAT VERSION. content/golden/ holds one
    tiny bundle per defense record format version that has ever shipped, each
    beside the result a real run of it produced. Those files are kept forever:
    the writer emits only the current version, so the older ones can never be
    made again, and they are the only evidence that the reader branch for a
    version still works. Deleting a reader branch turns the golden for that
    version red, and the runner's refusal names the version.

    EACH GOLDEN IS PLAYED AGAINST THE TABLE PINNED BESIDE IT, not against
    content/units.txt. defense-N.units is the table defense-N was recorded
    against, and its hash is the one stamped in that bundle's header, which is
    what the replay gate compares. Played against the live table instead, every
    one of these files would be refused by the first retune and none of them
    could be made again. -Regenerate pins a fresh copy beside the bundle it
    re-records and leaves the older ones alone.

.EXAMPLE
    ./tools/run-headless-match.ps1
    Play the committed match and print what happened.

.EXAMPLE
    ./tools/run-headless-match.ps1 -Out artefacts
    Write the trace and the landmark table into artefacts/ as well.

.EXAMPLE
    ./tools/run-headless-match.ps1 -Verify
    Exit 0 if a run of the committed bundle still produces the committed trace
    and the committed landmarks, and 1 naming the first difference if it does
    not. This is what the build gate runs.

.EXAMPLE
    ./tools/run-headless-match.ps1 -Verify -Simulation FreshRelease
    Build sim/ from source with the optimiser on, play the committed bundle
    with that image, and require the same trace, the same landmarks and the
    same golden results as the Debug bytes in the repository produce. One row
    of the determinism matrix; the build gate runs six.

.EXAMPLE
    ./tools/run-headless-match.ps1 -Regenerate
    Re-record content/match.replay from the content files and rewrite both
    committed artefacts from a real run of it. The one thing to do after a
    deliberate content change, and the reason the landmark table cannot go
    quietly stale.
#>
param(
    [string]$Out,
    [switch]$Verify,
    [switch]$Regenerate,

    # The seed the committed bundle carries. It lives in the match record
    # rather than in the defense, so changing the dice does not change what a
    # defense is -- and it is only needed here when re-recording the bundle,
    # because every run after that reads it out of the bundle's own bytes.
    [ulong]$Seed = 20260801,

    # Which map the recorded defense claims to be on. A handle for looking a
    # map up, and NOT what pins the geometry -- that is the map hash, which is
    # computed from the parsed grid and checked at the replay gate. Handles are
    # assigned by whatever stores maps; zero means "this record does not say",
    # and content/map.txt is the one map the skeleton ships, so it is one.
    [int]$MapHandle = 1,

    # Which simulation image to play. Committed is the bytes in the repository
    # -- the ones the engine loads as a plug-in, and the only ones anybody has
    # checked -- and it is the default for exactly that reason. The other two
    # build sim/ from source and play the result, which is the only way to run
    # a configuration the repository does not commit.
    [ValidateSet('Committed', 'FreshDebug', 'FreshRelease')]
    [string]$Simulation = 'Committed'
)

$ErrorActionPreference = 'Stop'

if ($Verify -and $Regenerate) {
    throw "-Verify and -Regenerate are opposites: one checks the committed files, the other rewrites them."
}

# The committed artefacts are the output of the committed simulation, and they
# have to stay that way. Regenerating them from a build that exists only in
# somebody's scratch directory would put a number in the repository that
# nothing in the repository produces.
if ($Regenerate -and $Simulation -ne 'Committed') {
    throw "-Regenerate rewrites the committed artefacts, so it only runs against the committed simulation."
}

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$content = Join-Path $repoRoot 'content'
$bundle = Join-Path $content 'match.replay'
$units = Join-Path $content 'units.txt'
$traceName = 'golden-trace.txt'
$landmarkName = 'landmarks.txt'

# One tiny bundle per defense record format version that has ever shipped, and
# the result a real run of each produced. Committed forever: the writer emits
# only the current version, so nothing can ever make an older one again.
$golden = Join-Path $content 'golden'

# Built into scratch space rather than into the project's own bin/, so that a
# run of this script cannot leave the working tree dirtier than it found it --
# which is a thing the headless test runner asserts and this one respects.
#
# The directory carries the image's name because two rows of the matrix run
# from the same shell on a laptop, and MSBuild deciding a differently-referenced
# build is up to date would silently play the previous row's simulation.
$build = Join-Path ([System.IO.Path]::GetTempPath()) ('simcli-build-' + $Simulation.ToLowerInvariant())
$program = Join-Path $build 'Sim.Cli.dll'

$committedSim = Join-Path $repoRoot 'client/Packages/com.ssalter.sim/Runtime/Sim.dll'

# The image this run is meant to play, and the one the assertion below holds
# the run to. Committed needs no build: those bytes are in the repository.
$intendedSim = $committedSim
$buildArguments = @('build', (Join-Path $repoRoot 'simcli'), '--configuration', 'Debug', '--nologo', '--output', $build)

if ($Simulation -ne 'Committed') {
    $configuration = if ($Simulation -eq 'FreshRelease') { 'Release' } else { 'Debug' }

    # --output is not optional here. sim/Sim.csproj sends its build straight
    # into client/Packages/com.ssalter.sim/Runtime/, so a build without it
    # overwrites the committed plug-in -- the artefact this script exists to
    # play -- and leaves the working tree dirty besides.
    $simBuild = Join-Path ([System.IO.Path]::GetTempPath()) ('sim-' + $configuration.ToLowerInvariant())
    $intendedSim = Join-Path $simBuild 'Sim.dll'

    & dotnet build (Join-Path $repoRoot 'sim') --configuration $configuration --nologo --output $simBuild | Out-Host

    if ($LASTEXITCODE -ne 0) {
        throw "Building the simulation at $configuration failed (exit $LASTEXITCODE)."
    }

    $buildArguments += "-p:SimAssembly=$intendedSim"
}

& dotnet @buildArguments | Out-Host

if ($LASTEXITCODE -ne 0) {
    throw "Building simcli failed (exit $LASTEXITCODE)."
}

# WHICH SIMULATION ACTUALLY GOT PLAYED, ASSERTED RATHER THAN ASSUMED. The
# reference is a HintPath and the override is an MSBuild property, so the way
# this goes wrong is silent: a property that does not reach the reference
# leaves the committed Debug image beside the runner, the row goes green, and
# what it proved is that the Debug image agrees with itself. Comparing the
# bytes the runner will load against the bytes this script meant it to load
# costs one hash and closes that hole.
$playedSim = Join-Path $build 'Sim.dll'

if (-not (Test-Path $playedSim)) {
    throw "No Sim.dll landed beside the runner in $build; the reference did not resolve."
}

$playedHash = (Get-FileHash -Algorithm SHA256 $playedSim).Hash
$intendedHash = (Get-FileHash -Algorithm SHA256 $intendedSim).Hash

if ($playedHash -ne $intendedHash) {
    throw ("The runner is about to play a different simulation than this run built.`n" +
        "  intended: $intendedSim`n" +
        "  playing : $playedSim`n" +
        "The -p:SimAssembly override did not reach simcli's reference.")
}

if ($Simulation -eq 'FreshRelease') {
    # And that it is optimised, which is the whole content of the word
    # "Release" here. Debug and Release differ in this attribute and in
    # nothing else the file name records, so a Release row whose build
    # configuration silently fell back to Debug would otherwise be a row that
    # measures nothing and says it measured the optimiser.
    $loaded = [System.Reflection.Assembly]::LoadFrom($playedSim)
    $debuggable = $loaded.GetCustomAttributes([System.Diagnostics.DebuggableAttribute], $false)

    if ($debuggable.Count -gt 0 -and $debuggable[0].IsJITOptimizerDisabled) {
        throw "The image built for the FreshRelease row has the optimiser disabled; it is a Debug build."
    }
}

Write-Host ("simulation $Simulation, SHA-256 " + $playedHash.Substring(0, 16)) -ForegroundColor Cyan

# The runner refuses by name and exits, rather than throwing: a record that
# will not replay has already said why in its own sentence, and a PowerShell
# stack trace on top of it buries the one line anybody needs to read.
function Invoke-SimCli {
    param([string[]]$CliArgs)

    & dotnet $program @CliArgs | Out-Host

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "simcli $($CliArgs[0]) refused (exit $LASTEXITCODE); its reason is above." -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# The same run, with what it printed handed back instead of shown. The golden
# results are compared byte for byte, so they have to be the runner's own
# output and not a transcription of it.
function Get-SimCliOutput {
    param([string[]]$CliArgs)

    $lines = & dotnet $program @CliArgs 2>&1

    if ($LASTEXITCODE -ne 0) {
        $lines | Out-Host
        Write-Host ""
        Write-Host "simcli $($CliArgs[0]) refused (exit $LASTEXITCODE); its reason is above." -ForegroundColor Red
        exit $LASTEXITCODE
    }

    return (($lines | ForEach-Object { $_.ToString() }) -join "`n") + "`n"
}

# The committed bundles, oldest format version first. The name carries the
# version because that is the thing each file exists to keep alive.
function Get-GoldenBundles {
    if (-not (Test-Path $golden)) {
        return @()
    }

    return Get-ChildItem -Path $golden -Filter 'defense-*.replay' | Sort-Object Name
}

function Get-GoldenResultPath {
    param([System.IO.FileInfo]$Bundle)

    return Join-Path $golden ($Bundle.BaseName + '.result')
}

# The ruleset that bundle was recorded against, and the one it is played
# against for the rest of time.
function Get-GoldenUnitsPath {
    param([System.IO.FileInfo]$Bundle)

    return Join-Path $golden ($Bundle.BaseName + '.units')
}

if ($Regenerate) {
    Invoke-SimCli @(
        'record',
        '--map', (Join-Path $content 'map.txt'),
        '--units', $units,
        '--defense', (Join-Path $content 'defense.txt'),
        '--wave', (Join-Path $content 'wave.txt'),
        '--seed', $Seed.ToString([System.Globalization.CultureInfo]::InvariantCulture),
        '--map-handle', $MapHandle.ToString([System.Globalization.CultureInfo]::InvariantCulture),
        '--out', $bundle)

    Invoke-SimCli @('run', '--bundle', $bundle, '--units', $units, '--out', $content)

    # The freshly recorded bundle becomes the golden for whatever version the
    # writer emits, and the version is read off the run rather than written in
    # here -- a number hard-coded in this script would go on naming version 1
    # after the writer moved to 2, and would overwrite the wrong file.
    New-Item -ItemType Directory -Force -Path $golden | Out-Null

    $fresh = Get-SimCliOutput @('run', '--bundle', $bundle, '--units', $units)
    $match = [regex]::Match($fresh, 'read at defense record format (\d+)')

    if (-not $match.Success) {
        throw "The runner did not say which defense format version it read; this script cannot name the golden."
    }

    $currentGolden = Join-Path $golden ("defense-" + $match.Groups[1].Value + ".replay")
    Copy-Item -Path $bundle -Destination $currentGolden -Force
    Write-Host "wrote      $currentGolden" -ForegroundColor Green

    # And the table it was just recorded against, pinned beside it. The bundle
    # carries that table's content hash and the replay gate compares the two, so
    # a re-recorded bundle whose pinned copy stayed behind is a bundle nothing
    # can replay. The older versions keep the copies they already have.
    $currentGoldenUnits = Get-GoldenUnitsPath (Get-Item $currentGolden)
    Copy-Item -Path $units -Destination $currentGoldenUnits -Force
    Write-Host "wrote      $currentGoldenUnits" -ForegroundColor Green

    # Every golden's result is rewritten, the old versions included: a rule
    # change moves what all of them do, and a stale result beside a live one is
    # the failure this whole arrangement exists to make loud. Each is played
    # against its own pinned table, so a retune leaves the older ones producing
    # exactly the bytes they already have.
    foreach ($goldenBundle in Get-GoldenBundles) {
        $goldenUnits = Get-GoldenUnitsPath $goldenBundle

        if (-not (Test-Path $goldenUnits)) {
            throw "content/golden/$($goldenBundle.Name) has no pinned unit table beside it, so there is nothing to replay it against."
        }

        $text = Get-SimCliOutput @('run', '--bundle', $goldenBundle.FullName, '--units', $goldenUnits)
        $resultPath = Get-GoldenResultPath $goldenBundle
        [System.IO.File]::WriteAllText($resultPath, $text)
        Write-Host "wrote      $resultPath" -ForegroundColor Green
    }

    exit 0
}

if (-not $Verify) {
    $arguments = @('run', '--bundle', $bundle, '--units', $units)

    if ($Out) {
        $arguments += @('--out', $Out)
    }

    Invoke-SimCli $arguments
    exit 0
}

# The observation the whole artefact rests on: a run of the committed bundle
# still produces the committed trace and the committed landmarks. It writes
# into scratch space and compares, rather than writing over the committed files
# and finding them equal -- which would be a check that cannot fail.
$scratch = Join-Path ([System.IO.Path]::GetTempPath()) ('simcli-verify-' + $Simulation.ToLowerInvariant())

if (Test-Path $scratch) {
    Remove-Item $scratch -Recurse -Force
}

Invoke-SimCli @('run', '--bundle', $bundle, '--units', $units, '--out', $scratch)

$differences = 0

# The first line the two disagree on, named. A whole-file "they differ" is a
# message nobody can act on, and the first difference is nearly always the only
# one that was not caused by the ones above it.
function Test-SameText {
    param([string]$What, [string]$Committed, [string]$Fresh)

    if ($Committed -eq $Fresh) {
        Write-Host "$What is what the run produced." -ForegroundColor Green
        return $true
    }

    $committedLines = $Committed -split "`n"
    $freshLines = $Fresh -split "`n"
    $limit = [Math]::Max($committedLines.Count, $freshLines.Count)

    Write-Host "$What is NOT what the run produced." -ForegroundColor Red

    for ($index = 0; $index -lt $limit; $index++) {
        $left = if ($index -lt $committedLines.Count) { $committedLines[$index] } else { '<end of file>' }
        $right = if ($index -lt $freshLines.Count) { $freshLines[$index] } else { '<end of file>' }

        if ($left -ne $right) {
            Write-Host ("  line {0}" -f ($index + 1))
            Write-Host ("    committed: {0}" -f $left)
            Write-Host ("    this run : {0}" -f $right)
            break
        }
    }

    return $false
}

foreach ($name in @($traceName, $landmarkName)) {
    $committed = [System.IO.File]::ReadAllText((Join-Path $content $name))
    $fresh = [System.IO.File]::ReadAllText((Join-Path $scratch $name))

    if (-not (Test-SameText "content/$name" $committed $fresh)) {
        $differences++
    }
}

# Every historical format version, replayed. The writer emits one version and
# only one, so these bundles can never be produced again -- they are the entire
# evidence that the reader branch for each retired version still reads. A
# deleted branch fails here, and the runner's refusal names the version.
$goldens = Get-GoldenBundles

if ($goldens.Count -eq 0) {
    Write-Host "content/golden/ holds no bundles at all; every historical format version is unproven." -ForegroundColor Red
    $differences++
}

foreach ($goldenBundle in $goldens) {
    $resultPath = Get-GoldenResultPath $goldenBundle
    $goldenUnits = Get-GoldenUnitsPath $goldenBundle

    if (-not (Test-Path $resultPath)) {
        Write-Host "content/golden/$($goldenBundle.Name) has no committed result beside it." -ForegroundColor Red
        $differences++
        continue
    }

    # There is no fallback to content/units.txt, and a missing pin is a
    # difference rather than a substitution: a golden played against a table it
    # was not recorded against is refused by the gate for a reason that has
    # nothing to do with the reader branch it is here to prove.
    if (-not (Test-Path $goldenUnits)) {
        Write-Host "content/golden/$($goldenBundle.Name) has no pinned unit table beside it." -ForegroundColor Red
        $differences++
        continue
    }

    $fresh = Get-SimCliOutput @('run', '--bundle', $goldenBundle.FullName, '--units', $goldenUnits)
    $committed = [System.IO.File]::ReadAllText($resultPath)

    if (-not (Test-SameText "content/golden/$($goldenBundle.BaseName).result" $committed $fresh)) {
        $differences++
    }
}

if ($differences -gt 0) {
    Write-Host ""
    Write-Host "The committed artefacts and a real run disagree." -ForegroundColor Yellow
    Write-Host "If the content or the rules changed on purpose, regenerate them:"
    Write-Host "  ./tools/run-headless-match.ps1 -Regenerate"
    exit 1
}

exit 0
