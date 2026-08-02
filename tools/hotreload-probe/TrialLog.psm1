# Reading a trial out of client/Logs/Editor.log.
#
# Split out of probe.ps1 because this is the part that is easy to get quietly
# wrong, and wrong here does not look wrong -- it looks like a number. Issue #34
# burned two trials on exactly these mistakes, so each function below exists to
# make one of them impossible rather than documented. TrialLog.Tests.ps1 holds
# them against a real captured log.

Set-StrictMode -Version Latest

# [hot-reload probe] 20:12:03.379 (domain reload) -- plug-in stamp: build #1 at 2026-08-01 20:08:26.743
$script:ReloadPattern  = '^\[hot-reload probe\] (?<clock>\d{2}:\d{2}:\d{2}\.\d{3}) \(domain reload\) -- plug-in stamp: (?<stamp>.+?)\s*$'
$script:BuiltAtPattern = ' at (?<built>\d{4}-\d{2}-\d{2} \d{2}:\d{2}:\d{2}\.\d{3})\s*$'
# Asset Pipeline Refresh (id=87fa...): Total: 26.731 seconds - Initiated by RefreshV2(AllowForceSynchronousImport)
$script:RefreshPattern = '^Asset Pipeline Refresh \(id=(?<id>[0-9a-fA-F]+)\): Total: (?<total>\d+(?:\.\d+)?) seconds - Initiated by (?<by>.+?)\s*$'

# Unity writes the log with the invariant forms above whatever the machine's
# locale is, so the parse has to be invariant too -- otherwise the same log
# reads differently on a machine with a comma decimal separator.
$script:Invariant = [Globalization.CultureInfo]::InvariantCulture

function Get-ProbeReload {
    <#
    .SYNOPSIS
        Every domain reload the probe logged, with the EDITOR'S OWN clock.

    .DESCRIPTION
        The timestamp inside the message is the event. When the line became
        readable is your poll loop plus Unity's flush, and in one trial on #34
        the reload had already run two seconds before the action that appeared
        to cause it. Nothing here ever looks at file mtime or read time.

        The line carries both clocks: the editor's, and the rebuild stamp the
        probe DLL was compiled with. That makes each reload self-anchoring --
        the stamp supplies the date the bare HH:mm:ss.fff does not have.

        Manual checks (Tools > Hot-reload probe > Print stamp now) log through
        the same prefix and are deliberately not matched: they say which build
        is loaded, not when one arrived.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][AllowEmptyString()][string[]]$Lines,
        [string]$Stamp
    )

    $wanted = if ($PSBoundParameters.ContainsKey('Stamp')) { $Stamp } else { $null }

    for ($i = 0; $i -lt $Lines.Count; $i++) {
        $m = [regex]::Match($Lines[$i], $script:ReloadPattern)
        if (-not $m.Success) { continue }

        $stampText = $m.Groups['stamp'].Value
        if ($wanted -and $stampText -ne $wanted) { continue }

        $b = [regex]::Match($stampText, $script:BuiltAtPattern)
        if (-not $b.Success) {
            throw "Probe line $($i + 1) has no build stamp to anchor its date to: $($Lines[$i])"
        }

        $builtAt = [datetime]::ParseExact($b.Groups['built'].Value, 'yyyy-MM-dd HH:mm:ss.fff', $script:Invariant)
        $timeOfDay = [timespan]::ParseExact($m.Groups['clock'].Value, 'hh\:mm\:ss\.fff', $script:Invariant)

        # The reload always follows the rebuild that caused it. If anchoring to
        # the stamp's date puts it earlier, the editor crossed midnight waiting.
        $reloadAt = $builtAt.Date + $timeOfDay
        if ($reloadAt -lt $builtAt) { $reloadAt = $reloadAt.AddDays(1) }

        [pscustomobject]@{
            LineIndex = $i
            Clock     = $m.Groups['clock'].Value
            Stamp     = $stampText
            BuiltAt   = $builtAt
            ReloadAt  = $reloadAt
        }
    }
}

function Get-RefreshAfter {
    <#
    .SYNOPSIS
        The Asset Pipeline Refresh record that the given reload happened inside.

    .DESCRIPTION
        The reload is logged near the END of the refresh that caused it, and the
        record naming that refresh's duration comes AFTER it. So the search runs
        forwards from the reload and takes the first record. Searching backwards
        would find the previous refresh, which is a different event entirely.

        Returns $null when the log has not reached the record yet. That is a
        "not finished" and never a "not found": the caller must keep polling
        rather than treat the absence as an answer.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][AllowEmptyCollection()][AllowEmptyString()][string[]]$Lines,
        [Parameter(Mandatory)][int]$FromIndex
    )

    for ($i = $FromIndex + 1; $i -lt $Lines.Count; $i++) {
        $m = [regex]::Match($Lines[$i], $script:RefreshPattern)
        if (-not $m.Success) { continue }

        return [pscustomobject]@{
            LineIndex    = $i
            Id           = $m.Groups['id'].Value
            TotalSeconds = [double]::Parse($m.Groups['total'].Value, $script:Invariant)
            InitiatedBy  = $m.Groups['by'].Value
        }
    }

    return $null
}

function Resolve-TrialOutcome {
    <#
    .SYNOPSIS
        Void, or a number -- decided from the refresh START, never the reload.

    .DESCRIPTION
        A refresh already under way when the alt-tab happened is not a fast
        result, it is no result. That is the trap #34 fell into twice: both
        attempts to force a transition landed on a refresh the editor had begun
        ~18 s earlier of its own accord, which is evidence of nothing. The
        refresh start is the reload timestamp less the refresh's own Total.
    #>
    [CmdletBinding()]
    param(
        [Parameter(Mandatory)][datetime]$ReloadAt,
        [Parameter(Mandatory)][double]$RefreshSeconds,
        [Parameter(Mandatory)][datetime]$BuiltAt,
        [AllowNull()][Nullable[datetime]]$AltTabAt
    )

    # Both clocks in the log carry milliseconds and no more, so the subtraction
    # is done in whole milliseconds. AddSeconds on a double leaves a tick or so
    # of residue, which is not precision -- it is noise that reads as precision.
    $refreshMs = [long][math]::Round($RefreshSeconds * 1000)
    $refreshStartedAt = $ReloadAt.AddTicks(-$refreshMs * [timespan]::TicksPerMillisecond)

    $verdict = 'valid'
    $altTabToRefresh = $null
    if ($null -eq $AltTabAt) {
        $verdict = 'no-alt-tab'
    } else {
        $altTabToRefresh = ($refreshStartedAt - $AltTabAt).TotalSeconds
        if ($refreshStartedAt -lt $AltTabAt) { $verdict = 'void' }
    }

    [pscustomobject]@{
        Verdict                = $verdict
        BuiltAt                = $BuiltAt
        AltTabAt               = $AltTabAt
        RefreshStartedAt       = $refreshStartedAt
        ReloadAt               = $ReloadAt
        RefreshSeconds         = $RefreshSeconds
        AltTabToRefreshSeconds = $altTabToRefresh
        RebuildToReloadSeconds = ($ReloadAt - $BuiltAt).TotalSeconds
    }
}

Export-ModuleMember -Function Get-ProbeReload, Get-RefreshAfter, Resolve-TrialOutcome
