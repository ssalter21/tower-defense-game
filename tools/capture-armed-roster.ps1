# Renders every unit in the roster holding what it holds, one PNG each.
#
# WHY THIS IS NOT capture-match-frames.ps1. That one photographs the recorded
# match, and the recorded match is one defense: content/defense.txt puts four
# archers and two mages on the board and nothing else. The Soldier, the
# Skeleton and the Skeleton Warrior are units the game can draw that no frame
# of that match will ever contain -- which is how the Soldier's sword went
# unreviewed. Changing the defense to get a photograph would re-freeze every
# committed golden for the sake of a picture.
#
# THE PNGs ARE DOCUMENTATION, NOT AN ORACLE. Nothing compares them to anything
# and nothing fails if they change. What catches a broken view is the
# assertions in ImportedArtTests and MatchViewTests. What this is for is
# letting a human see what a unit is holding.
#
# It draws through the real TowerView and CreepView with the art the scene is
# wired from, so what comes out is what the match draws.
#
# NOTHING IT WRITES IS COMMITTED. The default output is docs/frames/roster,
# which docs/frames/.gitignore already excludes, so running this leaves no
# untracked PNGs for the build gate's tree-clean step to trip over. Regenerate
# it when you want to look; it is not documentation anybody has to keep.
#
# IT ALSO RENDERS A SET THAT IS NOT THE ROSTER. -SetFile points at a candidate
# set -- a plain-text list of model, right hand, left hand and pose clip, one
# line per character, with every path relative to client/Assets/Art. Those are
# characters no row in content/units.txt points at yet, so they cannot come
# from the roster; they are what a proposal is asking for approval of. The set
# run writes one PNG per entry plus candidates-sheet.png and
# candidates-manifest.txt, which says which tile is which. See
# docs/roster-expansion-candidates.txt for the set the roster expansion put up,
# which is also where the line format is written out in full -- including the
# atlas and beside-prop columns and the two suffixes, @x,y,z to turn a held
# prop and !Node to leave a part of the body out of the render.
#
# -Strip N ALSO DRAWS EACH CANDIDATE ACROSS ITS WHOLE CLIP, N frames laid left
# to right into strip-NN-<name>.png, with strip-index.txt beside them giving
# each clip's length in seconds. A still cannot answer "which animation": posed
# at the strike, a chop and a diagonal slice are two pictures of a body holding
# a hammer, and what tells them apart is the path the hammer took. The camera is
# framed once across every phase, so the body moves in the strip and the world
# does not.
#
# THE SETS THAT ARE STANDING QUESTIONS, rather than one effort's proposal:
#   docs/roster-paladin-clips.txt        ids 20, 21 and 22, which ship posed
#                                        by nothing and stand in a bind pose
#   docs/roster-prop-turns.txt           the Witch's broom and the
#                                        Necromancer's scythe, which lie flat
#   docs/roster-grave-robber-sword.txt   id 49's sheathed sword, on and off
#
# -batchmode -executeMethod, so it needs no editor session, no bridge and
# nobody at a keyboard -- and therefore requires the editor to be CLOSED,
# because batchmode needs the project lock.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$OutDir,
    [string]$SetFile,
    [int]$Width = 700,
    [int]$Strip = 1,
    [string]$LogFile = "$PSScriptRoot\..\capture-armed-roster.log"
)

$ErrorActionPreference = 'Stop'

$project = (Resolve-Path "$PSScriptRoot\..\client").Path

if (-not (Test-Path $Unity)) { throw "Unity Editor not found at: $Unity" }

# ABSOLUTE, ALWAYS. A relative path handed to -executeMethod is resolved
# against the editor's working directory, which is the Unity project and not
# the repository root -- so a default of "docs/frames/roster" quietly writes
# client/docs/frames/roster, outside the ignore rule that is supposed to cover
# it, and leaves untracked PNGs for the gate's tree-clean step to fail on.
# capture-match-frames.ps1 had the same trap and now closes it the same way.
if (-not $OutDir) { $OutDir = Join-Path (Resolve-Path "$PSScriptRoot\..").Path "docs\frames\roster" }
if (-not [System.IO.Path]::IsPathRooted($OutDir)) { $OutDir = Join-Path (Get-Location).Path $OutDir }

# The set file has the same trap and needs the same absolute path, for the same
# reason: the editor's working directory is the Unity project, not the repo.
# Resolved here rather than in the editor so a typo fails in the shell, in a
# second, instead of three minutes into a batchmode run.
if ($SetFile) {
    if (-not (Test-Path $SetFile)) { throw "No candidate set file at: $SetFile" }
    $SetFile = (Resolve-Path $SetFile).Path
}

$unityArgs = @(
    '-batchmode', '-quit',
    '-projectPath', "`"$project`"",
    '-executeMethod', 'View.Editor.ArmedRosterCapture.Run',
    '-logFile', "`"$LogFile`"",
    '-rosterWidth', $Width
)

$unityArgs += @('-rosterOutDir', "`"$OutDir`"")

if ($Strip -gt 1) { $unityArgs += @('-rosterStrip', $Strip) }

if ($SetFile) {
    $unityArgs += @('-rosterSet', "`"$SetFile`"")
    Write-Host "drawing the candidate set $SetFile from $project"
}
else {
    Write-Host "drawing the armed roster from $project"
}

# Start-Process plus an explicit WaitForExit is what actually blocks on a
# GUI-subsystem executable and what actually yields its exit code. `& $Unity`
# returns in milliseconds and reports whatever ran before it.
$proc = Start-Process -FilePath $Unity -ArgumentList ($unityArgs -join ' ') -PassThru
$null = $proc.Handle
$proc.WaitForExit()

Write-Host "editor exited with $($proc.ExitCode)"

if ($proc.ExitCode -ne 0) {
    Write-Host "see $LogFile" -ForegroundColor Red
    exit $proc.ExitCode
}

Select-String -Path $LogFile -Pattern '^\[roster\]' | ForEach-Object { "  " + $_.Line.Trim() }
