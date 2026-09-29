@echo off
:: ============================================================================
:: PCAccess Agent - 1-Click Windows Service Installer (Auto-Start on Boot)
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
echo    PCAccess Agent - Windows Service Installation (Auto-Start)
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
    echo [ERROR] FileAccessAgent.exe was not found in the current directory!
    echo Please ensure you are running this script from the FileAccessAgent folder.
    pause
    exit /b 1
)

echo [1/2] Registering PCAccessAgent as a Windows Service...
"%EXE_PATH%" --install

echo.
echo [2/2] Checking Service Status...
"%EXE_PATH%" --status

echo.
echo ================================================================
echo   Installation Complete! The service will now automatically
echo   start on Windows boot and run silently in the background.
echo ================================================================
echo.
pause
