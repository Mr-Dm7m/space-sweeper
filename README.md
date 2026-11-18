# SpaceSweeper

A duplicate and junk file cleaner for Windows that fits in a **single 69 KB executable**.
No setup program, no admin rights, no network code, no telemetry.


## Why it exists

Most cleaners match **file names**. SpaceSweeper matches **bytes**:

1. group every file by exact byte length — a size shared by one file can never be a duplicate;
2. fingerprint only the real candidates (first 64 KB, SHA-256);
3. run a full SHA-256 only where the fingerprint already matched.

A 14 GB `Downloads` folder scans in **under a second**, because the large unique files are never read.

Duplicates with different names are caught. Files with the same name but different content are never flagged.

## What it finds

| Tab | What it does |
|---|---|
| **Duplicates** | Byte-exact groups, sorted by wasted space |
| **Large files** | Everything over 100 MB, biggest first |
| **Empty folders** | Post-order scan, so empty trees collapse together |
| **Junk** | `.tmp`, `.crdownload`, `.part`, `.partial`, `.dmp`, `Thumbs.db`, `.DS_Store`, `~$*.docx/xlsx/pptx` |

Also: HTML report export, `Ctrl+F` rescan, `Ctrl+A` select all in the current list, `Esc` cancel,
double-click a row to reveal it in Explorer, live drive-usage bar.


## Safety model

* Nothing is deleted permanently — removals go through `SHFileOperation` with `FOF_ALLOWUNDO` (Recycle Bin).
* One file per duplicate group is always kept; the rule is configurable (oldest / newest / shortest path / shallowest).
* `Windows`, `WinSxS`, `AppData`, `$Recycle.Bin`, junctions, symlinks and offline files are skipped by design.
* No network code exists in the binary.

## Build

There is no SDK requirement. Windows ships the C# compiler:

```powershell
.\build.ps1        # -> dist\SpaceSweeper.exe
.\make-icon.ps1    # regenerates assets\spacesweeper.ico from code
.\package.ps1      # -> release\ + SpaceSweeper-<version>.zip
```

`build.ps1` invokes `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe`, so the sources are
deliberately C# 5 compatible (no string interpolation, no null-propagation, no expression-bodied members).

```
src/Core.cs      scanner, hashing, empty-folder logic, Recycle Bin interop
src/Ui.cs        dark WinForms theme, custom controls, main window
src/Report.cs    standalone HTML report generator
src/Program.cs   entry point + headless --scan mode
```

## Running it on locked-down machines

Windows 11 with **Smart App Control** enabled refuses all unsigned executables
(`0x800711C7`, event 3077 in `Microsoft-Windows-CodeIntegrity/Operational`) and also refuses to load
one as an assembly. A signed binary is the only real fix, so this repo ships a second path:

```
portable\SpaceSweeper.cmd
```

That launcher compiles the same sources through `Add-Type` inside an already-trusted PowerShell host and
starts the app — same program, no binary on disk, ~1.9 s to window.

## Headless mode

```
SpaceSweeper.exe --scan "C:\Users\me\Downloads"
```

Prints a summary and writes `%TEMP%\spacesweeper-scan.txt`.

## Known limits

* Local NTFS/ReFS volumes; network shares work but are I/O bound.
* Files locked exclusively by another process are skipped (they cannot be hashed).
* Hard links are treated as separate files — they appear as duplicates but deleting one frees no space.
* 750,000 files per scan, guarded to keep memory bounded.

## License

MIT
