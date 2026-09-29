using System;
using System.Diagnostics;
using System.IO;
using System.Security.Principal;
using System.ServiceProcess;

namespace FileAccessAgent
{
    /// <summary>
    /// Manages the Windows Service lifecycle for FileAccessAgent.
    /// WHAT: Installs, starts, stops, and removes the PCAccessAgent Windows Service.
    /// REASON: Enables seamless background auto-start on boot under LocalSystem without third-party tools.
    /// </summary>
    public static class ServiceManager
    {
        public const string ServiceName = "PCAccessAgent";
        public const string DisplayName = "PCAccess Remote File Access Agent";
        public const string Description = "Enables secure remote file browsing, streaming, upload, and download for authorized users.";

        public static bool IsAdministrator()
        {
            if (Environment.OSVersion.Platform != PlatformID.Win32NT) return false;
            try
            {
                using var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }

        public static void InstallService()
        {
            if (!IsAdministrator())
            {
                AgentLogger.Error("Administrator privileges required to install Windows Service. Please run as Administrator.");
                return;
            }

            string exePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "FileAccessAgent.exe");
            if (!File.Exists(exePath))
            {
                exePath = Process.GetCurrentProcess().MainModule?.FileName ?? exePath;
            }

            AgentLogger.Info($"[SERVICE] Installing '{ServiceName}' pointing to '{exePath}'...");

            // 1. If service already exists, stop and delete it first
            if (IsServiceInstalled())
            {
                AgentLogger.Warn($"[SERVICE] Service '{ServiceName}' already exists. Reinstalling...");
                StopServiceInternal();
                RunSc($"delete \"{ServiceName}\"");
            }

            // 2. Create service with auto-start
            string binPath = $"\\\"{exePath}\\\" --service";
            string createArgs = $"create \"{ServiceName}\" binPath= \"{binPath}\" start= delayed-auto DisplayName= \"{DisplayName}\"";
            var createResult = RunSc(createArgs);

            if (createResult.Success)
            {
                // 3. Set description
                RunSc($"description \"{ServiceName}\" \"{Description}\"");

                // 4. Configure automatic failure recovery (restart after 1 min)
                RunSc($"failure \"{ServiceName}\" reset= 86400 actions= restart/60000/restart/60000/restart/60000");

                AgentLogger.Success($"[SERVICE] '{DisplayName}' registered successfully with Automatic (Delayed Start)!");

                // 5. Start service immediately
                StartService();
            }
            else
            {
                AgentLogger.Error($"[SERVICE] Failed to register service: {createResult.Output}");
            }
        }

        public static void UninstallService()
        {
            if (!IsAdministrator())
            {
                AgentLogger.Error("Administrator privileges required to uninstall Windows Service. Please run as Administrator.");
                return;
            }

            if (!IsServiceInstalled())
            {
                AgentLogger.Warn($"[SERVICE] Service '{ServiceName}' is not installed.");
                return;
            }

            AgentLogger.Info($"[SERVICE] Stopping and removing '{ServiceName}'...");
            StopServiceInternal();
            var result = RunSc($"delete \"{ServiceName}\"");

            if (result.Success)
            {
                AgentLogger.Success($"[SERVICE] '{ServiceName}' uninstalled successfully!");
            }
            else
            {
                AgentLogger.Error($"[SERVICE] Failed to delete service: {result.Output}");
            }
        }

        public static void StartService()
        {
            if (!IsServiceInstalled())
            {
                AgentLogger.Error($"[SERVICE] Cannot start: Service '{ServiceName}' is not installed.");
                return;
            }

            AgentLogger.Info($"[SERVICE] Starting '{ServiceName}'...");
            var result = RunSc($"start \"{ServiceName}\"");
            if (result.Success)
            {
                AgentLogger.Success($"[SERVICE] '{ServiceName}' started successfully in the background!");
            }
            else
            {
                AgentLogger.Warn($"[SERVICE] Start command output: {result.Output}");
            }
        }

        public static void StopService()
        {
            if (!IsServiceInstalled())
            {
                AgentLogger.Error($"[SERVICE] Cannot stop: Service '{ServiceName}' is not installed.");
                return;
            }

            AgentLogger.Info($"[SERVICE] Stopping '{ServiceName}'...");
            var result = StopServiceInternal();
            if (result.Success)
            {
                AgentLogger.Success($"[SERVICE] '{ServiceName}' stopped successfully.");
            }
            else
            {
                AgentLogger.Warn($"[SERVICE] Stop command output: {result.Output}");
            }
        }

        public static void ShowStatus()
        {
            AgentLogger.Info($"--- PCAccess Service Status Check ---");
            if (!IsServiceInstalled())
            {
                AgentLogger.Warn($"Service '{ServiceName}': NOT INSTALLED");
                AgentLogger.Info("To install, run: FileAccessAgent.exe --install (as Administrator)");
                return;
            }

            try
            {
                using var sc = new ServiceController(ServiceName);
                AgentLogger.Success($"Service Name   : {sc.ServiceName}");
                AgentLogger.Info($"Display Name   : {sc.DisplayName}");
                AgentLogger.Info($"Current Status : {sc.Status}");
                AgentLogger.Info($"Can Stop       : {sc.CanStop}");
            }
            catch (Exception ex)
            {
                AgentLogger.Error($"Failed to query service status: {ex.Message}");
            }
        }

        public static bool IsServiceInstalled()
        {
            try
            {
                using var sc = new ServiceController(ServiceName);
                var status = sc.Status;
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static (bool Success, string Output) StopServiceInternal()
        {
            return RunSc($"stop \"{ServiceName}\"");
        }

        private static (bool Success, string Output) RunSc(string arguments)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = arguments,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(psi);
                if (process == null) return (false, "Failed to start sc.exe process");

                string stdout = process.StandardOutput.ReadToEnd();
                string stderr = process.StandardError.ReadToEnd();
                process.WaitForExit(10000);

                string output = (stdout + " " + stderr).Trim();
                bool success = process.ExitCode == 0 || output.Contains("SUCCESS");
                return (success, output);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }
    }
}
