@echo off
setlocal
cd /d "%~dp0"
title PCAccess - Master Clean Reset All Agents

echo.
echo ========================================================================
echo                 PCAccess Master Reset Engine (All Agents)
echo ========================================================================
echo.

echo [1/4] Stopping all running Agent processes...
taskkill /f /im FileAccessAgent.exe 2>nul
taskkill /f /im UserLoginAgent.exe 2>nul
net stop PCAccessAgent 2>nul

echo [2/4] Unmounting all PCAccess Virtual Drives (R: and U:)...
subst R: /d 2>nul
subst U: /d 2>nul

echo [3/4] Resetting FileAccessAgent config and staging...
if exist "FileAccessAgent\bin\Debug\FileAccessAgent.exe" (
    "FileAccessAgent\bin\Debug\FileAccessAgent.exe" --reset
) else if exist "FileAccessAgent\FileAccessAgent.exe" (
    "FileAccessAgent\FileAccessAgent.exe" --reset
)

echo [4/4] Resetting UserLoginAgent config and staging...
if exist "UserLoginAgent\bin\Debug\UserLoginAgent.exe" (
    "UserLoginAgent\bin\Debug\UserLoginAgent.exe" reset
) else if exist "UserLoginAgent\UserLoginAgent.exe" (
    "UserLoginAgent\UserLoginAgent.exe" reset
)

echo.
echo ========================================================================
echo  [SUCCESS] All PCAccess agents, configs, and drives are cleanly reset!
echo  You are ready for fresh pairing and testing.
echo ========================================================================
echo.
pause
