@echo off
title PCAccess User Login Agent
echo Starting PCAccess User Login Agent...
cd /d "%~dp0"
if exist "bin\Debug\UserLoginAgent.exe" (
    start "" "bin\Debug\UserLoginAgent.exe"
) else if exist "UserLoginAgent.exe" (
    start "" "UserLoginAgent.exe"
) else (
    echo [ERROR] UserLoginAgent.exe not found! Please build the solution first.
    pause
)
