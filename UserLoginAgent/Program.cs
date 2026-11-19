using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UserLoginAgent
{
    /// <summary>
    /// UserLoginAgent Main Entry Point.
    /// WHAT: Interactive Desktop Client Agent allowing Users, Employees, and Admins to
    /// log in with Website URL, Username/Email, and Password, and automatically mount
    /// the virtual "Remote Drive (R:)" in Windows Explorer without a web browser.
    /// Provides interactive commands (Status, Logout/Stop, Refresh, Open Drive, Switch User)
    /// and CLI commands (status, stop, logout).
    /// </summary>
    internal class Program
    {
        private static UserAgentConfig _config = new UserAgentConfig();
        private static UserAgentLoginResponse _currentUser = null;
        private static bool _isRunning = true;
        private static bool _hasShutdown = false;

        static void Main(string[] args)
        {
            Console.Title = "PCAccess - User Login Agent (Remote Drive)";
            Console.OutputEncoding = Encoding.UTF8;

            System.Net.ServicePointManager.ServerCertificateValidationCallback = ValidateServerCertificate;
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls11 | System.Net.SecurityProtocolType.Tls;

            _config = UserAgentConfig.Load();

            // Handle CLI commands if arguments provided (e.g. status, stop, logout)
            if (args != null && args.Length > 0)
            {
                string cmd = args[0].Trim().ToLowerInvariant().TrimStart('-', '/');
                switch (cmd)
                {
                    case "status":
                    case "s":
                        HandleStatusCommand();
                        return;

                    case "stop":
                    case "logout":
                    case "exit":
                    case "q":
                        HandleStopLogoutCommand();
                        return;

                    case "help":
                    case "h":
                    case "?":
                        PrintCliHelp();
                        return;

                    case "reset":
                    case "r":
                        HandleResetCommand();
                        return;
                }
            }

            // Register cleanup handlers on exit
            Console.CancelKeyPress += (sender, e) =>
            {
                Shutdown();
                Environment.Exit(0);
            };

            AppDomain.CurrentDomain.ProcessExit += (sender, e) =>
            {
                Shutdown();
            };

            PrintBanner();

            RunAsync().GetAwaiter().GetResult();
        }

        #region CLI Command Handlers (status / stop / logout)

        private static void HandleStatusCommand()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
========================================================================
                 PCAccess User Login Agent - Status Check
========================================================================
");
            Console.ResetColor();

            int currentPid = Process.GetCurrentProcess().Id;
            var runningAgents = Process.GetProcessesByName("UserLoginAgent")
                                       .Where(p => p.Id != currentPid)
                                       .ToList();

            bool isProcessRunning = runningAgents.Count > 0;
            string driveLetter = string.IsNullOrWhiteSpace(_config.DriveLetter) ? "U:" : _config.DriveLetter;
            bool isDriveMounted = Directory.Exists(driveLetter.TrimEnd('\\') + @"\");

            Console.WriteLine($" Agent Process     : {(isProcessRunning ? "[RUNNING] (PID: " + string.Join(", ", runningAgents.Select(p => p.Id)) + ")" : "[STOPPED] (Not Running)")}");
            Console.WriteLine($" Remote Drive ({driveLetter}) : {(isDriveMounted ? "[MOUNTED & ACTIVE]" : "[NOT MOUNTED]")}");
            Console.WriteLine($" Configured Server : {_config.ServerUrl}");
            Console.WriteLine($" Last User Login   : {(string.IsNullOrWhiteSpace(_config.UsernameOrEmail) ? "None" : _config.UsernameOrEmail)}");
            Console.WriteLine($" Last Login Date   : {(_config.LastLoginDate == DateTime.MinValue ? "Never" : _config.LastLoginDate.ToString("yyyy-MM-dd HH:mm:ss"))}");
            Console.WriteLine($" Drive Label       : {_config.DriveLabel}");

            // Quick server ping
            bool ping = UserApiClient.PingAsync(_config.ServerUrl).GetAwaiter().GetResult();
            Console.WriteLine($" Server Reachable  : {(ping ? "[ONLINE]" : "[OFFLINE / UNREACHABLE]")}");

            Console.WriteLine("========================================================================\n");
        }

        private static void HandleStopLogoutCommand()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("\n[ACTION] Stopping UserLoginAgent and Unmounting Remote Drive...");
            Console.ResetColor();

            // 1. Unmount drive
            string drive = string.IsNullOrWhiteSpace(_config.DriveLetter) ? "U:" : _config.DriveLetter;
            ClientRemoteDriveManager.UnmountDrive(drive);

            // 2. Kill running processes if any
            int currentPid = Process.GetCurrentProcess().Id;
            var runningAgents = Process.GetProcessesByName("UserLoginAgent")
                                       .Where(p => p.Id != currentPid)
                                       .ToList();

            int stoppedCount = 0;
            foreach (var p in runningAgents)
            {
                try
                {
                    p.Kill();
                    stoppedCount++;
                }
                catch { }
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("========================================================================");
            Console.WriteLine($" [SUCCESS] Remote Drive ({drive}) unmounted cleanly.");
            if (stoppedCount > 0)
            {
                Console.WriteLine($" [SUCCESS] {stoppedCount} running UserLoginAgent process(es) terminated.");
            }
            Console.WriteLine(" [STATUS] User session logged out successfully.");
            Console.WriteLine("========================================================================\n");
            Console.ResetColor();
        }

        private static void PrintCliHelp()
        {
            Console.WriteLine(@"
PCAccess User Login Agent - Command Line Usage:
------------------------------------------------
  UserLoginAgent.exe             Start interactive desktop login agent
  UserLoginAgent.exe status      Check if agent is running and drive U: is mounted
  UserLoginAgent.exe stop        Stop running agent and unmount User Remote Drive (U:)
  UserLoginAgent.exe logout      Same as stop (unmounts drive and logs out)
  UserLoginAgent.exe reset       Cleanly resets agent state, unmounts drive, and clears credentials
  UserLoginAgent.exe help        Show this help message
");
        }

        /// <summary>
        /// WHAT: Cleanly resets UserLoginAgent state, unmounts user drive, and clears credentials.
        /// REASON: Task 1.4 - Allows user to run 'UserLoginAgent.exe reset' to terminate agent,
        /// unmount drive U:, and clear saved credentials so a fresh login prompt appears.
        /// </summary>
        private static void HandleResetCommand()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine(@"
========================================================================
             PCAccess UserLoginAgent - Clean Reset Engine
========================================================================
");
            Console.ResetColor();

            Console.WriteLine("[1/3] Stopping running UserLoginAgent processes...");
            int currentPid = Process.GetCurrentProcess().Id;
            var runningAgents = Process.GetProcessesByName("UserLoginAgent")
                                       .Where(p => p.Id != currentPid)
                                       .ToList();
            foreach (var p in runningAgents)
            {
                try { p.Kill(); } catch { }
            }

            Console.WriteLine("[2/3] Unmounting User Virtual Drive (U: & R:)...");
            try
            {
                string drive = string.IsNullOrWhiteSpace(_config.DriveLetter) ? "U:" : _config.DriveLetter;
                ClientRemoteDriveManager.UnmountDrive(drive);
                ClientRemoteDriveManager.UnmountDrive("R:");
            }
            catch { }

            Console.WriteLine("[3/3] Clearing saved credentials and staging files...");
            try
            {
                _config.UsernameOrEmail = "";
                _config.ClearSavedPassword();
                _config.DriveLetter = "U:";
                _config.DriveLabel = "User Remote Drive";
                _config.Save();

                string stagingDir = ClientRemoteDriveManager.GetStagingDirectory();
                if (Directory.Exists(stagingDir))
                {
                    Directory.Delete(stagingDir, true);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("  [WARN] Cleanup notice: " + ex.Message);
            }

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine(@"
========================================================================
 [SUCCESS] UserLoginAgent has been completely reset!
 - User Drive unmounted cleanly.
 - Saved credentials wiped.
 - Fresh login prompt will appear on next run.
========================================================================
");
            Console.ResetColor();
        }

        #endregion

        #region Interactive Session Loop

        private static async Task RunAsync()
        {
            while (_isRunning)
            {
                bool loggedIn = await PerformLoginFlowAsync();
                if (!loggedIn)
                {
                    Console.WriteLine();
                    Console.Write("Would you like to try again? (Y/n): ");
                    string ans = Console.ReadLine();
                    if (!string.IsNullOrWhiteSpace(ans) && ans.Trim().ToLower() == "n")
                    {
                        break;
                    }
                    continue;
                }

                // Main interactive session loop
                bool switchUserRequested = await RunActiveSessionLoopAsync();
                if (switchUserRequested)
                {
                    continue; // Loop back to login flow
                }

                break;
            }

            Shutdown();
        }

        private static async Task<bool> PerformLoginFlowAsync()
        {
            string serverUrl = _config.ServerUrl;
            string username = _config.UsernameOrEmail;
            string password = "";

            bool hasSaved = !string.IsNullOrWhiteSpace(username) && _config.HasSavedPassword;

            if (hasSaved)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n[SAVED SESSION DETECTED]");
                Console.ResetColor();
                Console.WriteLine($"Server  : {serverUrl}");
                Console.WriteLine($"Username: {username}");
                Console.WriteLine();
                Console.Write("Press [ENTER] to log in with saved credentials, or type 'C' to switch user: ");
                string choice = Console.ReadLine();

                if (string.IsNullOrWhiteSpace(choice) || choice.Trim().ToLower() != "c")
                {
                    password = _config.GetSavedPassword();
                    if (string.IsNullOrEmpty(password))
                    {
                        hasSaved = false;
                    }
                }
                else
                {
                    hasSaved = false;
                    _config.ClearSavedPassword();
                    _config.Save();
                }
            }

            if (!hasSaved)
            {
                Console.WriteLine();
                Console.ForegroundColor = ConsoleColor.White;
                Console.WriteLine("------------------------------------------------------------------------");
                Console.WriteLine("                  ENTER YOUR LOGIN CREDENTIALS                          ");
                Console.WriteLine("------------------------------------------------------------------------");
                Console.ResetColor();

                Console.Write($"Server URL [{_config.ServerUrl}]: ");
                string inputUrl = Console.ReadLine();
                if (!string.IsNullOrWhiteSpace(inputUrl)) serverUrl = inputUrl.Trim();

                Console.Write("Username or Email: ");
                username = Console.ReadLine()?.Trim() ?? "";

                Console.Write("Password: ");
                password = ReadMaskedPassword();
                Console.WriteLine();

                Console.Write("Remember credentials on this PC? (Y/n): ");
                string remInput = Console.ReadLine();
                _config.RememberCredentials = string.IsNullOrWhiteSpace(remInput) || remInput.Trim().ToLower() != "n";
            }

            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                UserAgentLogger.Error("Username/Email and Password cannot be empty.");
                return false;
            }

            UserAgentLogger.Info($"Connecting to server: {serverUrl}...");

            bool pingOk = await UserApiClient.PingAsync(serverUrl);
            if (!pingOk)
            {
                UserAgentLogger.Warn($"Could not ping server at {serverUrl}. Attempting login directly...");
            }

            UserAgentLogger.Info($"Authenticating as '{username}'...");
            var loginResult = await UserApiClient.LoginAsync(serverUrl, username, password);

            if (loginResult == null || loginResult.status != "success" || loginResult.data == null)
            {
                string errMsg = loginResult != null ? loginResult.message : "Unknown error during login.";
                UserAgentLogger.Error($"Login Failed: {errMsg}");
                return false;
            }

            _currentUser = loginResult.data;
            _config.ServerUrl = serverUrl;
            _config.UsernameOrEmail = username;

            if (_config.RememberCredentials)
            {
                _config.SetSavedPassword(password);
            }
            else
            {
                _config.ClearSavedPassword();
            }

            _config.LastLoginDate = DateTime.Now;
            _config.Save();

            // Display Welcome & Folders
            DisplaySuccessInfo(_currentUser);

            // Mount Remote Drive (R:)
            MountUserRemoteDrive(_currentUser);

            return true;
        }

        private static void DisplaySuccessInfo(UserAgentLoginResponse user)
        {
            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("========================================================================");
            Console.WriteLine($" [SUCCESS] Welcome, {user.name}! Logged in as: {user.role_name}");
            Console.WriteLine("========================================================================");
            Console.ResetColor();

            Console.WriteLine($"User ID  : {user.user_id}");
            Console.WriteLine($"Email    : {user.email}");
            Console.WriteLine($"Role     : {user.role_name}");
            Console.WriteLine($"Folders  : {user.folders.Count} accessible shared folder(s)");
            Console.WriteLine();

            if (user.folders.Count > 0)
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine("---------------------------------------------------------------------------------------------------");
                Console.WriteLine($"{"#",-3} | {"Folder Name",-20} | {"Host Device",-16} | {"Status",-10} | {"Permissions",-25}");
                Console.WriteLine("---------------------------------------------------------------------------------------------------");
                Console.ResetColor();

                int idx = 1;
                foreach (var f in user.folders)
                {
                    string status = f.is_device_online ? "ONLINE" : "OFFLINE";
                    string perms = $"View={(f.can_view ? "✓" : "✗")} Download={(f.can_download ? "✓" : "✗")} Upload={(f.can_upload ? "✓" : "✗")}";
                    Console.WriteLine($"{idx,-3} | {Truncate(f.folder_name, 20),-20} | {Truncate(f.device_name, 16),-16} | {status,-10} | {perms,-25}");
                    idx++;
                }
                Console.WriteLine("---------------------------------------------------------------------------------------------------");
            }
            else
            {
                UserAgentLogger.Warn("No shared folders are currently assigned or permitted for this account.");
            }
            Console.WriteLine();
        }

        private static void MountUserRemoteDrive(UserAgentLoginResponse user)
        {
            try
            {
                UserAgentLogger.Drive($"Mounting virtual drive {_config.DriveLetter} (\"{_config.DriveLabel}\")...");
                bool ok = ClientRemoteDriveManager.MountDrive(_config.DriveLetter, _config.DriveLabel, user.folders, _config.ServerUrl, user.user_id, user.session_token);

                if (ok)
                {
                    UserAgentLogger.Success($"Drive {_config.DriveLetter} successfully mounted and active in Windows Explorer!");
                }
                else
                {
                    UserAgentLogger.Warn($"Drive {_config.DriveLetter} could not be mapped directly, but folders are prepared.");
                }
            }
            catch (Exception ex)
            {
                UserAgentLogger.Error($"Drive mount error: {ex.Message}");
            }
        }

        /// <summary>
        /// Runs the active interactive loop.
        /// Returns true if user chose to switch user (re-login), false to exit.
        /// </summary>
        private static async Task<bool> RunActiveSessionLoopAsync()
        {
            PrintCommandsMenu();

            while (_isRunning)
            {
                if (Console.KeyAvailable)
                {
                    var key = Console.ReadKey(true);

                    // Q / L / X = Logout & Exit
                    if (key.Key == ConsoleKey.Q || key.Key == ConsoleKey.L || key.Key == ConsoleKey.X)
                    {
                        Console.WriteLine();
                        UserAgentLogger.Info("Logout & Exit requested by user.");
                        Shutdown();

                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.WriteLine("\n[SUCCESS] Logged out successfully. Remote Drive unmounted.");
                        Console.ResetColor();
                        Console.WriteLine("Exiting program in 2 seconds...");
                        Thread.Sleep(2000);
                        Environment.Exit(0);
                        return false;
                    }
                    // S = Status check
                    else if (key.Key == ConsoleKey.S)
                    {
                        ShowInlineStatus();
                    }
                    // P = Pull / Sync remote files
                    else if (key.Key == ConsoleKey.P)
                    {
                        Console.WriteLine();
                        await ClientRemoteDriveManager.SyncAllRemoteFilesAsync(_currentUser, _config.ServerUrl, _config.DriveLetter);
                        PrintCommandsMenu();
                    }
                    // O = Open Remote Drive in Explorer
                    else if (key.Key == ConsoleKey.O)
                    {
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "explorer.exe",
                                Arguments = _config.DriveLetter + @"\",
                                UseShellExecute = true
                            });
                            UserAgentLogger.Info($"Opened {_config.DriveLetter} in Windows Explorer.");
                        }
                        catch (Exception ex)
                        {
                            UserAgentLogger.Warn($"Could not open Explorer: {ex.Message}");
                        }
                    }
                    // R = Refresh folders from server
                    else if (key.Key == ConsoleKey.R)
                    {
                        UserAgentLogger.Info("Refreshing shared folders from server...");
                        var res = await UserApiClient.LoginAsync(_config.ServerUrl, _config.UsernameOrEmail, GetSavedPassword());
                        if (res != null && res.status == "success" && res.data != null)
                        {
                            _currentUser = res.data;
                            DisplaySuccessInfo(_currentUser);
                            MountUserRemoteDrive(_currentUser);
                        }
                        else
                        {
                            UserAgentLogger.Warn("Could not refresh folders.");
                        }
                        PrintCommandsMenu();
                    }
                    // C = Switch user / Clear saved login
                    else if (key.Key == ConsoleKey.C)
                    {
                        Console.WriteLine();
                        UserAgentLogger.Info("Switching user... Unmounting current drive.");
                        Shutdown();
                        _hasShutdown = false; // Reset so next session can unmount when needed

                        _config.ClearSavedPassword();
                        _config.UsernameOrEmail = "";
                        _config.Save();

                        Console.ForegroundColor = ConsoleColor.Yellow;
                        Console.WriteLine("[INFO] Saved login cleared. Ready for new user login.");
                        Console.ResetColor();
                        return true; // Triggers new login loop
                    }
                }

                Thread.Sleep(300);
            }

            return false;
        }

        private static void PrintCommandsMenu()
        {
            string drive = string.IsNullOrWhiteSpace(_config.DriveLetter) ? "U:" : _config.DriveLetter;
            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("\n------------------------------------------------------------------------");
            Console.WriteLine($" [STATUS] User Session Active & Remote Drive ({drive}) Mounted.");
            Console.WriteLine(" Commands:");
            Console.WriteLine("   [S] - Status: View session, drive, and connection health");
            Console.WriteLine($"   [O] - Open: Browse Remote Drive ({drive}) in Windows Explorer");
            Console.WriteLine($"   [P] - Pull / Sync: Download all remote files into {drive} for offline work");
            Console.WriteLine("   [R] - Refresh: Re-fetch shared folders from server");
            Console.WriteLine("   [C] - Switch User: Clear credentials and log in as another user");
            Console.WriteLine("   [L] or [Q] - Logout: Stop agent, unmount drive, and Exit");
            Console.WriteLine("------------------------------------------------------------------------\n");
            Console.ResetColor();
        }

        private static void ShowInlineStatus()
        {
            string drive = string.IsNullOrWhiteSpace(_config.DriveLetter) ? "U:" : _config.DriveLetter;
            bool driveActive = Directory.Exists(drive.TrimEnd('\\') + @"\");

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("------------------------------------------------------------------------");
            Console.WriteLine("                     CURRENT SESSION STATUS                             ");
            Console.WriteLine("------------------------------------------------------------------------");
            Console.ResetColor();
            Console.WriteLine($" User Name     : {_currentUser?.name ?? "Unknown"}");
            Console.WriteLine($" Email / ID    : {_currentUser?.email ?? _config.UsernameOrEmail} (ID: {_currentUser?.user_id})");
            Console.WriteLine($" Active Role   : {_currentUser?.role_name ?? "User"}");
            Console.WriteLine($" Remote Drive  : {drive} {(driveActive ? "[MOUNTED & READY]" : "[NOT MOUNTED]")}");
            Console.WriteLine($" Folders Count : {_currentUser?.folders?.Count ?? 0} accessible folder(s)");
            Console.WriteLine($" Server URL    : {_config.ServerUrl}");
            Console.WriteLine("------------------------------------------------------------------------\n");
        }

        private static string GetSavedPassword()
        {
            return _config?.GetSavedPassword() ?? "";
        }

        /// <summary>
        /// Validates SSL/TLS certificates selectively.
        /// WHAT: Allows self-signed certificates ONLY for local development (loopback/localhost).
        /// REASON: Task 2.3 - Prevents Man-in-the-Middle (MitM) attacks against production domains
        /// (e.g. remote.primaryportal.co.uk) while preserving effortless local dev testing on https://localhost:44355.
        /// </summary>
        private static bool ValidateServerCertificate(
            object sender, 
            System.Security.Cryptography.X509Certificates.X509Certificate cert, 
            System.Security.Cryptography.X509Certificates.X509Chain chain, 
            System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }

            // Allow self-signed dev certificate ONLY if connecting to localhost / loopback
            if (sender is System.Net.HttpWebRequest req && req.RequestUri != null)
            {
                if (req.RequestUri.IsLoopback ||
                    string.Equals(req.RequestUri.Host, "localhost", StringComparison.OrdinalIgnoreCase) ||
                    req.RequestUri.Host == "127.0.0.1")
                {
                    return true;
                }
            }

            if (_config != null && !string.IsNullOrWhiteSpace(_config.ServerUrl))
            {
                if (Uri.TryCreate(_config.ServerUrl, UriKind.Absolute, out Uri uri))
                {
                    if (uri.IsLoopback || string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase) || uri.Host == "127.0.0.1")
                    {
                        return true;
                    }
                }
            }

            UserAgentLogger.Warn($"[SSL SECURITY] Blocked untrusted SSL certificate for remote host: {sslPolicyErrors}");
            return false;
        }

        private static void Shutdown()
        {
            if (_hasShutdown) return;
            _hasShutdown = true;

            try
            {
                UserAgentLogger.Info("Unmounting Remote Drive...");
                ClientRemoteDriveManager.UnmountDrive(_config.DriveLetter);
            }
            catch { }
        }

        private static string ReadMaskedPassword()
        {
            StringBuilder sb = new StringBuilder();
            while (true)
            {
                ConsoleKeyInfo key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Enter) break;
                if (key.Key == ConsoleKey.Backspace)
                {
                    if (sb.Length > 0)
                    {
                        sb.Remove(sb.Length - 1, 1);
                        Console.Write("\b \b");
                    }
                }
                else if (!char.IsControl(key.KeyChar))
                {
                    sb.Append(key.KeyChar);
                    Console.Write("*");
                }
            }
            return sb.ToString();
        }

        private static string Truncate(string val, int max)
        {
            if (string.IsNullOrEmpty(val)) return "";
            return val.Length <= max ? val : val.Substring(0, max - 3) + "...";
        }

        private static void PrintBanner()
        {
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine(@"
 ========================================================================
               PCAccess - User Login Desktop Agent
        Direct Employee & Admin Remote Drive Access System
 ========================================================================");
            Console.ResetColor();
        }

        #endregion
    }
}

