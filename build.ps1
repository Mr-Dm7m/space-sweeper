$ErrorActionPreference = "Stop"
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$csc  = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$out  = Join-Path $root "dist\SpaceSweeper.exe"

New-Item -ItemType Directory -Force -Path (Join-Path $root "dist") | Out-Null

$src = Get-ChildItem (Join-Path $root "src") -Filter *.cs | ForEach-Object { $_.FullName }

$ico = Join-Path $root "assets\spacesweeper.ico"

$cargs = @(
  "/nologo", "/target:winexe", "/optimize+", "/platform:anycpu",
  "/out:$out",
  "/r:System.dll", "/r:System.Core.dll", "/r:System.Drawing.dll", "/r:System.Windows.Forms.dll"
) + $src

if (Test-Path $ico) { $cargs += ("/win32icon:" + $ico) }

& $csc $cargs

if ($LASTEXITCODE -ne 0) { throw "compile failed: $LASTEXITCODE" }

$fi = Get-Item $out
Write-Host ("OK  {0}  {1:N0} KB" -f $fi.FullName, ($fi.Length / 1KB))
