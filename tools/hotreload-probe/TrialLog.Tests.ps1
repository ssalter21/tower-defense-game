# Holds TrialLog.psm1 against a real captured Editor.log.
#
# fixtures/three-trials.log is a verbatim slice of client/Logs/Editor.log from
# the #34 session: three probe reloads, each followed by the Asset Pipeline
# Refresh record it happened inside, with the intervening noise left in. The
# three trials are also the three "focused, foreground" rows of the table in
# CLAUDE.md, so the numbers the parser produces here are numbers that have
# already been published -- a transcription error shows up as a red test.
#
# No Pester: this is four asserts and a runner, and the probe is throwaway.
#
#   ./tools/hotreload-probe/TrialLog.Tests.ps1

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

Import-Module (Join-Path $PSScriptRoot 'TrialLog.psm1') -Force

$script:Failures = 0
$script:Ran = 0

function Test-Case([string]$Name, [scriptblock]$Body) {
    $script:Ran++
    try {
        & $Body
        Write-Host "  PASS  $Name" -ForegroundColor DarkGreen
    } catch {
        $script:Failures++
        Write-Host "  FAIL  $Name" -ForegroundColor Red
        Write-Host "        $($_.Exception.Message)" -ForegroundColor Red
    }
}

function Assert-Equal($Expected, $Actual, [string]$What) {
    if ($Expected -ne $Actual) {
        throw "$What`: expected <$Expected>, got <$Actual>"
    }
}

function Assert-Close([double]$Expected, [double]$Actual, [double]$Tolerance, [string]$What) {
    if ([Math]::Abs($Expected - $Actual) -gt $Tolerance) {
        throw "$What`: expected <$Expected> +/- $Tolerance, got <$Actual>"
    }
}

$fixture = Get-Content (Join-Path $PSScriptRoot 'fixtures/three-trials.log')

Write-Host ''
Write-Host 'TrialLog' -ForegroundColor Cyan

Test-Case 'finds every probe reload in the log' {
    $r = @(Get-ProbeReload -Lines $fixture)
    Assert-Equal 3 $r.Count 'reload count'
    Assert-Equal '20:12:03.379' $r[0].Clock 'first clock'
    Assert-Equal '20:32:22.584' $r[2].Clock 'third clock'
}

Test-Case 'reads the editor clock and the build stamp as real datetimes' {
    $r = @(Get-ProbeReload -Lines $fixture)[1]
    Assert-Equal ([datetime]'2026-08-01 20:16:06.586') $r.ReloadAt 'reload time'
    Assert-Equal ([datetime]'2026-08-01 20:14:02.729') $r.BuiltAt  'build stamp time'
}

Test-Case 'selects the reload for one specific build, not merely the latest' {
    $r = @(Get-ProbeReload -Lines $fixture -Stamp 'build #2 at 2026-08-01 20:14:02.729')
    Assert-Equal 1 $r.Count 'matches for build #2'
    Assert-Equal ([datetime]'2026-08-01 20:16:06.586') $r[0].ReloadAt 'reload time'
}

Test-Case 'a stamp that never reached the editor matches nothing' {
    $r = @(Get-ProbeReload -Lines $fixture -Stamp 'build #9 at 2026-08-01 23:00:00.000')
    Assert-Equal 0 $r.Count 'matches for an unseen build'
}

Test-Case 'takes the refresh record AFTER the reload, not the one before it' {
    $second = @(Get-ProbeReload -Lines $fixture)[1]
    $refresh = Get-RefreshAfter -Lines $fixture -FromIndex $second.LineIndex
    # 26.731 is the record belonging to the PREVIOUS trial and sits earlier in
    # the file; picking it up would misdate this refresh's start by nine seconds.
    Assert-Close 17.800 $refresh.TotalSeconds 0.0005 'total seconds'
}

Test-Case 'reports no record rather than reaching backwards for one' {
    $lastReloadIndex = @(Get-ProbeReload -Lines $fixture)[2].LineIndex
    $truncated = $fixture[0..($lastReloadIndex + 3)]
    $refresh = Get-RefreshAfter -Lines $truncated -FromIndex $lastReloadIndex
    Assert-Equal $null $refresh 'refresh record past the end of the log'
}

Test-Case 'derives the refresh start by working back from its own Total' {
    $third = @(Get-ProbeReload -Lines $fixture)[2]
    $refresh = Get-RefreshAfter -Lines $fixture -FromIndex $third.LineIndex
    $outcome = Resolve-TrialOutcome -ReloadAt $third.ReloadAt -BuiltAt $third.BuiltAt `
        -RefreshSeconds $refresh.TotalSeconds -AltTabAt $null
    # 20:32:22.584 less 22.057 s
    Assert-Equal ([datetime]'2026-08-01 20:32:00.527') $outcome.RefreshStartedAt 'refresh start'
}

Test-Case 'a refresh already under way at the alt-tab is void, not a fast result' {
    $third = @(Get-ProbeReload -Lines $fixture)[2]
    $refresh = Get-RefreshAfter -Lines $fixture -FromIndex $third.LineIndex
    $outcome = Resolve-TrialOutcome -ReloadAt $third.ReloadAt -BuiltAt $third.BuiltAt `
        -RefreshSeconds $refresh.TotalSeconds -AltTabAt ([datetime]'2026-08-01 20:32:10')
    Assert-Equal 'void' $outcome.Verdict 'verdict'
}

Test-Case 'an alt-tab before the refresh started yields a delay in seconds' {
    $third = @(Get-ProbeReload -Lines $fixture)[2]
    $refresh = Get-RefreshAfter -Lines $fixture -FromIndex $third.LineIndex
    $outcome = Resolve-TrialOutcome -ReloadAt $third.ReloadAt -BuiltAt $third.BuiltAt `
        -RefreshSeconds $refresh.TotalSeconds -AltTabAt ([datetime]'2026-08-01 20:31:50')
    Assert-Equal 'valid' $outcome.Verdict 'verdict'
    Assert-Close 10.527 $outcome.AltTabToRefreshSeconds 0.0005 'alt-tab to refresh start'
}

Test-Case 'a trial nobody alt-tabbed into is void too, and says which' {
    $third = @(Get-ProbeReload -Lines $fixture)[2]
    $refresh = Get-RefreshAfter -Lines $fixture -FromIndex $third.LineIndex
    $outcome = Resolve-TrialOutcome -ReloadAt $third.ReloadAt -BuiltAt $third.BuiltAt `
        -RefreshSeconds $refresh.TotalSeconds -AltTabAt $null
    Assert-Equal 'no-alt-tab' $outcome.Verdict 'verdict'
}

Test-Case 'a reload logged after midnight belongs to the next day' {
    # The probe line carries HH:mm:ss.fff and no date; the stamp carries both.
    # Anchoring naively to the stamp's date puts this reload 23h 59m BEFORE the
    # rebuild that caused it, and the trial reads as a negative delay.
    $lines = @(
        '[hot-reload probe] 00:00:20.500 (domain reload) -- plug-in stamp: build #7 at 2026-08-01 23:59:50.000'
    )
    $r = @(Get-ProbeReload -Lines $lines)[0]
    Assert-Equal ([datetime]'2026-08-02 00:00:20.500') $r.ReloadAt 'reload time'
    Assert-Equal ([datetime]'2026-08-01 23:59:50.000') $r.BuiltAt 'build stamp time'
}

$phased = Get-Content (Join-Path $PSScriptRoot 'fixtures/refresh-with-phases.log')

Test-Case 'sums the phases that ran after the reload was logged' {
    $r = @(Get-ProbeReload -Lines $phased)[0]
    $refresh = Get-RefreshAfter -Lines $phased -FromIndex $r.LineIndex
    Assert-Equal $true $refresh.HasPhaseBreakdown 'has a phase breakdown'
    # ImportOutOfDateAssets 1198.104ms + PostProcessAllAssets 164.898ms
    Assert-Close 1.363002 $refresh.TailSeconds 0.0000005 'tail seconds'
}

Test-Case 'counts each tail phase once, not its children as well' {
    # ImportOutOfDateAssets has indented children summing to ~24ms. Matching
    # them too would subtract the same time twice and drag the start later.
    $r = @(Get-ProbeReload -Lines $phased)[0]
    $refresh = Get-RefreshAfter -Lines $phased -FromIndex $r.LineIndex
    if ($refresh.TailSeconds -ge 1.4) {
        throw "tail $($refresh.TailSeconds) looks like it double-counted nested children"
    }
}

Test-Case 'a record with no breakdown reports no tail rather than guessing one' {
    $second = @(Get-ProbeReload -Lines $fixture)[1]
    $refresh = Get-RefreshAfter -Lines $fixture -FromIndex $second.LineIndex
    Assert-Equal $false $refresh.HasPhaseBreakdown 'has a phase breakdown'
    Assert-Equal 0 $refresh.TailSeconds 'tail seconds'
}

Test-Case 'the tail moves the refresh start later, not the reload' {
    $r = @(Get-ProbeReload -Lines $phased)[0]
    $refresh = Get-RefreshAfter -Lines $phased -FromIndex $r.LineIndex
    $outcome = Resolve-TrialOutcome -ReloadAt $r.ReloadAt -BuiltAt $r.BuiltAt `
        -RefreshSeconds $refresh.TotalSeconds -TailSeconds $refresh.TailSeconds -AltTabAt $null
    # 21:42:23.750 less (14.955 - 1.363) = 13.592 s
    Assert-Equal ([datetime]'2026-08-02 21:42:10.158') $outcome.RefreshStartedAt 'refresh start'
}

Test-Case 'an alt-tab inside the tail window counts instead of being voided' {
    # The exact failure the run hit: alt-tab 6 s after the rebuild, refresh
    # started 6.8 s after it. Ignoring the tail puts the start at 5.4 s and
    # throws the trial away.
    $r = @(Get-ProbeReload -Lines $phased)[0]
    $refresh = Get-RefreshAfter -Lines $phased -FromIndex $r.LineIndex
    $altTab = $r.BuiltAt.AddSeconds(6)
    $withTail = Resolve-TrialOutcome -ReloadAt $r.ReloadAt -BuiltAt $r.BuiltAt `
        -RefreshSeconds $refresh.TotalSeconds -TailSeconds $refresh.TailSeconds -AltTabAt $altTab
    $withoutTail = Resolve-TrialOutcome -ReloadAt $r.ReloadAt -BuiltAt $r.BuiltAt `
        -RefreshSeconds $refresh.TotalSeconds -AltTabAt $altTab
    Assert-Equal 'valid' $withTail.Verdict 'verdict with the tail subtracted'
    Assert-Equal 'void'  $withoutTail.Verdict 'verdict without it'
}

Write-Host ''
if ($script:Failures -gt 0) {
    Write-Host "$($script:Failures) of $($script:Ran) failed." -ForegroundColor Red
    exit 1
}
Write-Host "$($script:Ran) passed." -ForegroundColor Green
exit 0
