@echo off
:: ============================================================================
:: PCAccess Agent - 1-Click Windows Service Uninstaller
:: ============================================================================
setlocal EnableDelayedExpansion

:: Check for Administrative Privileges and self-elevate if needed
net session >nul 2>&1
if %errorlevel% neq 0 (
    echo [ELEVATION] Requesting Administrator Privileges...
    powershell -NoProfile -ExecutionPolicy Bypass -Command "Start-Process -FilePath '%~f0' -Verb RunAs"
    exit /b
)

cd /d "%~dp0"

echo ================================================================
echo    PCAccess Agent - Windows Service Uninstallation
echo ================================================================
echo.

:: Locate FileAccessAgent.exe
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

echo [1/2] Stopping and removing PCAccessAgent Windows Service...
"%EXE_PATH%" --uninstall

echo.
echo [2/2] Verifying Service Removal...
"%EXE_PATH%" --status

echo.
echo ================================================================
echo   Uninstallation Complete!
echo ================================================================
echo.
pause
