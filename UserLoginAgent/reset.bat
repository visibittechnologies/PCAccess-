@echo off
setlocal
cd /d "%~dp0"
title PCAccess - UserLoginAgent Reset

echo.
echo ========================================================================
echo                 PCAccess UserLoginAgent - Clean Reset
echo ========================================================================
echo.

if exist "bin\Debug\UserLoginAgent.exe" (
    "bin\Debug\UserLoginAgent.exe" reset
) else if exist "UserLoginAgent.exe" (
    "UserLoginAgent.exe" reset
) else (
    echo [ERROR] UserLoginAgent.exe not found! Please build solution first.
    pause
    exit /b 1
)

pause
