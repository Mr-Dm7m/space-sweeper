@echo off
rem SpaceSweeper portable launcher - no install, no admin, no signing required.
start "" /b powershell.exe -NoProfile -STA -ExecutionPolicy Bypass -WindowStyle Hidden -File "%~dp0SpaceSweeper.ps1"
