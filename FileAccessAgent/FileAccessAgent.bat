@echo off
:: ============================================================================
:: PCAccess Agent - CLI Wrapper for FileAccessAgent
:: WHAT: Forwards all CLI arguments directly to the compiled FileAccessAgent.exe
:: REASON: Allows running FileAccessAgent commands directly from E:\Git\PCAccess\FileAccessAgent
:: without having to navigate into bin\Debug or bin\Release.
:: ============================================================================
setlocal

set "EXE_PATH=%~dp0bin\Debug\FileAccessAgent.exe"
if not exist "%EXE_PATH%" (
    set "EXE_PATH=%~dp0bin\Release\FileAccessAgent.exe"
)

if not exist "%EXE_PATH%" (
    echo [ERROR] FileAccessAgent.exe not found!
    echo Please build the solution in Visual Studio first.
    exit /b 1
)

"%EXE_PATH%" %*
