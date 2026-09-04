<#
.SYNOPSIS
    The helpers every shell end of simcli needs, in one file.

.DESCRIPTION
    Dot-source this from a script in tools/ once $program names the runner:

        . (Join-Path $PSScriptRoot '_shared.ps1')

    Dot-sourcing puts the functions in the calling script's own scope, so they
    read $program out of it at call time and every script stays a thing a shell
    runs on its own -- no module to install, no editor, no session.
#>

# THE CONTENT A RUN VERB IS PLAYED ON, AND NOT ONE FILE NAME IN SIGHT. Every
# verb that plays a run takes seven content files, and simcli declares all seven
# once -- the option, the file name and the parser together, in
# simcli/ContentFiles.cs. --content hands it the directory and it takes them out
# by those names.
#
# So a content file added to the runner needs no edit in any script here.
#
# -Elsewhere points single files somewhere else: @{ map = 'maps/second.txt' }
# scores another board and leaves the other six where they were. The key is the
# option's name, so the runner refuses an unknown one by name rather than this
# guessing.
function Get-ContentArguments {
    param([string]$Directory, [hashtable]$Elsewhere = @{})

    $arguments = @('--content', $Directory)

    # THE PARENTHESES ARE LOAD-BEARING. PowerShell binds ',' tighter than '+',
    # so `'--' + $option, $value` is `'--' + ($option, $value)` -- a string
    # concatenated with an ARRAY, which stringifies it space-separated into the
    # single token `--defense D:\d.txt`. The runner then refuses an option
    # nobody named, quoting a pair that looks correct, and the message points
    # at the option rather than at the joining. Measured: every -ContentFile
    # example in these scripts' own headers failed this way.
    foreach ($option in ($Elsewhere.Keys | Sort-Object)) {
        $arguments += @(('--' + $option), [string]$Elsewhere[$option])
    }

    return , $arguments
}

# The runner refuses by name and exits, rather than throwing: a record that
# will not replay has already said why in its own sentence, and a PowerShell
# stack trace on top of it buries the one line anybody needs to read.
#
# Every verb's output goes down a pipe, because no verb asks a question. The
# one that did was `play`, deleted with the shell specification it was built
# from; it needed the console handed to it whole, since a pipeline gives a
# native command's output a line at a time and a prompt has no newline after it.
function Invoke-SimCli {
    param([string[]]$CliArgs)

    & dotnet $program @CliArgs | Out-Host

    if ($LASTEXITCODE -ne 0) {
        Write-Host ""
        Write-Host "simcli $($CliArgs[0]) refused (exit $LASTEXITCODE); its reason is above." -ForegroundColor Red
        exit $LASTEXITCODE
    }
}

# Two texts compared, and the first line they disagree on named. A whole-file
# "they differ" is a message nobody can act on, and the first difference is
# nearly always the only one that was not caused by the ones above it.
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
