using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace FileAccessAgent
{
    /// <summary>
    /// Manages the virtual/mapped Windows Drive (e.g. R: "Remote Drive") under "This PC".
    /// WHAT: Automatically mounts a virtual drive letter containing NTFS directory junctions for all active
    /// shared folders configured on this device in PCAccess.
    /// REASON: Fulfills the user requirement: when the agent connects, a virtual drive appears in Windows Explorer
    /// showing all shared folders. Double-clicking any folder opens its contents directly at full native speed.
    /// On disconnect or agent exit, the drive is cleanly unmounted.
    /// </summary>
    public static class RemoteDriveManager
    {
        #region Win32 Native APIs

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool DefineDosDevice(int dwFlags, string lpDeviceName, string? lpTargetPath);

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

        /// <summary>
        /// WHAT: Gets the local staging root directory where directory junctions reside.
        /// REASON: Provides an isolated local folder that gets mounted to the drive letter (R:).
        /// </summary>
        public static string GetStagingDirectory()
        {
            try
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (!string.IsNullOrWhiteSpace(localAppData) && Directory.Exists(localAppData))
                {
                    return Path.Combine(localAppData, "PCAccess", "RemoteDriveRoot");
                }
            }
            catch { }

            // Fallback for Windows Service / LocalSystem environment
            string commonAppData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            return Path.Combine(commonAppData, "PCAccess", "RemoteDriveRoot");
        }

        /// <summary>
        /// WHAT: Normalizes a drive letter string to standard format (e.g. "R:").
        /// </summary>
        private static string NormalizeDriveLetter(string driveLetter)
        {
            if (string.IsNullOrWhiteSpace(driveLetter)) return "R:";
            string clean = driveLetter.Trim().ToUpperInvariant();
            if (clean.EndsWith("\\")) clean = clean.TrimEnd('\\');
            if (!clean.EndsWith(":")) clean += ":";
            return clean.Length == 2 && char.IsLetter(clean[0]) ? clean : "R:";
        }

        /// <summary>
        /// WHAT: Mounts the virtual drive letter and populates it with directory junctions for each shared folder.
        /// REASON: Executed upon successful agent connection (Online status).
        /// </summary>
        public static bool MountDrive(string driveLetter, string driveLabel, List<AgentSharedFolderDto> folders)
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

                    AgentLogger.Info($"[REMOTE DRIVE] Preparing drive {driveLetter} (\"{driveLabel}\") at staging path: {stagingDir}");

                    // 1. Sync directory junctions inside the staging folder
                    SyncJunctionsInternal(stagingDir, folders);

                    // 2. Unmount any stale mapping on this drive letter first
                    UnmountDriveLetterInternal(driveLetter, stagingDir);

                    // 3. Map drive letter using subst and DefineDosDevice
                    bool mapped = MapDriveLetterInternal(driveLetter, stagingDir);

                    // 4. Set Windows Explorer Drive Display Name in Registry
                    SetDriveLabelInRegistry(driveLetter, driveLabel);

                    // 5. Notify Windows Shell to refresh Explorer
                    NotifyShell(driveLetter, true);

                    _isMounted = mapped;

                    if (mapped)
                    {
                        AgentLogger.Info($"[REMOTE DRIVE: SUCCESS] Successfully mounted {driveLetter} as \"{driveLabel}\" with {folders.Count} shared folders.");

                        // Auto-open Explorer directly to R:\ so it instantly pops up on the user's screen!
                        if (Environment.UserInteractive)
                        {
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
                    }
                    else
                    {
                        AgentLogger.Warn($"[REMOTE DRIVE: WARN] Staging folder populated, but drive letter {driveLetter} could not be mapped directly.");
                    }

                    return mapped;
                }
                catch (Exception ex)
                {
                    AgentLogger.Error($"[REMOTE DRIVE: ERROR] Mount failed: {ex.Message}");
                    return false;
                }
            }
        }

        /// <summary>
        /// WHAT: Synchronizes shared folders inside the drive without unmounting.
        /// REASON: Invoked when shared folders are added, modified, or deleted on the web portal.
        /// </summary>
        public static void SyncFolders(string driveLetter, string driveLabel, List<AgentSharedFolderDto> folders)
        {
            lock (_syncLock)
            {
                try
                {
                    driveLetter = NormalizeDriveLetter(driveLetter);
                    string stagingDir = GetStagingDirectory();

                    if (!Directory.Exists(stagingDir))
                    {
                        Directory.CreateDirectory(stagingDir);
                    }

                    SyncJunctionsInternal(stagingDir, folders);

                    // If not currently mounted, mount it
                    if (!_isMounted)
                    {
                        MountDrive(driveLetter, driveLabel, folders);
                    }
                    else
                    {
                        NotifyShell(driveLetter, true);
                        AgentLogger.Info($"[REMOTE DRIVE: SYNC] Synced {folders.Count} shared folders on {driveLetter}.");
                    }
                }
                catch (Exception ex)
                {
                    AgentLogger.Error($"[REMOTE DRIVE: SYNC ERROR] {ex.Message}");
                }
            }
        }

        /// <summary>
        /// WHAT: Unmounts the virtual drive letter and cleans up staging junctions.
        /// REASON: Executed upon agent disconnect, Ctrl+C shutdown, or service stop.
        /// </summary>
        public static void UnmountDrive(string? driveLetter = null)
        {
            lock (_syncLock)
            {
                try
                {
                    driveLetter = NormalizeDriveLetter(driveLetter ?? _currentDriveLetter);
                    string stagingDir = GetStagingDirectory();

                    AgentLogger.Info($"[REMOTE DRIVE] Unmounting {driveLetter}...");

                    // 1. Unmount drive letter
                    UnmountDriveLetterInternal(driveLetter, stagingDir);

                    // 2. Notify Shell
                    NotifyShell(driveLetter, false);

                    // 3. Remove stale junctions from staging folder
                    CleanAllJunctionsInternal(stagingDir);

                    _isMounted = false;
                    AgentLogger.Info($"[REMOTE DRIVE] Drive {driveLetter} unmounted cleanly.");
                }
                catch (Exception ex)
                {
                    AgentLogger.Error($"[REMOTE DRIVE: UNMOUNT ERROR] {ex.Message}");
                }
            }
        }

        #region Internal Helper Methods

        /// <summary>
        /// WHAT: Updates NTFS directory junctions inside stagingDir to mirror the given folders list.
        /// </summary>
        private static void SyncJunctionsInternal(string stagingDir, List<AgentSharedFolderDto> folders)
        {
            folders = folders ?? new List<AgentSharedFolderDto>();

            // Map safe folder names to their targets
            var desiredFolderNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var folder in folders)
            {
                if (string.IsNullOrWhiteSpace(folder.folder_name) || string.IsNullOrWhiteSpace(folder.local_path))
                    continue;

                string safeName = MakeSafeFolderName(folder.folder_name);
                desiredFolderNames.Add(safeName);

                string junctionPath = Path.Combine(stagingDir, safeName);

                // Verify local physical path exists
                if (!Directory.Exists(folder.local_path))
                {
                    AgentLogger.Warn($"[REMOTE DRIVE: SKIP] Target path does not exist on disk: \"{folder.local_path}\"");
                    continue;
                }

                // If junction already exists and points to correct path, keep it
                if (Directory.Exists(junctionPath))
                {
                    try
                    {
                        // Remove existing link/directory to refresh
                        RemoveDirectoryOrJunction(junctionPath);
                    }
                    catch (Exception ex)
                    {
                        AgentLogger.Warn($"[REMOTE DRIVE] Could not refresh existing link: {ex.Message}");
                    }
                }

                // Create junction using cmd /c mklink /J
                CreateDirectoryJunction(junctionPath, folder.local_path);
            }

            // Remove any obsolete junctions not in desiredFolderNames
            if (Directory.Exists(stagingDir))
            {
                try
                {
                    var existingDirs = Directory.GetDirectories(stagingDir);
                    foreach (var dir in existingDirs)
                    {
                        string dirName = Path.GetFileName(dir);
                        if (!desiredFolderNames.Contains(dirName))
                        {
                            RemoveDirectoryOrJunction(dir);
                            AgentLogger.Info($"[REMOTE DRIVE] Removed obsolete folder link: {dirName}");
                        }
                    }
                }
                catch { }
            }
        }

        /// <summary>
        /// WHAT: Creates an NTFS directory junction via mklink /J.
        /// </summary>
        private static void CreateDirectoryJunction(string junctionPath, string targetPath)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c mklink /J \"{junctionPath}\" \"{targetPath}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit(3000);
                    if (proc.ExitCode == 0)
                    {
                        AgentLogger.Info($"[REMOTE DRIVE: LINK] Created junction: {Path.GetFileName(junctionPath)} -> {targetPath}");
                    }
                    else
                    {
                        string err = proc.StandardError.ReadToEnd();
                        AgentLogger.Warn($"[REMOTE DRIVE: LINK WARN] mklink returned code {proc.ExitCode}: {err}");
                    }
                }
            }
            catch (Exception ex)
            {
                AgentLogger.Error($"[REMOTE DRIVE: LINK ERROR] Failed to create junction for {junctionPath}: {ex.Message}");
            }
        }

        /// <summary>
        /// WHAT: Safely deletes a directory junction or folder.
        /// </summary>
        private static void RemoveDirectoryOrJunction(string path)
        {
            try
            {
                if (Directory.Exists(path))
                {
                    // For junctions, rmdir cleanly unlinks without touching the target contents
                    var psi = new ProcessStartInfo
                    {
                        FileName = "cmd.exe",
                        Arguments = $"/c rmdir \"{path}\"",
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using var proc = Process.Start(psi);
                    proc?.WaitForExit(2000);

                    if (Directory.Exists(path))
                    {
                        Directory.Delete(path, false);
                    }
                }
            }
            catch (Exception ex)
            {
                AgentLogger.Warn($"[REMOTE DRIVE] Could not remove {path}: {ex.Message}");
            }
        }

        /// <summary>
        /// WHAT: Cleans all items inside the staging directory.
        /// </summary>
        private static void CleanAllJunctionsInternal(string stagingDir)
        {
            try
            {
                if (Directory.Exists(stagingDir))
                {
                    foreach (var dir in Directory.GetDirectories(stagingDir))
                    {
                        RemoveDirectoryOrJunction(dir);
                    }
                }
            }
            catch { }
        }

        /// <summary>
        /// WHAT: Maps the drive letter to stagingDir via cmd.exe /c subst, System32 subst, and DefineDosDevice.
        /// </summary>
        private static bool MapDriveLetterInternal(string driveLetter, string stagingDir)
        {
            bool success = false;

            // 1. Map via cmd.exe /c subst (guaranteed to resolve PATH and run in user session)
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c subst {driveLetter} \"{stagingDir}\"",
                    CreateNoWindow = true,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true
                };

                using var proc = Process.Start(psi);
                if (proc != null)
                {
                    proc.WaitForExit(3000);
                    if (proc.ExitCode == 0)
                    {
                        success = true;
                        AgentLogger.Info($"[REMOTE DRIVE: SUBST] Successfully mapped {driveLetter} -> \"{stagingDir}\" via cmd.exe subst.");
                    }
                    else
                    {
                        string err = proc.StandardError.ReadToEnd();
                        AgentLogger.Warn($"[REMOTE DRIVE: SUBST WARN] subst exit code {proc.ExitCode}: {err}");
                    }
                }
            }
            catch (Exception ex)
            {
                AgentLogger.Warn($"[REMOTE DRIVE: SUBST ERR] {ex.Message}");
            }

            // 2. Direct subst.exe fallback using System32 path
            if (!success)
            {
                try
                {
                    string substExe = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.System), "subst.exe");
                    if (File.Exists(substExe))
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = substExe,
                            Arguments = $"{driveLetter} \"{stagingDir}\"",
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using var proc = Process.Start(psi);
                        proc?.WaitForExit(3000);
                        if (proc?.ExitCode == 0)
                        {
                            success = true;
                            AgentLogger.Info($"[REMOTE DRIVE: SUBST] Mapped {driveLetter} -> \"{stagingDir}\" via System32\\subst.exe.");
                        }
                    }
                }
                catch { }
            }

            // 3. Define DOS Device API for kernel/service visibility
            try
            {
                DefineDosDevice(0, driveLetter, stagingDir);
                DefineDosDevice(DDD_RAW_TARGET_PATH, @"\GLOBAL??\" + driveLetter, @"\??\" + stagingDir);
            }
            catch { }

            return success;
        }

        /// <summary>
        /// WHAT: Unmounts the drive letter via cmd.exe /c subst /d and DefineDosDevice.
        /// </summary>
        private static void UnmountDriveLetterInternal(string driveLetter, string stagingDir)
        {
            // 1. Unmount via cmd.exe /c subst /d
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c subst {driveLetter} /d",
                    CreateNoWindow = true,
                    UseShellExecute = false
                };
                using var proc = Process.Start(psi);
                proc?.WaitForExit(2000);
            }
            catch { }

            // 2. Remove DefineDosDevice
            try
            {
                DefineDosDevice(DDD_REMOVE_DEFINITION | DDD_EXACT_MATCH_ON_REMOVE, driveLetter, stagingDir);
                DefineDosDevice(DDD_REMOVE_DEFINITION | DDD_RAW_TARGET_PATH, @"\GLOBAL??\" + driveLetter, null);
            }
            catch { }
        }

        /// <summary>
        /// WHAT: Registers the drive display name (e.g. "Remote Drive") in Windows Explorer registry.
        /// REASON: Causes Windows Explorer to label the drive as "Remote Drive (R:)" in "This PC".
        /// </summary>
        private static void SetDriveLabelInRegistry(string driveLetter, string driveLabel)
        {
            try
            {
                string letterOnly = driveLetter.TrimEnd(':').ToUpperInvariant();

                // Set in Current User (Explorer reads this for interactive user)
                using (var key = Registry.CurrentUser.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\Explorer\DriveIcons\{letterOnly}\DefaultLabel"))
                {
                    key?.SetValue("", driveLabel);
                }

                // Also attempt setting in Local Machine (for service / multi-user)
                try
                {
                    using (var key = Registry.LocalMachine.CreateSubKey($@"Software\Microsoft\Windows\CurrentVersion\Explorer\DriveIcons\{letterOnly}\DefaultLabel"))
                    {
                        key?.SetValue("", driveLabel);
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                AgentLogger.Warn($"[REMOTE DRIVE: REGISTRY] Could not set drive label: {ex.Message}");
            }
        }

        /// <summary>
        /// WHAT: Broadcasts shell notifications to immediately refresh File Explorer.
        /// </summary>
        private static void NotifyShell(string driveLetter, bool added)
        {
            try
            {
                string path = driveLetter + @"\";
                IntPtr pathPtr = Marshal.StringToHGlobalAuto(path);
                try
                {
                    int eventId = added ? SHCNE_DRIVEADD : SHCNE_DRIVEREMOVED;
                    SHChangeNotify(eventId, SHCNF_PATH, pathPtr, IntPtr.Zero);
                    if (added)
                    {
                        // 0x00010000 = SHCNE_DRIVEADDGUI (notifies Explorer UI explicitly)
                        SHChangeNotify(0x00010000, SHCNF_PATH, pathPtr, IntPtr.Zero);
                    }
                    SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_FLUSH, IntPtr.Zero, IntPtr.Zero);
                }
                finally
                {
                    Marshal.FreeHGlobal(pathPtr);
                }
            }
            catch { }
        }

        /// <summary>
        /// WHAT: Strips forbidden filesystem characters from folder name.
        /// </summary>
        private static string MakeSafeFolderName(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return "SharedFolder";
            char[] invalidChars = Path.GetInvalidFileNameChars();
            var cleaned = new string(name.Select(c => invalidChars.Contains(c) ? '_' : c).ToArray());
            return string.IsNullOrWhiteSpace(cleaned) ? "SharedFolder" : cleaned.Trim();
        }

        #endregion
    }
}
