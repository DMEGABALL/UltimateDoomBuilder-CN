@echo off
rem =====================================================================
rem  Build the Simplified Chinese reference manual (Refmanual.chm)
rem  Requires HTML Help Workshop (hhc.exe)
rem =====================================================================
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1"
exit /b %ERRORLEVEL%
