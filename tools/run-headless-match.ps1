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

foreach ($name in @($traceName, $landmarkName)) {
    $committedPath = Join-Path $content $name
    $freshPath = Join-Path $scratch $name

    $committed = [System.IO.File]::ReadAllText($committedPath)
    $fresh = [System.IO.File]::ReadAllText($freshPath)

    if ($committed -eq $fresh) {
        Write-Host "content/$name is what the run produced." -ForegroundColor Green
        continue
    }

    $differences++

    $committedLines = $committed -split "`n"
    $freshLines = $fresh -split "`n"
    $limit = [Math]::Max($committedLines.Count, $freshLines.Count)

    Write-Host "content/$name is NOT what the run produced." -ForegroundColor Red

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
}

if ($differences -gt 0) {
    Write-Host ""
    Write-Host "The committed artefacts and a real run disagree." -ForegroundColor Yellow
    Write-Host "If the content or the rules changed on purpose, regenerate them:"
    Write-Host "  ./tools/run-headless-match.ps1 -Regenerate"
    exit 1
}

exit 0
