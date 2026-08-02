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
    [ulong]$Seed = 20260801
)

$ErrorActionPreference = 'Stop'

if ($Verify -and $Regenerate) {
    throw "-Verify and -Regenerate are opposites: one checks the committed files, the other rewrites them."
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
$build = Join-Path ([System.IO.Path]::GetTempPath()) 'simcli-build'
$program = Join-Path $build 'Sim.Cli.dll'

& dotnet build (Join-Path $repoRoot 'simcli') --configuration Debug --nologo --output $build | Out-Host

if ($LASTEXITCODE -ne 0) {
    throw "Building simcli failed (exit $LASTEXITCODE)."
}

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

if ($Regenerate) {
    Invoke-SimCli @(
        'record',
        '--map', (Join-Path $content 'map.txt'),
        '--units', $units,
        '--defense', (Join-Path $content 'defense.txt'),
        '--wave', (Join-Path $content 'wave.txt'),
        '--seed', $Seed.ToString([System.Globalization.CultureInfo]::InvariantCulture),
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

    # Every golden's result is rewritten, the old versions included: a rule
    # change moves what all of them do, and a stale result beside a live one is
    # the failure this whole arrangement exists to make loud.
    foreach ($goldenBundle in Get-GoldenBundles) {
        $text = Get-SimCliOutput @('run', '--bundle', $goldenBundle.FullName, '--units', $units)
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
$scratch = Join-Path ([System.IO.Path]::GetTempPath()) 'simcli-verify'

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

    if (-not (Test-Path $resultPath)) {
        Write-Host "content/golden/$($goldenBundle.Name) has no committed result beside it." -ForegroundColor Red
        $differences++
        continue
    }

    $fresh = Get-SimCliOutput @('run', '--bundle', $goldenBundle.FullName, '--units', $units)
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
