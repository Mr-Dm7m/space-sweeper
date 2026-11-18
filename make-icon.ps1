$ErrorActionPreference = "Stop"
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$assetsDir = Join-Path $root "assets"
New-Item -ItemType Directory -Force -Path $assetsDir | Out-Null
$tmp = Join-Path $env:TEMP "ss-icons"
New-Item -ItemType Directory -Force -Path $tmp | Out-Null

function New-RoundPath([int]$x, [int]$y, [int]$w, [int]$h, [int]$r) {
  $p = New-Object System.Drawing.Drawing2D.GraphicsPath
  $d = $r * 2
  $p.AddArc($x, $y, $d, $d, 180, 90)
  $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
  $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
  $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
  $p.CloseFigure()
  return $p
}

function New-IconPng([int]$size, [string]$file) {
  $bmp = New-Object System.Drawing.Bitmap $size, $size
  $g = [System.Drawing.Graphics]::FromImage($bmp)
  $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
  $g.Clear([System.Drawing.Color]::Transparent)

  $pad = [int]($size * 0.03)
  $path = New-RoundPath $pad $pad ($size - 2 * $pad) ($size - 2 * $pad) ([int]($size * 0.22))
  $rect = New-Object System.Drawing.Rectangle 0, 0, $size, $size
  $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush $rect, ([System.Drawing.Color]::FromArgb(108,140,255)), ([System.Drawing.Color]::FromArgb(139,92,246)), 45.0
  $g.FillPath($brush, $path)

  $pen = New-Object System.Drawing.Pen ([System.Drawing.Color]::White), ([float]($size * 0.07))
  $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
  $pen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round

  $g.DrawArc($pen, [int]($size * 0.22), [int]($size * 0.22), [int]($size * 0.50), [int]($size * 0.50), 200, 250)
  $g.DrawLine($pen, [int]($size * 0.50), [int]($size * 0.52), [int]($size * 0.74), [int]($size * 0.76))

  $g.Dispose()
  $bmp.Save($file, [System.Drawing.Imaging.ImageFormat]::Png)
  $bmp.Dispose()
  $pen.Dispose(); $brush.Dispose(); $path.Dispose()
}

$sizes = @(256, 64, 48, 32, 16)
$files = @()
foreach ($s in $sizes) {
  $f = Join-Path $tmp ("icon-{0}.png" -f $s)
  New-IconPng $s $f
  $files += $f
}

$datas = @()
foreach ($f in $files) { $datas += ,([System.IO.File]::ReadAllBytes($f)) }

$icoPath = Join-Path $assetsDir "spacesweeper.ico"
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter $fs
$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$sizes.Count)

$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
  $s = $sizes[$i]
  $data = $datas[$i]
  $dim = 0
  if ($s -ne 256) { $dim = $s }
  $bw.Write([byte]$dim)
  $bw.Write([byte]$dim)
  $bw.Write([byte]0)
  $bw.Write([byte]0)
  $bw.Write([UInt16]1)
  $bw.Write([UInt16]32)
  $bw.Write([UInt32]$data.Length)
  $bw.Write([UInt32]$offset)
  $offset += $data.Length
}
foreach ($d in $datas) { $bw.Write([byte[]]$d) }
$bw.Flush(); $bw.Close(); $fs.Close()

Copy-Item $files[0] (Join-Path $assetsDir "spacesweeper-256.png") -Force
Write-Host ("icon: {0} ({1:N0} bytes)" -f $icoPath, (Get-Item $icoPath).Length)
