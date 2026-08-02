<#
.SYNOPSIS
    Asserts the client's expensive-to-change project settings, by reading the
    settings files. No editor involved.

.DESCRIPTION
    Issue #6's research sorted Unity's creation-time settings into three tiers,
    and this script covers the top two: the ones that are permanent, plus the
    ones whose cost is every judgement already made on top of the old value.
    Most of them arrived correct from the Universal 3D template, which is
    exactly why they need a check -- a default nobody chose is a default nobody
    would notice changing.

    Reading is the whole mechanism, deliberately. Writing a Unity project
    setting from outside the editor means hand-editing serialized YAML, which
    this repo does not do: Unity writes those files or nobody does. Confirming
    one costs nothing and needs no lock, so it is free to do often.

    Run it with the editor closed. An open editor holds settings in memory and
    writes them on quit, so a file read while one is running answers about the
    last save rather than about the project.

.EXAMPLE
    ./tools/check-project-settings.ps1
#>

$ErrorActionPreference = 'Stop'

$client = (Resolve-Path "$PSScriptRoot\..\client").Path
$ps     = Get-Content "$client\ProjectSettings\ProjectSettings.asset" -Raw
$vcs    = Get-Content "$client\ProjectSettings\VersionControlSettings.asset" -Raw
$editor = Get-Content "$client\ProjectSettings\EditorSettings.asset" -Raw
$gfx    = Get-Content "$client\ProjectSettings\GraphicsSettings.asset" -Raw
$ver    = Get-Content "$client\ProjectSettings\ProjectVersion.txt" -Raw
$man    = Get-Content "$client\Packages\manifest.json" -Raw

if (Test-Path "$client\Temp\UnityLockfile") {
    Write-Warning "client/Temp/UnityLockfile exists -- an editor may be running, and these files may be stale."
}

$results = [System.Collections.Generic.List[object]]::new()
function Check([string]$what, [string]$where, [string]$found, [bool]$ok) {
    $results.Add([pscustomobject]@{ Setting = $what; File = $where; Found = $found; OK = $ok })
}

# --- Editor version. Permanent downward: there is no supported downgrade. -----
$m = [regex]::Match($ver, 'm_EditorVersion:\s*(\S+)')
Check 'Editor version' 'ProjectVersion.txt' $m.Groups[1].Value ($m.Groups[1].Value -eq '6000.5.6f1')

# --- Render pipeline. Permanent in the honest sense: there is a converter into
#     URP and none out of it. Three independent records have to agree.
$mapped   = $gfx -match 'UnityEngine\.Rendering\.Universal\.UniversalRenderPipeline:'
$assigned = [regex]::Match($gfx, 'm_CustomRenderPipeline:\s*\{fileID:\s*\d+,\s*guid:\s*([0-9a-f]+)')
$declared = $man -match '"com\.unity\.render-pipelines\.universal"'
$rpOk     = $mapped -and $declared -and $assigned.Success -and
            $assigned.Groups[1].Value -notmatch '^0+$'
$rpFound  = "global settings map: $mapped; package declared: $declared; " +
            "pipeline asset guid: $(if ($assigned.Success) { $assigned.Groups[1].Value } else { 'none' })"
Check 'Universal render pipeline' 'GraphicsSettings.asset + manifest.json' $rpFound $rpOk

# --- Colour space. Annoying: the dropdown is free, re-judging every material,
#     light and particle ramp by eye is not. 1 == Linear.
$m = [regex]::Match($ps, 'm_ActiveColorSpace:\s*(\d+)')
Check 'Linear colour space' 'ProjectSettings.asset' "m_ActiveColorSpace: $($m.Groups[1].Value)" ($m.Groups[1].Value -eq '1')

# --- Input handling. Free to change, but it decides which of two APIs the view
#     is written against. 1 == Input System Package (New).
$m = [regex]::Match($ps, 'activeInputHandler:\s*(\d+)')
Check 'New input system' 'ProjectSettings.asset' "activeInputHandler: $($m.Groups[1].Value)" ($m.Groups[1].Value -eq '1')

# --- API compatibility level. One-way in practice: widening is a dropdown, but
#     code written against the wider surface will not compile back down. This is
#     the setting that lets Unity load a netstandard2.1 Sim.dll. 6 == .NET Standard.
$m = [regex]::Match($ps, 'apiCompatibilityLevel:\s*(\d+)')
Check '.NET Standard API level' 'ProjectSettings.asset' "apiCompatibilityLevel: $($m.Groups[1].Value)" ($m.Groups[1].Value -eq '6')

# --- Scripting backend. Free -- it is a per-platform build setting -- and Mono
#     is the faster loop for the skeleton. Unity only serializes non-default
#     entries, so Mono for Standalone is recorded by the absence of a Standalone
#     line rather than by a value. That is worth asserting precisely because a
#     missing line is easy to read as "nothing was checked".
$m = [regex]::Match($ps, '(?m)^  scriptingBackend:\s*\r?\n((?:^    .*\r?\n)*)')
$entries = if ($m.Success -and $m.Groups[1].Value.Trim()) { $m.Groups[1].Value.Trim() -replace '\s*\r?\n\s*', ', ' } else { '(none)' }
Check 'Mono scripting backend (Standalone)' 'ProjectSettings.asset' "scriptingBackend: $entries" ($entries -notmatch 'Standalone')

# --- Asset serialization. Annoying: flipping it re-serializes every asset in the
#     project, in one commit nobody can read. 2 == Force Text.
$m = [regex]::Match($editor, 'm_SerializationMode:\s*(\d+)')
Check 'Force-text serialization' 'EditorSettings.asset' "m_SerializationMode: $($m.Groups[1].Value)" ($m.Groups[1].Value -eq '2')

# --- Version control mode. Same re-write cost, and the .meta files it makes
#     visible are what carry every GUID git has to track -- including the one on
#     Sim.dll that holds "Auto Reference off".
$m = [regex]::Match($vcs, 'm_Mode:\s*(.+)')
$mode = $m.Groups[1].Value.Trim()
Check 'Visible meta files' 'VersionControlSettings.asset' "m_Mode: $mode" ($mode -eq 'Visible Meta Files')

# --- Report -------------------------------------------------------------------
foreach ($r in $results) {
    $mark = if ($r.OK) { 'OK  ' } else { 'FAIL' }
    $colour = if ($r.OK) { 'Green' } else { 'Red' }
    Write-Host ("{0}  {1,-38} {2}" -f $mark, $r.Setting, $r.Found) -ForegroundColor $colour
    if (-not $r.OK) { Write-Host ("      read from {0}" -f $r.File) -ForegroundColor Red }
}

$bad = @($results | Where-Object { -not $_.OK })
Write-Host ""
if ($bad.Count) {
    Write-Host "$($bad.Count) of $($results.Count) settings are not what this project decided on." -ForegroundColor Red
    Write-Host "Fix them in the editor's own settings windows. Do not hand-edit the YAML."
    exit 1
}

Write-Host "all $($results.Count) settings confirmed." -ForegroundColor Green
exit 0
