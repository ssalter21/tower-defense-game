<#
.SYNOPSIS
    What a committed sheet or frame was drawn from, recorded beside it.

.DESCRIPTION
    Dot-source this from a script in tools/:

        . (Join-Path $PSScriptRoot '_rendered-from.ps1')

    A picture under docs/ is a claim about the authored content it was drawn
    from, and the claim goes stale when that content moves. check-docs.ps1 asks
    the question with a date: a picture whose last commit is older than
    content/units.txt's is a picture of a game this repository no longer builds.

    A DATE ONLY ANSWERS IT WHERE RE-RENDERING CHANGES THE BYTES. A capture that
    draws the same pixels leaves git nothing to record, so the picture keeps the
    commit stamp it had -- and a date cannot tell that picture apart from one
    nobody re-captured. It is not a rare case: it is what happens every time the
    content that moved is not drawn in that particular picture, which is most of
    them for most changes.

    So a capture writes down what it drew from. Each directory of committed
    pictures carries a rendered-from.txt of one line per picture: the file name
    and one digest over the authored files every picture draws. A checker
    recomputes the digest and compares, which answers correctly for a re-render
    that came out identical.

    THE RECORD IS WRITTEN BY THE CAPTURE AND NEVER BY HAND. It says a picture
    was drawn, and only the thing that drew it knows that. A capture updates the
    lines for the pictures it actually wrote and leaves the rest alone, because
    one run captures the ticks it was asked for and says nothing about the
    others.
#>

# The authored files a sheet or a frame draws: the board, the roster, the
# ladder, and the scenery standing on the board. Named here once, so the capture
# and the checker cannot disagree about what a picture is a claim about.
function Get-DrawnContent {
    return , @(
        'content/map.txt'
        'content/units.txt'
        'content/upgrades.txt'
        'content/dressing.txt'
    )
}

# One digest over all four in that fixed order, so a picture records a single
# short string rather than four. The bytes as the files hold them: a picture is
# drawn from what a parser read, and a re-wrapped comment that moves no number
# still moves this. That is the safe direction -- it asks for a re-capture that
# was not needed rather than missing one that was.
function Get-DrawnContentStamp {
    param([string]$RepoRoot)

    $sha = [System.Security.Cryptography.SHA256]::Create()

    try {
        $digests = [System.Collections.Generic.List[byte]]::new()

        foreach ($file in (Get-DrawnContent)) {
            $path = Join-Path $RepoRoot $file

            if (-not (Test-Path $path)) {
                throw "$file is missing, so there is nothing to stamp a picture against."
            }

            $digests.AddRange($sha.ComputeHash([System.IO.File]::ReadAllBytes($path)))
        }

        $stamp = [System.BitConverter]::ToString($sha.ComputeHash($digests.ToArray())) -replace '-', ''

        return $stamp.Substring(0, 16)
    }
    finally {
        $sha.Dispose()
    }
}

# Where the record for a directory of pictures lives.
function Get-RenderedFromPath {
    param([string]$Directory)

    return Join-Path $Directory 'rendered-from.txt'
}

# The record as a map of file name to stamp. A directory with no record yet
# reads as an empty map rather than as an error: the first capture into one
# writes it.
function Read-RenderedFrom {
    param([string]$Directory)

    $record = @{}
    $path = Get-RenderedFromPath $Directory

    if (-not (Test-Path $path)) {
        return $record
    }

    foreach ($line in (Get-Content -LiteralPath $path)) {
        $fields = $line.Trim() -split '\s+'

        if ($fields.Count -ne 3 -or $fields[0] -ne 'picture') {
            continue
        }

        $record[$fields[1]] = $fields[2]
    }

    return $record
}

# Writes the stamp against every picture named and leaves the rest of the record
# where it was. Sorted by name, so two captures in either order leave the same
# file and a diff of one is the lines that moved.
function Update-RenderedFrom {
    param([string]$Directory, [string[]]$Pictures, [string]$Stamp)

    if (-not $Pictures) {
        return
    }

    $record = Read-RenderedFrom $Directory

    foreach ($picture in $Pictures) {
        $record[$picture] = $Stamp
    }

    $lines = @(
        '# What each picture in this directory was drawn from: one line per'
        '# picture, and a digest over content/map.txt, content/units.txt,'
        '# content/upgrades.txt and content/dressing.txt as they stood when it'
        '# was captured.'
        '#'
        '# WRITTEN BY THE CAPTURE AND NEVER BY HAND. It says a picture was drawn,'
        '# and only the thing that drew it knows that. tools/check-docs.ps1 reads'
        '# it where a commit date cannot answer -- a re-render that came out'
        '# pixel-identical leaves git nothing to date.'
        '#'
        '#       picture                        drawn from'
        ''
    )

    foreach ($name in ($record.Keys | Sort-Object)) {
        $lines += ('picture  {0}  {1}' -f $name.PadRight(30), $record[$name])
    }

    Set-Content -LiteralPath (Get-RenderedFromPath $Directory) -Value $lines -Encoding utf8
}

# The names of the PNGs a capture actually wrote, told from the ones that were
# already sitting in the directory by their write times. A capture rewrites the
# file whether or not the pixels moved, which is the whole point: the write is
# the evidence the picture was drawn again.
function Get-WrittenPictures {
    param([string]$Directory, [hashtable]$Before)

    $written = @()

    foreach ($file in (Get-ChildItem -LiteralPath $Directory -Filter '*.png')) {
        if (-not $Before.ContainsKey($file.Name) -or $Before[$file.Name] -ne $file.LastWriteTimeUtc) {
            $written += $file.Name
        }
    }

    return , $written
}

# The write times to compare against afterwards.
function Get-PictureWriteTimes {
    param([string]$Directory)

    $times = @{}

    if (-not (Test-Path $Directory)) {
        return $times
    }

    foreach ($file in (Get-ChildItem -LiteralPath $Directory -Filter '*.png')) {
        $times[$file.Name] = $file.LastWriteTimeUtc
    }

    return $times
}
