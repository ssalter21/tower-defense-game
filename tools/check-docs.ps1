<#
.SYNOPSIS
    Fails when a document in docs/ states something the repository contradicts.

.DESCRIPTION
    Every other gate step asks whether the code still does what it did. None of
    them asks whether the prose still describes it, and prose is the surface in
    this repository that drifts without anything moving: a sentence goes stale
    when a file somewhere else changes, so nothing shows up in its diff and no
    test has an opinion. Three drifts were sitting here when this was written --
    a picture of a board that had been rebuilt since, an issue described as open
    that had been closed, and a count of records that was five short.

    The four invariants below are the ones that can be settled mechanically.
    Each is a claim a document makes about something else in the repository,
    and each refusal names the file, the claim and what is true instead.

    1. An issue cited as open or pending is open. Only a citation that asserts
       a state is checked -- "#56 is open for this check", "pending under #56".
       A bare mention of an issue number is a reference and not a claim, and
       reading every one of those as a claim would make the check unusable in a
       repository whose decision log cites hundreds of closed tickets. Reported
       speech is not a claim either: a note saying the vision still calls
       something pending is describing another document rather than asserting
       anything, and refusing it would be refusing a true sentence.
    2. A committed sheet or frame is at least as new as the content it was
       rendered from. The pictures are rendered through the real board, the real
       roster and the real prices, so a picture older than content/map.txt is a
       picture of a game this project no longer builds -- and a stale one goes
       on looking entirely reasonable, which is why nobody catches it by eye.
       The measure is the last commit to touch each file, so a picture
       re-captured in the same commit as the content passes, per rule 4 of
       AGENTS.md. It is a date and not the pixels: a rename or a rebase that
       re-stamps a picture buys it a pass it did not earn, and only a person
       looking at it can catch that. Where the date says no, the capture's own
       record is asked -- rendered-from.txt beside the pictures, naming the
       content each was drawn from -- because a re-render that came out
       pixel-identical leaves git nothing to date, and that is what happens
       every time the content that moved is not drawn in that picture. See
       tools/_rendered-from.ps1. A sheet docs/chrome/README.md lists as a
       chosen arrangement rather than as a baseline is named as exempt and
       reported n/a, because it records a decision rather than describing the
       board; an exemption naming a file that is no longer committed is itself
       a refusal.
    3. The record count docs/README.md quotes for docs/adr/ is the number of
       records in it.
    4. Every ADR a source file cites exists. A comment pointing at a record
       that was never written, or was renumbered, sends a reader nowhere. Only
       sim/, simcli/ and client/Assets/View/ are read, so a citation from
       tools/ or from a document is nobody's business here.

    Invariant 1 is the only one that needs the network, through `gh`. Where gh
    is missing, or is there but cannot reach GitHub as anybody, the check is
    SKIPPED and said to be skipped rather than counted as passing: an
    environment that cannot ask the question is not evidence that the documents
    are honest. A cited number that gh reaches GitHub and fails to find is a
    refusal, because a document citing an issue that does not exist is the same
    species of lie as one citing an issue that is closed.

    Sit-down ticks are not checked here. SitDownTests already pins every tick
    quoted in docs/sit-down.md against content/landmarks.txt, and a second
    opinion on the same file would only be a second thing to keep in step.

.EXAMPLE
    ./tools/check-docs.ps1
    Exit code 0 if every document agrees with the repository, 1 otherwise.
#>

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

$refusals = [System.Collections.Generic.List[string]]::new()
$skipped  = [System.Collections.Generic.List[string]]::new()

function Refuse([string]$sentence) {
    $refusals.Add($sentence)
    Write-Host "  FAIL  $sentence" -ForegroundColor Red
}

function Checked([string]$sentence) {
    Write-Host "  ok    $sentence" -ForegroundColor Green
}

function Skip([string]$what, [string]$why) {
    $skipped.Add($what)
    Write-Host "  SKIP  $why" -ForegroundColor Yellow
}

# A file an invariant was never meant to cover. Distinct from SKIP, which is a
# question this machine could not ask, and from ok, which is a question that was
# asked and answered: printing an exemption green would be the check claiming to
# have looked at something it deliberately did not.
function Exempt([string]$sentence) {
    Write-Host "  n/a   $sentence" -ForegroundColor DarkGray
}

# Tracked files only. An untracked draft under docs/ is somebody's working
# copy, and the gate has no business having an opinion about it.
function TrackedUnder([string]$path) {
    $files = & git -C $repoRoot ls-files -- $path
    if ($LASTEXITCODE -ne 0) { throw "git ls-files $path failed in $repoRoot (exit $LASTEXITCODE)." }
    @($files | Where-Object { $_ })
}

function LastCommitted([string]$path) {
    $stamp = & git -C $repoRoot log -1 --format=%cI -- $path
    if ($LASTEXITCODE -ne 0) { throw "git log for $path failed in $repoRoot (exit $LASTEXITCODE)." }
    if (-not $stamp) { return $null }
    [datetimeoffset]::Parse($stamp)
}

function Day([datetimeoffset]$when) { $when.ToString('d MMMM yyyy') }

# --- 1. An issue cited as open or pending is open -----------------------------
# The two shapes a state claim takes here: the issue first and the state after
# it ("#56 is open for exactly this check"), or the state first and the issue
# after it ("pending browser confirmation under #56"). Both are bounded to the
# clause the number sits in -- a full stop, a semicolon or a table cell ends the
# window -- so a claim in one sentence cannot be read onto a number in the next.
$claimShapes = @(
    '#(?<n>\d+)\b[^.;|]{0,40}?\b(?:is|are|remains|stays|remain)\b\s+(?:still\s+)?(?:open|pending)\b'
    '\b(?:open|pending)\b[^.;|]{0,40}?\bunder\s+#(?<n>\d+)\b'
)

# What turns the same words into a report of somebody else's claim. Searched
# backwards from the match to the start of its clause, so "the vision still
# says X is pending under #56" is read as a description of the vision and
# "#56 is open for this check" is read as an assertion.
$reportedSpeech = '\b(?:says?|said|saying|states?|stated|claims?|claimed|quotes?|quoted|according\s+to)\b'

Write-Host "1. An issue a document calls open or pending is open."

$docs = TrackedUnder 'docs' | Where-Object { $_ -like '*.md' }
$claims = [System.Collections.Generic.List[object]]::new()

# A markdown link wraps the number in a URL that carries full stops, and the
# clause window would end inside the href rather than at the end of the
# sentence. Collapse [#56](url) to #56 before matching.
function Unlinked([string]$text) { [regex]::Replace($text, '\[#(\d+)\]\([^)]*\)', '#$1') }

foreach ($doc in $docs) {
    $lines = @(Get-Content (Join-Path $repoRoot $doc))
    for ($index = 0; $index -lt $lines.Count; $index++) {
        # docs/ is hard-wrapped at about 110 columns, so a claim straddles a
        # line break as often as not. The following line joins on unless it is
        # blank -- a blank line is a paragraph break and nothing reads across
        # one -- and only a match starting within this line is taken, so each
        # claim is found once and reported at the line it starts on.
        $text = Unlinked $lines[$index]
        $ownLength = $text.Length
        if ($index + 1 -lt $lines.Count -and $lines[$index + 1].Trim()) {
            $text = $text + ' ' + (Unlinked $lines[$index + 1])
        }

        foreach ($shape in $claimShapes) {
            foreach ($match in [regex]::Matches($text, $shape, 'IgnoreCase')) {
                if ($match.Index -ge $ownLength) { continue }
                $clauseStart = $text.LastIndexOfAny([char[]]('.', ';', '|'), $match.Index) + 1
                $before = $text.Substring($clauseStart, $match.Index - $clauseStart)
                if ([regex]::IsMatch($before, $reportedSpeech, 'IgnoreCase')) { continue }
                $claims.Add([pscustomobject]@{
                    File   = $doc
                    Line   = $index + 1
                    Issue  = [int]$match.Groups['n'].Value
                    Phrase = $match.Value.Trim()
                })
            }
        }
    }
}

# Whether GitHub can be asked at all, separately from what it answers. gh
# installed but signed in as nobody exits non-zero on every query, and reading
# that as "the document is lying" would turn a laptop without a token into a
# repository full of false refusals.
$ghReady = $false
if (Get-Command gh -ErrorAction SilentlyContinue) {
    & gh auth status 2>$null | Out-Null
    $ghReady = ($LASTEXITCODE -eq 0)
}

if (-not $ghReady) {
    Skip 'issues cited as open' "GitHub cannot be asked from here, so the state of an issue cannot be read. $($claims.Count) state claim(s) in $($docs.Count) documents went unchecked."
} else {
    $states = @{}
    foreach ($issue in ($claims.Issue | Sort-Object -Unique)) {
        # gh's own diagnostics stay on stderr and go to the console, so a
        # notice it decided to print does not arrive here as a parse failure.
        $json = & gh issue view $issue --json state,title
        if ($LASTEXITCODE -ne 0) {
            Refuse "#$issue is cited as open but GitHub would not say what it is (gh exit $LASTEXITCODE); its reason is above."
            continue
        }
        $states[$issue] = $json | ConvertFrom-Json
    }

    foreach ($claim in $claims) {
        if (-not $states.ContainsKey($claim.Issue)) { continue }
        $issue = $states[$claim.Issue]
        if ($issue.state -eq 'OPEN') {
            Checked "$($claim.File):$($claim.Line) says `"$($claim.Phrase)`", and #$($claim.Issue) is open."
        } else {
            Refuse "$($claim.File):$($claim.Line) says `"$($claim.Phrase)`", and #$($claim.Issue) is $($issue.state.ToLower()): $($issue.title)"
        }
    }

    if (-not $claims.Count) {
        Checked "No document in the $($docs.Count) under docs/ calls an issue open or pending."
    }
}

# --- 2. A picture is newer than the content it was rendered from --------------
# The authored content a sheet or a frame draws -- the board, the roster, the
# ladder and the scenery standing on the board -- is named in
# tools/_rendered-from.ps1, beside the digest the capture tools record against
# every picture they write. One list, so the capture and this cannot disagree
# about what a picture is a claim about.
. (Join-Path $PSScriptRoot '_rendered-from.ps1')

$rendered = Get-DrawnContent
$drawnStamp = Get-DrawnContentStamp $repoRoot

Write-Host ""
Write-Host "2. A committed sheet or frame is at least as new as the content it was rendered from."

$contentMoved = $null
$newestContent = $null
foreach ($file in $rendered) {
    $when = LastCommitted $file
    if (-not $when) { throw "$file is not committed, so there is nothing to date the pictures against." }
    if (-not $contentMoved -or $when -gt $contentMoved) {
        $contentMoved = $when
        $newestContent = $file
    }
}

$pictures = @(TrackedUnder 'docs/chrome') + @(TrackedUnder 'docs/frames') |
            Where-Object { $_ -like '*.png' } | Sort-Object

if (-not $pictures) { throw "No committed picture found under docs/chrome/ or docs/frames/." }

# The sheets that record a decision instead of describing the board: the ones
# docs/chrome/README.md lists as the chosen arrangement rather than as one of
# the baselines beneath it. What such a sheet shows is where the chrome is
# going, so rebuilding the board cannot make it stale the way it makes a
# baseline stale, and dating it against content/ asks it the wrong question.
# Named one file at a time on purpose -- a pattern here would exempt the next
# sheet dropped into the directory as well.
# The beside-prop sheet is the same species from the other direction: it
# draws ten characters that no row in content/units.txt points at, standing
# beside props no row names, so dating it against the roster asks it about
# content it does not contain. What would make it stale is somebody changing
# which prop stands beside which tower, and that is a line in docs/roster.md.
# The melee-lines sheet is drawn the same way and exempt for the same reason,
# from the other direction again: its nine rungs DO have rows now, but a set
# sheet is drawn from a set file -- models, props, atlases and one pose -- and
# reads none of the four authored files. Move a price or a range and it renders
# the same pixels, so a date against content/ would ask it a question it cannot
# answer. What would make it stale is a look in docs/roster.md moving, which is
# a person's job to notice and not this check's. The caster-lines and
# pierce-turret-lines sheets are the same sheet for the other six lines and
# exempt on the same grounds.
$decisionSheets = @(
    'docs/chrome/chosen-build-phase.png'
    'docs/frames/roster/beside-props-sheet.png'
    'docs/frames/roster/melee-lines-sheet.png'
    'docs/frames/roster/caster-lines-sheet.png'
    'docs/frames/roster/pierce-turret-lines-sheet.png'
)

# An exemption for a file that is no longer committed covers nothing, and it
# would go on reading as though it still applied to something.
foreach ($sheet in $decisionSheets) {
    if ($pictures -notcontains $sheet) {
        Refuse "$sheet is named as exempt from this invariant and is not a committed picture, so the exemption covers nothing. Restore the file, or drop its name from the exemption."
    }
}

foreach ($picture in $pictures) {
    if ($decisionSheets -contains $picture) {
        Exempt "$picture records a decision rather than describing the board, so its date is not compared."
        continue
    }

    $when = LastCommitted $picture
    # At least as new, not newer. A picture re-captured in the same commit as
    # the content that moved -- which is what AGENTS.md rule 4 asks for --
    # carries the identical commit stamp.
    if ($when -ge $contentMoved) {
        Checked "$picture was committed $(Day $when), and $newestContent, the last of the content it draws to move, moved $(Day $contentMoved)."
        continue
    }

    # A DATE CANNOT SEE A RE-RENDER THAT CAME OUT IDENTICAL. Where the content
    # that moved is not drawn in this particular picture -- which is most
    # pictures for most changes -- re-capturing rewrites the same pixels, there
    # is no diff, and the picture keeps the commit stamp it had. So the capture's
    # own record is asked second: it names the content each picture was drawn
    # from, and the tool that drew it is what wrote the line.
    $recorded = Read-RenderedFrom (Split-Path (Join-Path $repoRoot $picture) -Parent)
    $name = Split-Path $picture -Leaf

    if (-not $recorded.ContainsKey($name)) {
        Refuse "$picture was captured $(Day $when) and shows the content as it was then; $newestContent moved on $(Day $contentMoved), and nothing beside the picture says what it was drawn from. Re-capture it -- the capture writes rendered-from.txt as it goes."
    } elseif ($recorded[$name] -ne $drawnStamp) {
        Refuse "$picture was drawn from content $($recorded[$name]) and this repository holds $drawnStamp, so the picture is of a game it no longer builds. Re-capture it."
    } else {
        Checked "$picture was committed $(Day $when), before $newestContent moved on $(Day $contentMoved) -- and it was drawn from content $drawnStamp, which is what this repository holds, so re-rendering it draws the same pixels."
    }
}

# --- 3. The record count docs/README.md quotes is the record count ------------
Write-Host ""
Write-Host "3. The number of records docs/README.md quotes for docs/adr/ is the number there are."

$adrFiles = TrackedUnder 'docs/adr' | Where-Object { (Split-Path $_ -Leaf) -match '^\d{4}-.+\.md$' }
$readme = Join-Path $repoRoot 'docs/README.md'
$quoted = [regex]::Match((Get-Content $readme -Raw), '(?m)^.*\badr/.*?(?<count>\d+)\s+records\b.*$')

if (-not $quoted.Success) {
    Refuse "docs/README.md no longer quotes a record count for docs/adr/, so this check has nothing to compare the $($adrFiles.Count) records against. Restore the count or delete this invariant."
} elseif ([int]$quoted.Groups['count'].Value -ne $adrFiles.Count) {
    Refuse "docs/README.md says docs/adr/ holds $($quoted.Groups['count'].Value) records; it holds $($adrFiles.Count)."
} else {
    Checked "docs/README.md says $($adrFiles.Count) records, and docs/adr/ holds $($adrFiles.Count)."
}

# --- 4. Every ADR a source file cites exists ----------------------------------
# Both spellings the source uses: ADR-0051 in prose, adr/0048 where the comment
# is pointing at the file.
Write-Host ""
Write-Host "4. Every ADR cited in sim/, simcli/ and client/Assets/View/ exists."

$sourceRoots = @('sim', 'simcli', 'client/Assets/View')
$sourceKinds = @('.cs', '.uxml', '.uss', '.asmdef', '.json')
$numbers = $adrFiles | ForEach-Object { (Split-Path $_ -Leaf).Substring(0, 4) }

$citations = [System.Collections.Generic.List[object]]::new()
foreach ($root in $sourceRoots) {
    foreach ($file in (TrackedUnder $root)) {
        if ($sourceKinds -notcontains [System.IO.Path]::GetExtension($file)) { continue }
        $number = 0
        foreach ($line in (Get-Content (Join-Path $repoRoot $file))) {
            $number++
            foreach ($match in [regex]::Matches($line, 'adr[-/](?<n>\d{4})', 'IgnoreCase')) {
                $citations.Add([pscustomobject]@{ File = $file; Line = $number; Adr = $match.Groups['n'].Value })
            }
        }
    }
}

$missing = @($citations | Where-Object { $numbers -notcontains $_.Adr })
foreach ($citation in $missing) {
    Refuse "$($citation.File):$($citation.Line) cites ADR-$($citation.Adr), and docs/adr/ has no record with that number."
}
if (-not $missing) {
    $distinct = ($citations.Adr | Sort-Object -Unique)
    Checked "$($citations.Count) citations of $($distinct.Count) records, every one of them in docs/adr/: $($distinct -join ', ')"
}

# --- Report -------------------------------------------------------------------
Write-Host ""
if ($refusals.Count) {
    Write-Host "$($refusals.Count) claim(s) in docs/ are not true of this repository." -ForegroundColor Red
    Write-Host "Edit the document, or move the thing it describes. Do not relax the check."
    exit 1
}

if ($skipped.Count) {
    Write-Host "Every claim this machine could check is true; $($skipped.Count) check(s) skipped: $($skipped -join ', ')." -ForegroundColor Yellow
    exit 0
}

Write-Host "Every claim checked is true of this repository." -ForegroundColor Green
exit 0
