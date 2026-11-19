@echo off
setlocal
cd /d "%~dp0"
title PCAccess - FileAccessAgent Reset

echo.
echo ========================================================================
echo                 PCAccess FileAccessAgent - Clean Reset
echo ========================================================================
echo.

if exist "bin\Debug\FileAccessAgent.exe" (
    "bin\Debug\FileAccessAgent.exe" --reset
) else if exist "FileAccessAgent.exe" (
    "FileAccessAgent.exe" --reset
) else (
    echo [ERROR] FileAccessAgent.exe not found! Please build solution first.
    pause
    exit /b 1
)

pause
