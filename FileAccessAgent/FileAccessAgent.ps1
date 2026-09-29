# ============================================================================
# PCAccess Agent - PowerShell Wrapper for FileAccessAgent
# WHAT: Forwards all CLI arguments to the compiled FileAccessAgent.exe
# REASON: Allows running .\FileAccessAgent directly from E:\Git\PCAccess\FileAccessAgent in PowerShell
# ============================================================================
$exePath = Join-Path $PSScriptRoot "bin\Debug\FileAccessAgent.exe"
if (-not (Test-Path $exePath)) {
    $exePath = Join-Path $PSScriptRoot "bin\Release\FileAccessAgent.exe"
}

if (-not (Test-Path $exePath)) {
    Write-Error "[ERROR] FileAccessAgent.exe not found! Please build the solution first."
    exit 1
}

& $exePath $args
