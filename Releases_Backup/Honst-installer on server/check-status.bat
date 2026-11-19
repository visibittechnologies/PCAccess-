@echo off
:: ============================================================================
:: PCAccess Agent - Check Service Status
:: ============================================================================
setlocal EnableDelayedExpansion

cd /d "%~dp0"

set "EXE_PATH=FileAccessAgent.exe"
if not exist "%EXE_PATH%" (
    if exist "bin\Debug\FileAccessAgent.exe" (
        set "EXE_PATH=bin\Debug\FileAccessAgent.exe"
    ) else if exist "bin\Debug\net10.0-windows\FileAccessAgent.exe" (
        set "EXE_PATH=bin\Debug\net10.0-windows\FileAccessAgent.exe"
    )
)

if not exist "%EXE_PATH%" (
    echo [ERROR] FileAccessAgent.exe was not found!
    pause
    exit /b 1
)

echo ================================================================
echo    PCAccess Agent - Service Status
echo ================================================================
echo.

"%EXE_PATH%" --status

echo.
echo ================================================================
echo Latest log entries:
if exist "logs" (
    powershell -NoProfile -Command "Get-ChildItem -Path logs -Filter 'agent_*.log' | Sort-Object LastWriteTime -Descending | Select-Object -First 1 | Get-Content -Tail 5"
)
echo ================================================================
echo.
pause
