@echo off
setlocal
title git clean

powershell -ExecutionPolicy Unrestricted -Command Write-Host -Foreground Cyan 'Removing ignored and untracked files and directories...'
echo.

:: Delete all untracked and ignored files and directories.
:: NOTE: We need to use '-x' instead of '-X' here. It's not really clear
::   what's the difference but when using '-X' some directories don't get
::   deleted (for example, the "bin/Debug" directories in C# projects).
git clean -f -d -x "%~dp0\"

:: Clean all submodules. (The above command only cleans the current repo.)
git submodule foreach --recursive git clean -f -d -x

echo.
pause
