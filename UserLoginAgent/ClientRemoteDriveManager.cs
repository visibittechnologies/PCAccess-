using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace UserLoginAgent
{
    /// <summary>
    /// Manages the virtual/mapped Windows Drive (e.g. R: "Remote Drive") under "This PC" for UserLoginAgent.
    /// WHAT: Automatically mounts a virtual drive letter containing all permitted shared folders
    /// for the authenticated user/employee.
    /// REASON: Fulfills requirement: when user logs in via agent, a virtual drive appears in Windows Explorer
    /// showing all permitted shared folders without opening a web browser.
    /// </summary>
    public static class ClientRemoteDriveManager
    {
        #region Win32 Native APIs

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DefineDosDevice(int dwFlags, string lpDeviceName, string lpTargetPath);

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, uint uFlags, IntPtr dwItem1, IntPtr dwItem2);

        private const int DDD_RAW_TARGET_PATH = 0x00000001;
        private const int DDD_REMOVE_DEFINITION = 0x00000002;
        private const int DDD_EXACT_MATCH_ON_REMOVE = 0x00000004;

        private const int SHCNE_DRIVEADD = 0x00000100;
        private const int SHCNE_DRIVEREMOVED = 0x00000080;
        private const int SHCNE_ASSOCCHANGED = 0x08000000;
        private const uint SHCNF_PATH = 0x0005;
        private const uint SHCNF_FLUSH = 0x1000;

        #endregion

        private static readonly object _syncLock = new object();
        private static bool _isMounted = false;
        private static string _currentDriveLetter = "U:";

        public static string GetStagingDirectory()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrWhiteSpace(localAppData))
                {
                    return Path.Combine(localAppData, "PCAccess", "UserRemoteDriveRoot");
                }
            }
            catch { }

            string temp = Path.GetTempPath();
            return Path.Combine(temp, "PCAccess_UserRemoteDriveRoot");
        }

        private static string NormalizeDriveLetter(string driveLetter)
        {
            if (string.IsNullOrWhiteSpace(driveLetter)) return "U:";
            string clean = driveLetter.Trim().ToUpperInvariant();
            if (clean.EndsWith("\\")) clean = clean.TrimEnd('\\');
            if (!clean.EndsWith(":")) clean += ":";
            return clean.Length == 2 && char.IsLetter(clean[0]) ? clean : "U:";
        }

        /// <summary>
        /// Mounts the virtual drive letter and populates it with all permitted shared folders.
        /// </summary>
        public static bool MountDrive(
            string driveLetter, 
            string driveLabel, 
            List<UserAgentFolderDto> folders,
            string serverUrl = null,
            long userId = 0,
            string sessionToken = null)
        {
            lock (_syncLock)
            {
                try
                {
                    driveLetter = NormalizeDriveLetter(driveLetter);
                    driveLabel = string.IsNullOrWhiteSpace(driveLabel) ? "Remote Drive" : driveLabel.Trim();
                    _currentDriveLetter = driveLetter;

                    string stagingDir = GetStagingDirectory();
                    if (!Directory.Exists(stagingDir))
                    {
                        Directory.CreateDirectory(stagingDir);
                    }

                    UserAgentLogger.Drive($"Preparing {driveLetter} (\"{driveLabel}\") with {folders.Count} shared folders...");

                    // 1. Sync folders inside staging directory
                    SyncFoldersInternal(stagingDir, folders, serverUrl, userId, sessionToken);

                    // 2. Unmount any stale mapping first
                    UnmountDriveLetterInternal(driveLetter, stagingDir);

                    // 3. Map drive letter using subst and DefineDosDevice
                    bool mapped = MapDriveLetterInternal(driveLetter, stagingDir);

                    // 4. Set drive label in registry
                    SetDriveLabelInRegistry(driveLetter, driveLabel);

                    // 5. Notify Shell
                    NotifyShell(driveLetter, true);

                    _isMounted = mapped;

                    if (mapped)
                    {
                        UserAgentLogger.Success($"Successfully mounted {driveLetter} as \"{driveLabel}\" with {folders.Count} shared folders.");

                        // Auto-open Explorer to R:\
                        try
                        {
                            Process.Start(new ProcessStartInfo
                            {
                                FileName = "explorer.exe",
                                Arguments = driveLetter + @"\",
                                UseShellExecute = true
                            });
                        }
                        catch { }
                    }
                    else
                    {
                        UserAgentLogger.Warn($"Staging directory populated, but drive letter {driveLetter} could not be mapped directly.");
                    }

                    return mapped;
                }
                catch (Exception ex)
                {
                    UserAgentLogger.Error($"Mount failed: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// Unmounts the virtual drive letter cleanly.
        /// </summary>
        public static void UnmountDrive(string driveLetter = null)
        {
            lock (_syncLock)
            {
                try
                {
                    driveLetter = NormalizeDriveLetter(driveLetter ?? _currentDriveLetter);
                    string stagingDir = GetStagingDirectory();

                    UnmountDriveLetterInternal(driveLetter, stagingDir);
                    NotifyShell(driveLetter, false);

                    _isMounted = false;
                    UserAgentLogger.Drive($"Drive {driveLetter} unmounted cleanly.");
                }
                catch (Exception ex)
                {
                    UserAgentLogger.Error($"Unmount error: {ex.Message}");
                }
            }
        }

        #region Internal Helpers

        private static void SyncFoldersInternal(
            string stagingDir, 
            List<UserAgentFolderDto> folders,
            string serverUrl = null,
            long userId = 0,
            string sessionToken = null)
        {
            folders = folders ?? new List<UserAgentFolderDto>();
            var activeFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in folders)
            {
                if (string.IsNullOrWhiteSpace(folder.folder_name)) continue;

                string safeName = MakeSafeFolderName(folder.folder_name);
                activeFolderNames.Add(safeName);

                string targetDir = Path.Combine(stagingDir, safeName);

                // If folder is physically on this computer
                if (!string.IsNullOrWhiteSpace(folder.local_path) && Directory.Exists(folder.local_path))
                {
                    if (Directory.Exists(targetDir))
                    {
                        RemoveDirectoryOrJunction(targetDir);
                    }
                    CreateDirectoryJunction(targetDir, folder.local_path);
                }
                else
                {
                    // Remote folder hosted on another machine
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                    }

                    // 1. Write folder metadata info file
                    try
                    {
                        string infoPath = Path.Combine(targetDir, "README_Folder_Details.txt");
                        string infoContent = $"===============================================================\r\n" +
                                             $" PCAccess Shared Folder: {folder.folder_name}\r\n" +
                                             $"===============================================================\r\n" +
                                             $"Host Workstation : {folder.device_name}\r\n" +
                                             $"Device Status    : {(folder.is_device_online ? "ONLINE (Active)" : "OFFLINE")}\r\n" +
                                             $"Remote Path      : {folder.local_path}\r\n" +
                                             $"Permissions      : View={(folder.can_view ? "YES" : "NO")}, " +
                                             $"Download={(folder.can_download ? "YES" : "NO")}, " +
                                             $"Upload={(folder.can_upload ? "YES" : "NO")}\r\n" +
                                             $"Description      : {folder.description}\r\n" +
                                             $"===============================================================\r\n" +
                                             $"This folder is connected live to {folder.device_name}.\r\n";
                        File.WriteAllText(infoPath, infoContent);

                        // 2. Write Windows Web Shortcut to browse files online directly
                        if (!string.IsNullOrWhiteSpace(serverUrl))
                        {
                            string urlPath = Path.Combine(targetDir, "Open_In_Browser.url");
                            string urlContent = $"[InternetShortcut]\r\nURL={serverUrl.TrimEnd('/')}/Admin/FileBrowser?folderGuid={folder.folder_guid}\r\nIconIndex=0\r\n";
                            File.WriteAllText(urlPath, urlContent);
                        }

                        // 3. Task 3.2: If device is online, discover remote files and populate file stubs
                        if (folder.is_device_online && !string.IsNullOrWhiteSpace(serverUrl) && userId > 0)
                        {
                            PopulateRemoteFolderItems(targetDir, folder, serverUrl, userId, sessionToken);
                        }
                    }
                    catch { }
                }
            }
        }

        private static void PopulateRemoteFolderItems(string targetDir, UserAgentFolderDto folder, string serverUrl, long userId, string sessionToken)
        {
            try
            {
                var task = UserApiClient.GetFolderContentsAsync(serverUrl, userId, sessionToken, folder.folder_guid.ToString(), "");
                task.Wait(4000); // Fast initial discovery budget
                var res = task.Result;

                if (res != null && res.status == "success" && res.data != null && res.data.items != null)
                {
                    foreach (var item in res.data.items)
                    {
                        if (item.is_folder)
                        {
                            string subDir = Path.Combine(targetDir, item.name);
                            if (!Directory.Exists(subDir)) Directory.CreateDirectory(subDir);
                        }
                        else
                        {
                            string filePath = Path.Combine(targetDir, item.name);
                            if (!File.Exists(filePath))
                            {
                                string stubContent = $"[PCAccess Remote File Stub]\r\n" +
                                                     $"FileName: {item.name}\r\n" +
                                                     $"Size: {item.size_formatted} ({item.size_bytes} bytes)\r\n" +
                                                     $"LastModified: {item.last_modified:yyyy-MM-dd HH:mm:ss}\r\n" +
                                                     $"Folder: {folder.folder_name}\r\n" +
                                                     $"HostDevice: {folder.device_name}\r\n" +
                                                     $"\r\n(To download the complete file to this drive, press [P] in the UserLoginAgent console window.)\r\n";
                                File.WriteAllText(filePath, stubContent);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                UserAgentLogger.Warn($"Remote discovery for '{folder.folder_name}': {ex.Message}");
            }
        }

        /// <summary>
        /// WHAT: Downloads and synchronizes all remote files from online shared folders directly into the local drive.
        /// REASON: Task 3.2 - Allows users to pull remote files so they can be opened and edited locally.
        /// </summary>
        public static async System.Threading.Tasks.Task SyncAllRemoteFilesAsync(UserAgentLoginResponse user, string serverUrl, string driveLetter)
        {
            if (user == null || user.folders == null || user.folders.Count == 0)
            {
                UserAgentLogger.Warn("No folders available to sync.");
                return;
            }

            string stagingDir = GetStagingDirectory();
            int downloadedCount = 0;
            long totalBytesDownloaded = 0;

            UserAgentLogger.Info($"[SYNC] Starting remote files synchronization for {user.folders.Count} folder(s)...");

            foreach (var folder in user.folders)
            {
                // Only sync remote folders (local ones already have real junctions)
                if (!string.IsNullOrWhiteSpace(folder.local_path) && Directory.Exists(folder.local_path))
                {
                    continue;
                }

                if (!folder.is_device_online)
                {
                    UserAgentLogger.Warn($"[SYNC:SKIP] '{folder.folder_name}' is offline on '{folder.device_name}'.");
                    continue;
                }

                if (!folder.can_download)
                {
                    UserAgentLogger.Warn($"[SYNC:SKIP] You do not have download permission for '{folder.folder_name}'.");
                    continue;
                }

                string safeName = MakeSafeFolderName(folder.folder_name);
                string targetDir = Path.Combine(stagingDir, safeName);
                if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                UserAgentLogger.Info($"[SYNC] Querying files for '{folder.folder_name}'...");
                var listRes = await UserApiClient.GetFolderContentsAsync(serverUrl, user.user_id, user.session_token, folder.folder_guid.ToString(), "");

                if (listRes != null && listRes.status == "success" && listRes.data != null && listRes.data.items != null)
                {
                    var files = listRes.data.items.FindAll(i => !i.is_folder);
                    UserAgentLogger.Info($"[SYNC] Found {files.Count} file(s) in '{folder.folder_name}'. Downloading...");

                    foreach (var file in files)
                    {
                        string localPath = Path.Combine(targetDir, file.name);
                        bool ok = await UserApiClient.DownloadFileAsync(serverUrl, user.user_id, folder.folder_guid.ToString(), file.relative_path, localPath);
                        if (ok)
                        {
                            downloadedCount++;
                            totalBytesDownloaded += file.size_bytes;
                            UserAgentLogger.Success($"  -> Downloaded: {file.name} ({file.size_formatted})");
                        }
                        else
                        {
                            UserAgentLogger.Warn($"  -> Failed: {file.name}");
                        }
                    }
                }
            }

            UserAgentLogger.Success($"[SYNC:COMPLETE] Successfully synced {downloadedCount} file(s) ({(totalBytesDownloaded / (1024.0 * 1024.0)):F2} MB) into {driveLetter}.");
        }

        private static bool MapDriveLetterInternal(string driveLetter, string stagingDir)
        {
            try
            {
                // Method 1: subst command
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c subst {driveLetter} \"{stagingDir}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using (var proc = Process.Start(psi))
                {
                    proc?.WaitForExit(3000);
                    if (proc != null && proc.ExitCode == 0 && Directory.Exists(driveLetter + @"\"))
                    {
                        return true;
                    }
                }
            }
            catch { }

            try
            {
                // Method 2: DefineDosDevice Win32 API
                bool res = DefineDosDevice(0, driveLetter, stagingDir);
                if (res && Directory.Exists(driveLetter + @"\")) return true;
            }
            catch { }

            return Directory.Exists(driveLetter + @"\");
        }

        private static void UnmountDriveLetterInternal(string driveLetter, string stagingDir)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c subst {driveLetter} /D",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (var proc = Process.Start(psi)) { proc?.WaitForExit(2000); }
            }
            catch { }

            try
            {
                DefineDosDevice(DDD_REMOVE_DEFINITION | DDD_EXACT_MATCH_ON_REMOVE, driveLetter, stagingDir);
            }
            catch { }
        }

        private static void CreateDirectoryJunction(string junctionPath, string targetPath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c mklink /J \"{junctionPath}\" \"{targetPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (var proc = Process.Start(psi)) { proc?.WaitForExit(3000); }
            }
            catch { }
        }

        private static void RemoveDirectoryOrJunction(string path)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c rmdir \"{path}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using (var proc = Process.Start(psi)) { proc?.WaitForExit(2000); }
            }
            catch { }
        }

        private static void SetDriveLabelInRegistry(string driveLetter, string driveLabel)
        {
            try
            {
                char letter = driveLetter[0];
                string keyPath = $@"Software\Microsoft\Windows\CurrentVersion\Explorer\DriveIcons\{letter}\DefaultLabel";
                using (RegistryKey key = Registry.CurrentUser.CreateSubKey(keyPath))
                {
                    key?.SetValue("", driveLabel);
                }
            }
            catch { }
        }

        private static void NotifyShell(string driveLetter, bool isAdded)
        {
            try
            {
                int eventId = isAdded ? SHCNE_DRIVEADD : SHCNE_DRIVEREMOVED;
                IntPtr pDrive = Marshal.StringToHGlobalAuto(driveLetter + @"\");
                SHChangeNotify(eventId, SHCNF_PATH | SHCNF_FLUSH, pDrive, IntPtr.Zero);
                Marshal.FreeHGlobal(pDrive);
                SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
            }
            catch { }
        }

        private static string MakeSafeFolderName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            foreach (char c in invalid) name = name.Replace(c, '_');
            return name.Trim();
        }

        #endregion
    }
}
