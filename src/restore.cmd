@echo off
rem Restore the original English build.
rem Usage: restore.cmd ["path\to\Ultimate Doom Builder"]
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0restore.ps1" %*
