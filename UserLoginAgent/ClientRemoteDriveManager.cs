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
        private static string _currentDriveLetter = "R:";

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
            if (string.IsNullOrWhiteSpace(driveLetter)) return "R:";
            string clean = driveLetter.Trim().ToUpperInvariant();
            if (clean.EndsWith("\\")) clean = clean.TrimEnd('\\');
            if (!clean.EndsWith(":")) clean += ":";
            return clean.Length == 2 && char.IsLetter(clean[0]) ? clean : "R:";
        }

        /// <summary>
        /// Mounts the virtual drive letter and populates it with all permitted shared folders.
        /// </summary>
        public static bool MountDrive(string driveLetter, string driveLabel, List<UserAgentFolderDto> folders)
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
                    SyncFoldersInternal(stagingDir, folders);

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

        private static void SyncFoldersInternal(string stagingDir, List<UserAgentFolderDto> folders)
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

                    // Write folder metadata info file
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
                    }
                    catch { }
                }
            }
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
