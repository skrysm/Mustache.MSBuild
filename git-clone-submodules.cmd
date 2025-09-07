::
:: Call this script when the current repository contains git submodules and you forgot to clone it
:: with "--recurse-submodules". (The end effect will be the same as if you had clone this repository
:: with "--recurse-submodules".)
::
@echo off
setlocal
title git clone submodules

powershell -ExecutionPolicy Unrestricted -Command Write-Host -Foreground Cyan 'Cloning submodules...'
echo.

cd /D "%~dp0\"
git submodule init
git submodule update

echo.
pause
