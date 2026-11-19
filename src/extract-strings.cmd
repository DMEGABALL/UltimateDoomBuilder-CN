@echo off
rem Dump all strings from Builder.exe and plugin assemblies.
rem Usage: extract-strings.cmd <out.tsv> ["path\to\Ultimate Doom Builder"]
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0extract-strings.ps1" %*
