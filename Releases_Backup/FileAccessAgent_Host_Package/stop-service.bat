@echo off
:: ============================================================================
:: PCAccess Agent - Stop Windows Service
:: ============================================================================
setlocal EnableDelayedExpansion

net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [ELEVATION] Requesting Administrator Privileges...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

cd /d "%~dp0"

set "EXE_PATH=FileAccessAgent.exe"
if not exist "%EXE_PATH%" (
    if exist "bin\Debug\FileAccessAgent.exe" (
        set "EXE_PATH=bin\Debug\FileAccessAgent.exe"
    ) else if exist "bin\Debug\net10.0-windows\FileAccessAgent.exe" (
        set "EXE_PATH=bin\Debug\net10.0-windows\FileAccessAgent.exe"
    )
)

echo Stopping PCAccessAgent Service...
"%EXE_PATH%" --stop

echo.
"%EXE_PATH%" --status
echo.
pause
