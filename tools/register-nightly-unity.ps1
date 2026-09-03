<#
.SYNOPSIS
    Registers -- or removes -- the Windows scheduled task that runs
    `nightly-unity.ps1` against this checkout at 03:00.

.DESCRIPTION
    ONE TASK ON THE MACHINE, UNDER ONE NAME, POINTING AT ONE CHECKOUT.
    Registering again replaces it, which is how it is repointed after a
    checkout moves. Batchmode needs the project lock, so two of these running
    at once would take the lock from each other and lose both nights.

    IT REFUSES TO REGISTER FROM A WORKTREE. Rule 5 of AGENTS.md is that a
    worktree under .claude/worktrees/ is finished and deleted, and a daily task
    left pointing into a deleted directory fires forever, fails silently and is
    nobody's to notice. A worktree is told apart from a checkout by its .git,
    which is a file there and a directory here.

    THE TASK RUNS AS THE LOGGED-ON USER AND ONLY WHILE THEY ARE LOGGED ON. That
    is what needs no stored password and no elevation. The alternative, a task
    with a saved credential that runs at the winlogon desktop, would want a
    password typed into this script -- and the Unity editor it drives is
    licensed to a signed-in session anyway.

    IT RUNS LATE RATHER THAN NOT AT ALL. A machine asleep at 03:00 misses the
    trigger, so the task is set to start when it next can, and to survive going
    onto battery. The four-hour limit is the other end of the same argument: a
    batchmode editor that hangs holds the project lock, and a lock still held
    at breakfast is worse than a night with no report.

.EXAMPLE
    ./tools/register-nightly-unity.ps1
    Create or replace the task, and print what it registered.

.EXAMPLE
    ./tools/register-nightly-unity.ps1 -Unregister
    Remove the task.
#>
param([switch]$Unregister)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$nightly = Join-Path $repoRoot 'tools/nightly-unity.ps1'
$taskName = 'NightlyUnityTests'

if ($Unregister) {
    if (-not (Get-ScheduledTask -TaskName $taskName -ErrorAction SilentlyContinue)) {
        Write-Host "no scheduled task named $taskName -- nothing to remove."
        exit 0
    }

    Unregister-ScheduledTask -TaskName $taskName -Confirm:$false
    Write-Host "removed scheduled task $taskName"
    exit 0
}

if (Test-Path -LiteralPath (Join-Path $repoRoot '.git') -PathType Leaf) {
    Write-Host "$repoRoot is a git worktree, and worktrees get deleted (AGENTS.md rule 5)." -ForegroundColor Red
    Write-Host "A nightly task pointing into one would outlive it. Register from the main checkout."
    exit 1
}

# The host this is being registered from, so the task starts the same
# PowerShell rather than whichever one Task Scheduler would find.
$powershell = (Get-Process -Id $PID).Path

$action = New-ScheduledTaskAction -Execute $powershell `
    -Argument ('-NoProfile -NonInteractive -File "{0}"' -f $nightly) `
    -WorkingDirectory $repoRoot

$trigger = New-ScheduledTaskTrigger -Daily -At '03:00'

$principal = New-ScheduledTaskPrincipal -UserId ('{0}\{1}' -f $env:USERDOMAIN, $env:USERNAME) `
    -LogonType Interactive

$settings = New-ScheduledTaskSettingsSet -StartWhenAvailable `
    -AllowStartIfOnBatteries -DontStopIfGoingOnBatteries `
    -ExecutionTimeLimit (New-TimeSpan -Hours 4)

$task = Register-ScheduledTask -TaskName $taskName -Action $action -Trigger $trigger `
    -Principal $principal -Settings $settings -Force

Write-Host "registered $($task.TaskName)"
Write-Host "  runs   $powershell -NoProfile -NonInteractive -File `"$nightly`""
Write-Host "  in     $repoRoot"
Write-Host "  at     03:00 daily, as $($principal.UserId), while logged on"
Write-Host "  logs   $(Join-Path $repoRoot 'client/Logs/nightly.log')"
Write-Host ""
Write-Host "remove it with: ./tools/register-nightly-unity.ps1 -Unregister"
