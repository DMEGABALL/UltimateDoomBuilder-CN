@echo off
rem Apply the Simplified Chinese language pack.
rem Usage: apply.cmd ["path\to\Ultimate Doom Builder"]
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0apply.ps1" %*
