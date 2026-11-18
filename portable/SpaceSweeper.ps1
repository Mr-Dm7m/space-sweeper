$ErrorActionPreference = "Stop"

# SpaceSweeper - portable launcher.
# Compiles the C# sources in .\src into memory and starts the app.
# Works on machines where Smart App Control blocks unsigned .exe files.

$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$srcDir = Join-Path $here "src"
if (-not (Test-Path $srcDir)) { $srcDir = Join-Path (Split-Path -Parent $here) "src" }
if (-not (Test-Path $srcDir)) { throw "No src folder found next to the launcher." }

Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$src = Get-ChildItem $srcDir -Filter *.cs | ForEach-Object { $_.FullName }
Add-Type -Path $src -ReferencedAssemblies System.Windows.Forms, System.Drawing -ErrorAction Stop

[System.Windows.Forms.Application]::EnableVisualStyles()
[System.Windows.Forms.Application]::SetCompatibleTextRenderingDefault($false)
try { [SpaceSweeper.Native]::SetProcessDPIAware() | Out-Null } catch { }

$form = New-Object SpaceSweeper.MainForm
[System.Windows.Forms.Application]::Run($form)
