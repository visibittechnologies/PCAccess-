@echo off
title PCAccess User Login Agent - Stop
cd /d "%~dp0"

set "EXE_PATH=UserLoginAgent.exe"
if not exist "%EXE_PATH%" (
    if exist "bin\Debug\UserLoginAgent.exe" (
        set "EXE_PATH=bin\Debug\UserLoginAgent.exe"
    )
)

if not exist "%EXE_PATH%" (
    echo [ERROR] UserLoginAgent.exe not found! Please build the solution first.
    pause
    exit /b 1
)

"%EXE_PATH%" stop

echo.
pause
