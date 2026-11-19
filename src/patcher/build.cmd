@echo off
setlocal
cd /d "%~dp0"

rem =====================================================================
rem  Build the localization tools (requires .NET Framework 4.x csc.exe).
rem  Output goes to ..\tools\
rem =====================================================================

set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo [ERROR] csc.exe not found. .NET Framework 4.x is required.
  exit /b 1
)

set "CECIL=..\tools\Mono.Cecil.dll"
if not exist "%CECIL%" (
  echo [ERROR] Missing ..\tools\Mono.Cecil.dll
  exit /b 1
)

set "OPTS=/nologo /codepage:65001 /target:exe /optimize+ /r:%CECIL%"

echo Building apply.exe ...
"%CSC%" %OPTS% /out:..\tools\apply.exe apply.cs || goto :error
echo Building setres.exe ...
"%CSC%" %OPTS% /out:..\tools\setres.exe setres.cs || goto :error
echo Building extract.exe ...
"%CSC%" %OPTS% /out:..\tools\extract.exe extract.cs || goto :error
echo Building scan.exe ...
"%CSC%" %OPTS% /out:..\tools\scan.exe scan.cs || goto :error
echo Building toasts.exe ...
"%CSC%" %OPTS% /out:..\tools\toasts.exe toasts.cs || goto :error

echo.
echo Done. Output: ..\tools\
exit /b 0

:error
echo.
echo [ERROR] Build failed.
exit /b 1
