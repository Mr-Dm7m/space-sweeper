$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$rel  = Join-Path $root "release"
$ver  = "1.0.0"

if (Test-Path $rel) { Remove-Item $rel -Recurse -Force }
New-Item -ItemType Directory -Force -Path $rel | Out-Null
New-Item -ItemType Directory -Force -Path (Join-Path $rel "portable\src") | Out-Null

Copy-Item (Join-Path $root "dist\SpaceSweeper.exe") $rel -Force
Copy-Item (Join-Path $root "portable\SpaceSweeper.cmd") (Join-Path $rel "portable") -Force
Copy-Item (Join-Path $root "portable\SpaceSweeper.ps1") (Join-Path $rel "portable") -Force
Copy-Item (Join-Path $root "src\*.cs") (Join-Path $rel "portable\src") -Force

Copy-Item (Join-Path $root "assets\spacesweeper-256.png") $rel -Force

$readme = @"
SpaceSweeper $ver
=================

Duplicate and junk file scanner for Windows.

WHAT IT DOES
  Scans folders you choose and lists:
    - exact duplicate files  (compared by content, not by name)
    - the largest files on disk
    - empty folders
    - temp / junk files

HOW TO RUN
  1. Try:  SpaceSweeper.exe  (double-click)
  2. If Windows says "Application Control policy blocked this file"
     (Smart App Control), use the portable launcher instead:
         portable\SpaceSweeper.cmd

  No install, no admin rights, nothing to uninstall. Delete this
  folder and the app is gone.

SAFETY
  Every removal goes to the Recycle Bin. Right-click > Restore in the
  Recycle Bin brings any file back. One file per duplicate group is
  always kept, and you review the list before anything moves.

  The app contains no network code. Nothing is uploaded, ever.

TIP
  Press Ctrl+F to rescan, Ctrl+A to select everything in the current
  list, Esc to cancel a running scan. Double-click a row to show the
  file in Explorer.

space-sweeper.app
"@
Set-Content -Path (Join-Path $rel "README.txt") -Value $readme -Encoding UTF8

$zip = Join-Path $root ("SpaceSweeper-{0}.zip" -f $ver)
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $rel "*") -DestinationPath $zip -CompressionLevel Optimal

Write-Host ("release : {0}" -f $rel)
Get-ChildItem $rel -Recurse -File | ForEach-Object {
    Write-Host ("  {0,-46} {1,8:N0} KB" -f $_.FullName.Substring($rel.Length + 1), ($_.Length / 1KB))
}
Write-Host ("zip     : {0}  ({1:N0} KB)" -f $zip, ((Get-Item $zip).Length / 1KB))
