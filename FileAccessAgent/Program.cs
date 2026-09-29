using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR.Client;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace FileAccessAgent
{
    /// <summary>
    /// Entry point for the Windows PC File Access Agent.
    /// WHAT: Hybrid agent supporting both Interactive Console mode and native Windows Service mode.
    /// REASON: Enables seamless background auto-start on boot under LocalSystem, plus easy console testing.
    /// </summary>
    internal class Program
    {
        private static AgentConfig _config = new AgentConfig();
        private static HubConnection? _hubConnection;
        private static IHubProxy? _deviceHub;
        private static Timer? _heartbeatTimer;
        private static readonly HttpClient _httpClient = new HttpClient();
        private static bool _isRunning = true;

        private static int _isConnecting = 0;

        static Program()
        {
#pragma warning disable SYSLIB0014
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
#pragma warning restore SYSLIB0014
        }


        static async Task Main(string[] args)
        {
            // 1. Process CLI argument switches
            if (args.Length > 0)
            {
                string cmd = args[0].ToLowerInvariant().TrimStart('-', '/');
                switch (cmd)
                {
                    case "install":
                    case "i":
                        ServiceManager.InstallService();
                        return;

                    case "uninstall":
                    case "u":
                    case "remove":
                        ServiceManager.UninstallService();
                        return;

                    case "start":
                        ServiceManager.StartService();
                        return;

                    case "stop":
                        ServiceManager.StopService();
                        return;

                    case "status":
                    case "s":
                        ServiceManager.ShowStatus();
                        return;

                    case "service":
                        // Explicit Windows Service entry point called by Windows SCM
                        RunAsWindowsService();
                        return;

                    case "help":
                    case "?":
                        ShowHelp();
                        return;
                }
            }

            // 2. Detect if started directly by Windows Service Control Manager (Session 0)
            if (!Environment.UserInteractive)
            {
                RunAsWindowsService();
                return;
            }

            // 3. Interactive Console Mode
            Console.Title = "PCAccess File Access Agent";
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("================================================================");
            Console.WriteLine("    PCAccess - Remote File Access Agent (Hybrid Service/Console) ");
            Console.WriteLine("================================================================");
            Console.ResetColor();
            Console.WriteLine("Available commands: --install, --uninstall, --start, --stop, --status");
            Console.WriteLine();

            using var cts = new CancellationTokenSource();

            // Handle graceful shutdown in Console (Ctrl+C / window close)
            Console.CancelKeyPress += (sender, eventArgs) =>
            {
                eventArgs.Cancel = true;
                AgentLogger.Warn("[SHUTDOWN] Stopping agent and notifying server...");
                cts.Cancel();
                _isRunning = false;
                StopAgentAsync().GetAwaiter().GetResult();
                Environment.Exit(0);
            };

            await StartAgentAsync(cts.Token);
        }

        private static void ShowHelp()
        {
            Console.WriteLine("PCAccess File Access Agent - Command Line Options:");
            Console.WriteLine("  --install     Installs and starts the background Windows Service (Runs on boot)");
            Console.WriteLine("  --uninstall   Stops and removes the Windows Service");
            Console.WriteLine("  --start       Starts the installed Windows Service");
            Console.WriteLine("  --stop        Stops the running Windows Service");
            Console.WriteLine("  --status      Displays current Windows Service status");
            Console.WriteLine("  --service     Starts agent in headless Windows Service mode (Used by SCM)");
            Console.WriteLine("  (no args)     Runs interactively in console mode for testing & initial pairing");
        }

        private static void RunAsWindowsService()
        {
            AgentLogger.Info("[INIT] Starting PCAccess native ServiceBase host...");
            System.ServiceProcess.ServiceBase.Run(new AgentWindowsService());
        }

        /// <summary>
        /// WHAT: Core agent engine start method.
        /// REASON: Executed by both Console mode and Windows Service (AgentBackgroundService).
        /// </summary>
        public static async Task StartAgentAsync(CancellationToken cancellationToken)
        {
            // Bypass SSL certificate validation for localhost development and internal connections
#pragma warning disable SYSLIB0014
            System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, cert, chain, sslPolicyErrors) => true;
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12 | System.Net.SecurityProtocolType.Tls13;
#pragma warning restore SYSLIB0014
            // 1. Load or initialize configuration
            _config = AgentConfig.Load();

            // 2. Ensure device is paired
            if (!_config.IsPaired)
            {
                if (Environment.UserInteractive)
                {
                    bool paired = await PerformFirstTimePairingAsync();
                    if (!paired)
                    {
                        AgentLogger.Error("[ERROR] Pairing was not successful. Exiting.");
                        Console.WriteLine("Press any key to exit...");
                        try { Console.ReadKey(); } catch { }
                        return;
                    }
                }
                else
                {
                    AgentLogger.Error("[FATAL] Device is not paired! Please run FileAccessAgent.exe once in interactive console mode to pair with your account before starting as a service.");
                    return;
                }
            }
            else
            {
                AgentLogger.Info($"[CONFIG] Loaded existing device: {_config.DeviceName} ({_config.DeviceGuid})");
                AgentLogger.Info($"[CONFIG] Server URL: {_config.ServerUrl}");
            }

            // 3. Connect to SignalR and maintain connection
            await ConnectAndMaintainSignalRAsync();

            // 4. Keep running until cancellation requested
            _isRunning = true;
            try
            {
                while (_isRunning && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(1000, cancellationToken);
                }
            }
            catch (OperationCanceledException) { }

            await StopAgentAsync();
        }

        /// <summary>
        /// WHAT: Graceful agent shutdown method.
        /// </summary>
        public static async Task StopAgentAsync()
        {
            _isRunning = false;
            try
            {
                _heartbeatTimer?.Dispose();
                _heartbeatTimer = null;
            }
            catch { }

            try
            {
                if (_hubConnection != null)
                {
                    _hubConnection.Stop();
                    _hubConnection.Dispose();
                    _hubConnection = null;
                }
            }
            catch { }

            try
            {
                if (_config != null && _config.EnableRemoteDrive)
                {
                    RemoteDriveManager.UnmountDrive(_config.DriveLetter);
                }
            }
            catch { }

            AgentLogger.Info("[SHUTDOWN] Agent stopped cleanly.");
            await Task.CompletedTask;
        }

        /// <summary>
        /// WHAT: Interactive first-run setup flow prompting for server URL and 6-digit pairing code.
        /// REASON: Securely pairs local PC with the user's web account without asking for user passwords.
        /// </summary>
        private static async Task<bool> PerformFirstTimePairingAsync()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine();
            Console.WriteLine("[PAIRING REQUIRED] This computer has not yet been paired with your account.");
            Console.ResetColor();

            // Server URL prompt
            Console.Write($"Enter Server URL (default: {_config.ServerUrl}): ");
            string? urlInput = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(urlInput))
            {
                _config.ServerUrl = urlInput.TrimEnd('/');
            }

            // PC Name prompt
            Console.Write($"Enter Device Name (default: {_config.DeviceName}): ");
            string? nameInput = Console.ReadLine()?.Trim();
            if (!string.IsNullOrWhiteSpace(nameInput))
            {
                _config.DeviceName = nameInput;
            }

            // Pairing Code prompt
            Console.ForegroundColor = ConsoleColor.Green;
            Console.Write("Enter 6-digit Pairing Code from your Dashboard (e.g. 849-210): ");
            Console.ResetColor();
            string? pairingCode = Console.ReadLine()?.Trim();

            if (string.IsNullOrWhiteSpace(pairingCode))
            {
                Console.WriteLine("[ERROR] Pairing code cannot be empty.");
                return false;
            }

            // Generate permanent GUID for this PC
            _config.DeviceGuid = Guid.NewGuid();

            AgentLogger.Info($"[REGISTER] Registering {_config.DeviceName} ({_config.DeviceGuid}) with {_config.ServerUrl}...");

            try
            {
                var payload = new
                {
                    pairing_code = pairingCode,
                    device_guid = _config.DeviceGuid,
                    device_name = _config.DeviceName
                };

                string jsonPayload = JsonConvert.SerializeObject(payload);
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

                string registerEndpoint = _config.ServerUrl.TrimEnd('/') + "/DeviceAPI/Register";
                var response = await _httpClient.PostAsync(registerEndpoint, content);
                string responseBody = await response.Content.ReadAsStringAsync();

                var res = JObject.Parse(responseBody);
                string? status = res["status"]?.ToString();
                string? message = res["message"]?.ToString();

                if (status == "success" && res["data"] != null)
                {
                    _config.DeviceToken = res["data"]?["device_token"]?.ToString() ?? "";
                    _config.Save();

                    AgentLogger.Success("[SUCCESS] Device successfully paired and configuration saved to agent_config.json!");
                    return true;
                }
                else
                {
                    AgentLogger.Error($"[ERROR] Pairing failed: {message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                AgentLogger.Error($"[ERROR] Network error connecting to server: {ex.Message}");
                return false;
            }
        }

        private static async Task ConnectAndMaintainSignalRAsync()
        {
            string serverUrl = _config.ServerUrl.TrimEnd('/');
            AgentLogger.Info();
            AgentLogger.Info($"[SIGNALR] Connecting to {serverUrl}/signalr...");

            _hubConnection = new HubConnection(serverUrl);
            _deviceHub = _hubConnection.CreateHubProxy("DeviceHub");

            // REASON: Step 3 - Listen for folder path validation commands from server/dashboard
            _deviceHub.On<string, string>("validateFolder", async (requestId, folderPath) =>
            {
                await HandleFolderValidationAsync(requestId, folderPath);
            });

            // REASON: Step 4 - Listen for directory listing requests from server/dashboard
            _deviceHub.On<string, string, string>("readDirectory", async (requestId, rootPath, relativePath) =>
            {
                await HandleReadDirectoryAsync(requestId, rootPath, relativePath);
            });


            // WHAT: Step 5 - Listen for file download requests from server/dashboard
            // REASON: Agent reads the file in 64KB chunks and sends back Base64-encoded segments
            _deviceHub.On<string, string, string>("readFile", async (requestId, rootPath, relativePath) =>
            {
                await HandleReadFileAsync(requestId, rootPath, relativePath);
            });

            // WHAT: Step 6 - Listen for file upload requests from server/dashboard
            // REASON: Agent streams file from server HTTP endpoint, writes to a temporary staging file,
            // and atomically moves it to the target shared directory.
            _deviceHub.On<string, string, string, string, string, string, long>("receiveUpload", async (requestId, rootPath, relativePath, fileName, conflictAction, uploadToken, fileSizeBytes) =>
            {
                await HandleReceiveUploadAsync(requestId, rootPath, relativePath, fileName, conflictAction, uploadToken, fileSizeBytes);
            });

            // WHAT: Listen for shared folder synchronization signals from server/dashboard
            // REASON: Dynamically updates directory junctions in "Remote Drive (R:)" when folders change on web portal
            _deviceHub.On("syncSharedFolders", async () =>
            {
                await RefreshRemoteDriveAsync();
            });

            // Connection lifecycle hooks
            _hubConnection.Closed += async () =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                AgentLogger.Info($"[DISCONNECTED] SignalR connection closed at {DateTime.Now:HH:mm:ss}. Reconnecting in 5s...");
                Console.ResetColor();

                _heartbeatTimer?.Change(Timeout.Infinite, Timeout.Infinite);

                await Task.Delay(5000);
                await TryStartSignalRAsync();
            };

            _hubConnection.Reconnecting += () =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                AgentLogger.Warn("[RECONNECTING] Connection interrupted. Re-establishing link...");
                Console.ResetColor();
            };

            _hubConnection.Reconnected += async () =>
            {
                Console.ForegroundColor = ConsoleColor.Green;
                AgentLogger.Info($"[RECONNECTED] SignalR re-established at {DateTime.Now:HH:mm:ss}.");
                Console.ResetColor();

                await RegisterDeviceWithHubAsync();
            };

            await TryStartSignalRAsync();
        }

        private static async Task TryStartSignalRAsync()
        {
            if (Interlocked.CompareExchange(ref _isConnecting, 1, 0) != 0)
                return;

            try
            {
                while (_isRunning)
                {
                    try
                    {
                        AgentLogger.Info("[CONNECTING] Connecting to SignalR server...");
                        await _hubConnection!.Start();

                        Console.ForegroundColor = ConsoleColor.Green;
                        AgentLogger.Info($"[CONNECTED] SignalR connection established! ConnectionId: {_hubConnection.ConnectionId}");
                        Console.ResetColor();

                        // Register device with server hub
                        await RegisterDeviceWithHubAsync();

                        // Start 30-second heartbeat timer
                        _heartbeatTimer = new Timer(SendHeartbeatCallback, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(30));
                        break;
                    }
                    catch (Exception ex)
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        AgentLogger.Info($"[FAILED] Connection attempt failed: {ex.Message}. Retrying in 10 seconds...");
                        Console.ResetColor();
                        await Task.Delay(10000);
                    }
                }
            }
            finally
            {
                Interlocked.Exchange(ref _isConnecting, 0);
            }
        }

        private static async Task RegisterDeviceWithHubAsync()
        {
            try
            {
                if (_deviceHub != null)
                {
                    bool registered = await _deviceHub.Invoke<bool>("RegisterAgent", _config.DeviceGuid.ToString(), _config.DeviceToken);
                    if (registered)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        AgentLogger.Info($"[STATUS: ONLINE] Server marked {_config.DeviceName} as ONLINE. Dashboard updated!");
                        Console.ResetColor();

                        // Automatically mount local Remote Drive (R:) with shared folders
                        if (_config.EnableRemoteDrive)
                        {
                            await RefreshRemoteDriveAsync();
                        }
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                        AgentLogger.Error("[ERROR] Server rejected agent credentials. Check agent_config.json.");
                        Console.ResetColor();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                AgentLogger.Info($"[ERROR] RegisterAgent call failed: {ex.Message}");
                Console.ResetColor();
            }
        }

        /// <summary>
        /// WHAT: Fetches registered shared folders from DeviceHub and mounts the local "Remote Drive (R:)".
        /// REASON: Provides seamless Explorer access to all shared folders under "This PC".
        /// </summary>
        private static async Task RefreshRemoteDriveAsync()
        {
            try
            {
                if (_deviceHub != null && _config.EnableRemoteDrive)
                {
                    AgentLogger.Info($"[REMOTE DRIVE] Requesting shared folders from server for {_config.DeviceName}...");
                    var folders = await _deviceHub.Invoke<List<AgentSharedFolderDto>>("GetDeviceSharedFolders", _config.DeviceGuid.ToString(), _config.DeviceToken);
                    folders = folders ?? new List<AgentSharedFolderDto>();

                    RemoteDriveManager.MountDrive(_config.DriveLetter, _config.DriveLabel, folders);
                }
            }
            catch (Exception ex)
            {
                AgentLogger.Warn($"[REMOTE DRIVE] Could not refresh drive: {ex.Message}");
            }
        }

        /// <summary>
        /// WHAT: 30-second heartbeat callback invoking SendHeartbeat on DeviceHub.
        /// REASON: Keeps connection fresh in SQL Server and proves agent is alive.
        /// </summary>
        private static void SendHeartbeatCallback(object? state)
        {
            if (_hubConnection != null && _hubConnection.State == ConnectionState.Connected && _deviceHub != null)
            {
                try
                {
                    _deviceHub.Invoke<bool>("SendHeartbeat", _config.DeviceGuid.ToString())
                        .ContinueWith(t =>
                        {
                            if (!t.IsFaulted)
                            {
                                AgentLogger.Info($"[HEARTBEAT] Ping sent at {DateTime.Now:HH:mm:ss} -> OK");
                            }
                            else
                            {
                                Console.ForegroundColor = ConsoleColor.Yellow;
                                AgentLogger.Info($"[HEARTBEAT] Ping failed: {t.Exception?.GetBaseException().Message}");
                                Console.ResetColor();
                            }
                        });
                }
                catch (Exception ex)
                {
                    AgentLogger.Info($"[HEARTBEAT] Error sending ping: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// WHAT: Validates whether a local directory exists and is readable on this PC.
        /// REASON: Step 3 Folder Validation - Browser never decides validity; Agent checks actual disk.
        /// </summary>
        private static async Task HandleFolderValidationAsync(string requestId, string folderPath)
        {
            AgentLogger.Info();
            Console.ForegroundColor = ConsoleColor.Yellow;
            AgentLogger.Info($"[VALIDATE] Received folder validation request #{requestId} for: \"{folderPath}\"");
            Console.ResetColor();

            bool isValid = false;
            string message = "";
            string normalizedPath = "";

            try
            {
                if (string.IsNullOrWhiteSpace(folderPath))
                {
                    isValid = false;
                    message = "Folder path cannot be empty.";
                }
                else
                {
                    string trimmed = folderPath.Trim();

                    // Security: Reject traversal attempts (e.g. "..")
                    if (trimmed.Contains(".."))
                    {
                        isValid = false;
                        message = "Path traversal sequences ('..') are not allowed for security reasons.";
                    }
                    else if (trimmed.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                    {
                        isValid = false;
                        message = "The path contains invalid characters.";
                    }
                    else
                    {
                        var dirInfo = new DirectoryInfo(trimmed);
                        normalizedPath = dirInfo.FullName;

                        if (!dirInfo.Exists)
                        {
                            isValid = false;
                            message = $"Folder does not exist on this PC: {normalizedPath}";
                        }
                        else
                        {
                            // Verify folder read access by checking directory attributes
                            try
                            {
                                var testEnum = dirInfo.EnumerateFileSystemInfos();
                                var enumerator = testEnum.GetEnumerator();
                                enumerator.MoveNext();
                                enumerator.Dispose();

                                isValid = true;
                                message = $"Folder verified and accessible: \"{dirInfo.Name}\"";
                            }
                            catch (UnauthorizedAccessException)
                            {
                                isValid = false;
                                message = $"Access Denied: The Agent process does not have read permissions for: {normalizedPath}";
                            }
                            catch (Exception ex)
                            {
                                isValid = false;
                                message = $"Cannot access folder: {ex.Message}";
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                isValid = false;
                message = "Invalid folder path format: " + ex.Message;
            }

            if (isValid)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                AgentLogger.Info($"[VALIDATE: SUCCESS] {message}");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                AgentLogger.Info($"[VALIDATE: FAILED] {message}");
                Console.ResetColor();
            }

            // Return validation result back to DeviceHub
            if (_deviceHub != null)
            {
                try
                {
                    await _deviceHub.Invoke("FolderValidationResult", requestId, isValid, message, normalizedPath);
                }
                catch (Exception ex)
                {
                    AgentLogger.Error("[ERROR] Failed to send validation result to hub: " + ex.Message);
                }
            }
        }
/// <summary>
        /// WHAT: Reads directory contents (subfolders and files) within a configured shared folder root.
        /// REASON: Step 4 Remote File Browser - Strictly enforces root boundary; rejects any '..' traversal.
        /// Excludes hidden/system files and caps response at 500 items for optimal performance.
        /// </summary>
        private static async Task HandleReadDirectoryAsync(string requestId, string rootPath, string relativePath)
        {
            AgentLogger.Info();
            Console.ForegroundColor = ConsoleColor.Cyan;
            AgentLogger.Info($"[DIRECTORY: REQUEST] #{requestId} | Root: \"{rootPath}\" | Relative: \"{relativePath}\"");
            Console.ResetColor();

            bool isSuccess = false;
            string errorMessage = "";
            string safeRelPath = (relativePath ?? "").Trim().TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
            var items = new List<FileItemDto>();

            try
            {
                if (string.IsNullOrWhiteSpace(rootPath))
                {
                    isSuccess = false;
                    errorMessage = "Shared folder root path is empty or invalid.";
                }
                else if (safeRelPath.Contains(".."))
                {
                    isSuccess = false;
                    errorMessage = "Access Denied: Path traversal sequences ('..') are strictly prohibited.";
                }
                else if (safeRelPath.IndexOfAny(Path.GetInvalidPathChars()) >= 0)
                {
                    isSuccess = false;
                    errorMessage = "The requested path contains invalid characters.";
                }
                else
                {
                    string normalizedRoot = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                    string targetPath = string.IsNullOrWhiteSpace(safeRelPath)
                        ? normalizedRoot
                        : Path.GetFullPath(Path.Combine(normalizedRoot, safeRelPath));

                    // SECURITY CHECK: Verify targetPath is within normalizedRoot
                    if (!targetPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase) &&
                        !targetPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                    {
                        isSuccess = false;
                        errorMessage = "Access Denied: Target path escapes the configured shared folder boundary.";
                    }
                    else
                    {
                        var dirInfo = new DirectoryInfo(targetPath);
                        if (!dirInfo.Exists)
                        {
                            isSuccess = false;
                            errorMessage = $"Directory does not exist on host PC: {dirInfo.Name}";
                        }
                        else
                        {
                            int totalCount = 0;
                            const int maxItems = 500;

                            // 1. Enumerate Subdirectories
                            try
                            {
                                foreach (var subDir in dirInfo.EnumerateDirectories())
                                {
                                    if (totalCount >= maxItems) break;

                                    // Filter out hidden and system directories
                                    if ((subDir.Attributes & FileAttributes.Hidden) != 0 ||
                                        (subDir.Attributes & FileAttributes.System) != 0)
                                    {
                                        continue;
                                    }

                                    string itemRel = string.IsNullOrWhiteSpace(safeRelPath)
                                        ? subDir.Name
                                        : Path.Combine(safeRelPath, subDir.Name).Replace('\\', '/');

                                    items.Add(new FileItemDto
                                    {
                                        name = subDir.Name,
                                        type = "Folder",
                                        is_folder = true,
                                        size_bytes = 0,
                                        size_formatted = "-",
                                        extension = "",
                                        last_modified = subDir.LastWriteTime,
                                        last_modified_formatted = subDir.LastWriteTime.ToString("dd-MMM-yyyy hh:mm tt"),
                                        relative_path = itemRel,
                                        icon_type = "folder"
                                    });

                                    totalCount++;
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                // If subdirectory lacks access, continue
                            }

                            // 2. Enumerate Files
                            try
                            {
                                foreach (var file in dirInfo.EnumerateFiles())
                                {
                                    if (totalCount >= maxItems) break;

                                    // Filter out hidden and system files
                                    if ((file.Attributes & FileAttributes.Hidden) != 0 ||
                                        (file.Attributes & FileAttributes.System) != 0)
                                    {
                                        continue;
                                    }

                                    string itemRel = string.IsNullOrWhiteSpace(safeRelPath)
                                        ? file.Name
                                        : Path.Combine(safeRelPath, file.Name).Replace('\\', '/');

                                    string ext = file.Extension.ToLowerInvariant();
                                    string iconType = ResolveFileIconType(ext);

                                    items.Add(new FileItemDto
                                    {
                                        name = file.Name,
                                        type = ResolveFileTypeDescription(ext),
                                        is_folder = false,
                                        size_bytes = file.Length,
                                        size_formatted = FormatFileSize(file.Length),
                                        extension = ext,
                                        last_modified = file.LastWriteTime,
                                        last_modified_formatted = file.LastWriteTime.ToString("dd-MMM-yyyy hh:mm tt"),
                                        relative_path = itemRel,
                                        icon_type = iconType
                                    });

                                    totalCount++;
                                }
                            }
                            catch (UnauthorizedAccessException)
                            {
                                // If file lacks access, continue
                            }

                            // 3. Sort: Folders alphabetically first, then files alphabetically
                            items = items
                                .OrderByDescending(i => i.is_folder)
                                .ThenBy(i => i.name, StringComparer.OrdinalIgnoreCase)
                                .ToList();

                            isSuccess = true;
                            errorMessage = "";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMessage = "Failed to read directory: " + ex.Message;
            }

            if (isSuccess)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                AgentLogger.Info($"[DIRECTORY: SUCCESS] Read {items.Count(i => i.is_folder)} folders and {items.Count(i => !i.is_folder)} files.");
                Console.ResetColor();
            }
            else
            {
                Console.ForegroundColor = ConsoleColor.Red;
                AgentLogger.Info($"[DIRECTORY: FAILED] {errorMessage}");
                Console.ResetColor();
            }

            // Return directory listing back to DeviceHub
            if (_deviceHub != null)
            {
                try
                {
                    string returnRelPath = safeRelPath.Replace('\\', '/');
                    await _deviceHub.Invoke("DirectoryContentsResult", requestId, isSuccess, errorMessage, returnRelPath, items);
                }
                catch (Exception ex)
                {
                    AgentLogger.Error("[ERROR] Failed to send directory contents result to hub: " + ex.Message);
                }
            }
        }


        /// <summary>
        /// WHAT: Reads a requested file in 64KB chunks and sends each chunk to the server via SignalR.
        /// REASON: Step 5 Secure File Download — chunked transfer avoids memory exhaustion for large files.
        /// Security: Enforces path traversal prevention and root boundary check identical to Step 4.
        /// </summary>
        private static async Task HandleReadFileAsync(string requestId, string rootPath, string relativePath)
        {
            const long MaxFileSizeBytes = 500L * 1024 * 1024; // 500 MB limit
            const int ChunkSize = 32 * 1024;                  // 64 KB per chunk

            bool isSuccess = false;
            string errorMessage = "";
            string fileName = "";
            string contentType = "application/octet-stream";
            long fileSizeBytes = 0;

            Console.ForegroundColor = ConsoleColor.Cyan;
            AgentLogger.Info($"[FILE:REQUEST] RequestId={requestId} | RelPath={relativePath}");
            Console.ResetColor();

            try
            {
                // 1. Reject path traversal attempts
                if (string.IsNullOrWhiteSpace(relativePath) || relativePath.Contains(".."))
                {
                    errorMessage = "Access Denied: Path traversal sequences ('..') are prohibited.";
                }
                else
                {
                    // 2. Resolve full path and enforce root boundary
                    string normalizedRoot = Path.GetFullPath(rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    string safeRelPath = relativePath.Trim().TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
                    string fullPath = Path.GetFullPath(Path.Combine(normalizedRoot, safeRelPath));

                    if (!fullPath.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                        && !fullPath.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        errorMessage = "Access Denied: Requested path is outside the configured root directory.";
                    }
                    else if (!File.Exists(fullPath))
                    {
                        errorMessage = $"File not found: '{Path.GetFileName(fullPath)}'";
                    }
                    else
                    {
                        var fileInfo = new FileInfo(fullPath);

                        // 3. Skip hidden/system files
                        if ((fileInfo.Attributes & FileAttributes.Hidden) != 0 ||
                            (fileInfo.Attributes & FileAttributes.System) != 0)
                        {
                            errorMessage = "Access Denied: Cannot download system or hidden files.";
                        }
                        // 4. Enforce file size limit
                        else if (fileInfo.Length > MaxFileSizeBytes)
                        {
                            errorMessage = $"File too large: '{fileInfo.Name}' is {(fileInfo.Length / (1024.0 * 1024.0)):F1} MB. Maximum allowed is 500 MB.";
                        }
                        else
                        {
                            fileName = fileInfo.Name;
                            fileSizeBytes = fileInfo.Length;
                            contentType = ResolveContentType(fileInfo.Extension.ToLowerInvariant());

                            // 5. Read file in 64KB chunks and send each via SignalR
                            using (var fs = new FileStream(fullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                            {
                                byte[] buffer = new byte[ChunkSize];
                                int totalChunks = (int)Math.Ceiling((double)fileSizeBytes / ChunkSize);
                                int chunkIndex = 0;
                                int bytesRead;

                                while ((bytesRead = await fs.ReadAsync(buffer, 0, ChunkSize)) > 0)
                                {
                                    string base64Chunk = Convert.ToBase64String(buffer, 0, bytesRead);

                                    if (_deviceHub != null)
                                    {
                                        await _deviceHub.Invoke("FileChunkResult", requestId, chunkIndex, totalChunks, base64Chunk);
                                    }

                                    chunkIndex++;
                                }
                            }

                            isSuccess = true;
                            Console.ForegroundColor = ConsoleColor.Green;
                            AgentLogger.Info($"[FILE:SUCCESS] Sent '{fileName}' ({fileSizeBytes / 1024.0:F1} KB) in chunks.");
                            Console.ResetColor();
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                errorMessage = "Access Denied: Insufficient permissions to read this file.";
            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMessage = "Failed to read file: " + ex.Message;
                Console.ForegroundColor = ConsoleColor.Red;
                AgentLogger.Info($"[FILE:ERROR] {errorMessage}");
                Console.ResetColor();
            }

            // 6. Send completion signal back to DeviceHub
            if (_deviceHub != null)
            {
                try
                {
                    await _deviceHub.Invoke("FileTransferComplete", requestId, isSuccess, errorMessage, fileName, contentType, fileSizeBytes);
                }
                catch (Exception ex)
                {
                    AgentLogger.Error("[ERROR] Failed to send FileTransferComplete: " + ex.Message);
                }
            }
        }


        /// <summary>
        /// WHAT: Handles a file upload streamed from the web server into a local shared folder.
        /// REASON: Step 6 Secure Remote File Upload - verifies path security, target directory existence,
        /// write permissions, duplicate conflict resolution (autorename vs replace), writes to a temporary
        /// staging file (.uploading_{requestId}), streams from the server via HTTP, and atomically renames.
        /// </summary>
        private static async Task HandleReceiveUploadAsync(
            string requestId,
            string rootPath,
            string relativePath,
            string fileName,
            string conflictAction,
            string uploadToken,
            long fileSizeBytes)
        {
            bool isSuccess = false;
            string errorMessage = "";
            string finalFileName = fileName;
            long finalSizeBytes = 0;
            string tempStagingPath = null;

            Console.ForegroundColor = ConsoleColor.Cyan;
            AgentLogger.Info($"[UPLOAD:REQUEST] RequestId={requestId} | File={fileName} ({FormatFileSize(fileSizeBytes)}) | RelPath={relativePath}");
            Console.ResetColor();

            try
            {
                // 1. Validate relative path & path traversal sequences
                if ((relativePath ?? "").Contains("..") || (fileName ?? "").Contains(".."))
                {
                    errorMessage = "Access Denied: Path traversal sequences ('..') are prohibited.";
                }
                else
                {
                    // 2. Resolve target directory & enforce root boundary
                    string normalizedRoot = Path.GetFullPath(rootPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
                    string safeRelPath = (relativePath ?? "").Trim().TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
                    string targetDirectory = string.IsNullOrWhiteSpace(safeRelPath) 
                        ? normalizedRoot 
                        : Path.GetFullPath(Path.Combine(normalizedRoot, safeRelPath));

                    if (!targetDirectory.StartsWith(normalizedRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                        && !targetDirectory.Equals(normalizedRoot, StringComparison.OrdinalIgnoreCase))
                    {
                        errorMessage = "Access Denied: Target directory is outside the configured shared root folder.";
                    }
                    else if (!Directory.Exists(targetDirectory))
                    {
                        errorMessage = "Target folder does not exist.";
                    }
                    else
                    {
                        // 3. Verify write permissions in target directory
                        string probeTestFile = Path.Combine(targetDirectory, $".probe_write_{Guid.NewGuid():N}.tmp");
                        bool hasWriteAccess = false;
                        try
                        {
                            using (var probeFs = File.Create(probeTestFile)) { }
                            File.Delete(probeTestFile);
                            hasWriteAccess = true;
                        }
                        catch (Exception)
                        {
                            hasWriteAccess = false;
                        }

                        if (!hasWriteAccess)
                        {
                            errorMessage = "Unable to write to the selected folder: Insufficient Windows filesystem permissions.";
                        }
                        else
                        {
                            // 4. Sanitize file name and handle duplicate conflict resolution
                            string sanitizedName = Path.GetFileName(fileName);
                            foreach (char c in Path.GetInvalidFileNameChars())
                            {
                                sanitizedName = sanitizedName.Replace(c, '_');
                            }
                            if (string.IsNullOrWhiteSpace(sanitizedName))
                            {
                                sanitizedName = "uploaded_file_" + DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            }

                            string nameWithoutExt = Path.GetFileNameWithoutExtension(sanitizedName);
                            string extension = Path.GetExtension(sanitizedName);
                            string candidateName = sanitizedName;
                            string targetFilePath = Path.Combine(targetDirectory, candidateName);

                            if (File.Exists(targetFilePath))
                            {
                                if (string.Equals(conflictAction, "replace", StringComparison.OrdinalIgnoreCase))
                                {
                                    candidateName = sanitizedName;
                                    targetFilePath = Path.Combine(targetDirectory, candidateName);
                                }
                                else
                                {
                                    // Default: Auto-rename (Keep Both) -> "Report (1).pdf", "Report (2).pdf"
                                    int counter = 1;
                                    while (File.Exists(targetFilePath))
                                    {
                                        candidateName = $"{nameWithoutExt} ({counter}){extension}";
                                        targetFilePath = Path.Combine(targetDirectory, candidateName);
                                        counter++;
                                    }
                                }
                            }

                            finalFileName = candidateName;

                            // 5. Create temporary staging file (e.g. "Report.pdf.uploading_UPL_12345")
                            tempStagingPath = Path.Combine(targetDirectory, $"{finalFileName}.uploading_{requestId}");

                            // 6. Connect to Server HTTP upload stream endpoint
                            string serverUrl = _config.ServerUrl.TrimEnd('/');
                            string streamEndpoint = $"{serverUrl}/FolderAPI/GetUploadStream?uploadToken={Uri.EscapeDataString(uploadToken)}&deviceGuid={Uri.EscapeDataString(_config.DeviceGuid.ToString())}";

                            using (var response = await _httpClient.GetAsync(streamEndpoint, HttpCompletionOption.ResponseHeadersRead))
                            {
                                if (!response.IsSuccessStatusCode)
                                {
                                    errorMessage = $"Server streaming failed with HTTP status {(int)response.StatusCode}: {response.ReasonPhrase}";
                                }
                                else
                                {
                                    using (var httpStream = await response.Content.ReadAsStreamAsync())
                                    using (var fileStream = new FileStream(tempStagingPath, FileMode.Create, FileAccess.Write, FileShare.None, 64 * 1024, true))
                                    {
                                        byte[] buffer = new byte[64 * 1024];
                                        int bytesRead;
                                        while ((bytesRead = await httpStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
                                        {
                                            await fileStream.WriteAsync(buffer, 0, bytesRead);
                                            finalSizeBytes += bytesRead;
                                        }
                                        await fileStream.FlushAsync();
                                    }

                                    // 7. Atomic finalize: rename/replace staging file to final destination
                                    if (File.Exists(targetFilePath))
                                    {
                                        File.Delete(targetFilePath);
                                    }
                                    File.Move(tempStagingPath, targetFilePath);
                                    tempStagingPath = null; // Cleared so finally block doesn't delete it

                                    isSuccess = true;
                                    Console.ForegroundColor = ConsoleColor.Green;
                                    AgentLogger.Info($"[UPLOAD:SUCCESS] Saved '{finalFileName}' ({FormatFileSize(finalSizeBytes)}) to '{targetDirectory}'");
                                    Console.ResetColor();
                                }
                            }
                        }
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                errorMessage = "Unable to write to the selected folder: Access denied.";
            }
            catch (Exception ex)
            {
                isSuccess = false;
                errorMessage = "Failed to write uploaded file: " + ex.Message;
                Console.ForegroundColor = ConsoleColor.Red;
                AgentLogger.Info($"[UPLOAD:ERROR] {errorMessage}");
                Console.ResetColor();
            }
            finally
            {
                // Ensure temporary staging file is cleaned up on any failure/cancellation
                if (!string.IsNullOrEmpty(tempStagingPath) && File.Exists(tempStagingPath))
                {
                    try
                    {
                        File.Delete(tempStagingPath);
                        AgentLogger.Info($"[UPLOAD:CLEANUP] Deleted partial file: {Path.GetFileName(tempStagingPath)}");
                    }
                    catch (Exception cleanEx)
                    {
                        AgentLogger.Info($"[UPLOAD:CLEANUP_WARN] Failed to delete temp file: {cleanEx.Message}");
                    }
                }
            }

            // 8. Send completion signal back to DeviceHub
            if (_deviceHub != null)
            {
                try
                {
                    await _deviceHub.Invoke("UploadTransferComplete", requestId, isSuccess, errorMessage, finalFileName, finalSizeBytes);
                }
                catch (Exception ex)
                {
                    AgentLogger.Error("[ERROR] Failed to send UploadTransferComplete: " + ex.Message);
                }
            }
        }

        private static string ResolveContentType(string ext)
        {
            switch (ext)
            {
                case ".pdf": return "application/pdf";
                case ".doc": return "application/msword";
                case ".docx": return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xls": return "application/vnd.ms-excel";
                case ".xlsx": return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".ppt": return "application/vnd.ms-powerpoint";
                case ".pptx": return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                case ".csv": return "text/csv";
                case ".txt": return "text/plain";
                case ".log": return "text/plain";
                case ".md": return "text/markdown";
                case ".json": return "application/json";
                case ".xml": return "application/xml";
                case ".html": return "text/html";
                case ".css": return "text/css";
                case ".js": return "application/javascript";
                case ".png": return "image/png";
                case ".jpg": case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".webp": return "image/webp";
                case ".svg": return "image/svg+xml";
                case ".bmp": return "image/bmp";
                case ".mp4": return "video/mp4";
                case ".avi": return "video/avi";
                case ".mkv": return "video/x-matroska";
                case ".mov": return "video/quicktime";
                case ".mp3": return "audio/mpeg";
                case ".wav": return "audio/wav";
                case ".ogg": return "audio/ogg";
                case ".zip": return "application/zip";
                case ".rar": return "application/x-rar-compressed";
                case ".7z": return "application/x-7z-compressed";
                case ".tar": return "application/x-tar";
                case ".gz": return "application/gzip";
                case ".cs": return "text/plain";
                case ".sql": return "text/plain";
                case ".py": return "text/plain";
                default: return "application/octet-stream";
            }
        }

        private static string ResolveFileIconType(string ext)
        {
            switch (ext)
            {
                case ".pdf": return "pdf";
                case ".doc": case ".docx": case ".rtf": return "word";
                case ".xls": case ".xlsx": case ".csv": return "excel";
                case ".ppt": case ".pptx": return "powerpoint";
                case ".png": case ".jpg": case ".jpeg": case ".gif": case ".webp": case ".bmp": case ".svg": return "image";
                case ".mp4": case ".avi": case ".mkv": case ".mov": case ".wmv": return "video";
                case ".mp3": case ".wav": case ".ogg": case ".flac": case ".m4a": return "audio";
                case ".zip": case ".rar": case ".7z": case ".tar": case ".gz": return "archive";
                case ".cs": case ".js": case ".html": case ".css": case ".json": case ".xml": case ".sql": case ".py": case ".ts": return "code";
                case ".txt": case ".log": case ".md": return "text";
                default: return "file";
            }
        }

        private static string ResolveFileTypeDescription(string ext)
        {
            switch (ext)
            {
                case ".pdf": return "PDF Document";
                case ".doc": case ".docx": return "Word Document";
                case ".xls": case ".xlsx": return "Excel Spreadsheet";
                case ".csv": return "CSV File";
                case ".ppt": case ".pptx": return "PowerPoint Presentation";
                case ".png": case ".jpg": case ".jpeg": case ".gif": case ".webp": case ".bmp": case ".svg": return "Image File";
                case ".mp4": case ".avi": case ".mkv": case ".mov": return "Video File";
                case ".mp3": case ".wav": case ".ogg": case ".flac": return "Audio File";
                case ".zip": case ".rar": case ".7z": return "Compressed Archive";
                case ".cs": return "C# Source File";
                case ".js": return "JavaScript File";
                case ".html": return "HTML Document";
                case ".css": return "CSS Stylesheet";
                case ".json": return "JSON File";
                case ".xml": return "XML File";
                case ".sql": return "SQL Script";
                case ".txt": return "Text Document";
                case ".log": return "Log File";
                case ".md": return "Markdown Document";
                default: return string.IsNullOrWhiteSpace(ext) ? "File" : ext.TrimStart('.').ToUpperInvariant() + " File";
            }
        }

        private static string FormatFileSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024.0).ToString("F1") + " KB";
            if (bytes < 1024 * 1024 * 1024) return (bytes / (1024.0 * 1024.0)).ToString("F1") + " MB";
            return (bytes / (1024.0 * 1024.0 * 1024.0)).ToString("F2") + " GB";
        }
    }
}

