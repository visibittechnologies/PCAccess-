@echo off
setlocal
set "EXE_PATH=%~dp0bin\Debug\FileAccessAgent.exe"
if not exist "%EXE_PATH%" (
    set "EXE_PATH=%~dp0bin\Release\FileAccessAgent.exe"
)
if not exist "%EXE_PATH%" (
    echo [ERROR] FileAccessAgent.exe not found! Please build the solution first.
    exit /b 1
)
"%EXE_PATH%" %*
