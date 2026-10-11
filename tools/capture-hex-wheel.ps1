# Renders the hex wheel's candidates for issue #323 -- what an empty hex holds,
# where the wave is composed, and whether the wheel opens on hover or on click
# -- on the same board and the same hex at 1600x900, then lays the eight frames
# out on one labelled contact sheet so the three decisions are taken from it.
#
# The spec is docs/chrome/hex-wheel/spec.json and this passes it through
# capture-ui-previews.ps1. Each shot names a candidate in
# client/Assets/Editor/ChromeCandidates/HexWheelCandidates.cs, which opens
# client/Assets/View/HexWheel.cs on the hex.
#
# The contact sheet is drawn here from the frames the capture wrote and the
# labels in its manifest, so it can only hold what was rendered. It is stamped
# in rendered-from.txt beside the frames.
#
# -batchmode -executeMethod, so the editor must be CLOSED. Never edit a file
# while a run is going: it forces a synchronous recompile and the run dies with
# exit 3 and no results.

param(
    [string]$Unity = "C:\Program Files\Unity\Hub\Editor\6000.5.6f1\Editor\Unity.exe",
    [string]$Spec,
    [string]$SheetName = 'hex-wheel-sheet.png'
)

$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path

if (-not $Spec) { $Spec = Join-Path $repoRoot 'docs/chrome/hex-wheel/spec.json' }

& (Join-Path $PSScriptRoot 'capture-ui-previews.ps1') -Unity $Unity -Spec $Spec `
    -LogFile (Join-Path $repoRoot 'capture-hex-wheel.log')

if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }

. (Join-Path $PSScriptRoot '_rendered-from.ps1')
Add-Type -AssemblyName System.Drawing

$outDir = (Get-Content -LiteralPath $Spec -Raw | ConvertFrom-Json).outDir
if (-not [System.IO.Path]::IsPathRooted($outDir)) { $outDir = Join-Path $repoRoot $outDir }

$manifest = Get-Content -LiteralPath (Join-Path $outDir 'manifest.json') -Raw | ConvertFrom-Json
$shots = @($manifest.shots)

if ($shots.Count -eq 0) { throw "The capture wrote no frames, so there is no contact sheet to draw." }

$columns = 2
$cellWidth = 800
$cellHeight = 450
$captionHeight = 44
$gap = 8
$rows = [Math]::Ceiling($shots.Count / $columns)
$width = ($columns * $cellWidth) + (($columns + 1) * $gap)
$height = ($rows * ($cellHeight + $captionHeight)) + (($rows + 1) * $gap)

$sheet = [System.Drawing.Bitmap]::new($width, $height)
$canvas = [System.Drawing.Graphics]::FromImage($sheet)
$font = [System.Drawing.Font]::new('Segoe UI', 15, [System.Drawing.FontStyle]::Bold, [System.Drawing.GraphicsUnit]::Pixel)
$ink = [System.Drawing.SolidBrush]::new([System.Drawing.Color]::FromArgb(230, 234, 242))

try {
    $canvas.Clear([System.Drawing.Color]::FromArgb(16, 18, 23))
    $canvas.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $canvas.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

    for ($index = 0; $index -lt $shots.Count; $index++) {
        $shot = $shots[$index]
        $left = $gap + (($index % $columns) * ($cellWidth + $gap))
        $top = $gap + ([Math]::Floor($index / $columns) * ($cellHeight + $captionHeight + $gap))

        $frame = [System.Drawing.Image]::FromFile((Join-Path $outDir $shot.png))

        try {
            $canvas.DrawString($shot.label, $font, $ink, [System.Drawing.RectangleF]::new($left, $top + 10, $cellWidth, $captionHeight))
            $canvas.DrawImage($frame, $left, $top + $captionHeight, $cellWidth, $cellHeight)
        }
        finally {
            $frame.Dispose()
        }
    }

    $sheet.Save((Join-Path $outDir $SheetName), [System.Drawing.Imaging.ImageFormat]::Png)
}
finally {
    $ink.Dispose()
    $font.Dispose()
    $canvas.Dispose()
    $sheet.Dispose()
}

Write-Host "contact sheet: $(Join-Path $outDir $SheetName)"

Update-RenderedFrom $outDir @($SheetName) (Get-DrawnContentStamp $repoRoot)

exit 0
