using System.Collections.Generic;
using System.Linq;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Microsoft.AspNet.SignalR;
using PCAccess.BAL;
using PCAccess.Models;

namespace PCAccess.Hubs
{
    /// <summary>
    /// SignalR Hub for real-time PC Agent and Dashboard communication.
    /// WHAT: Manages persistent connections from Windows PC Agents and Web Dashboards.
    /// REASON: Enables instant status propagation (Online/Offline) and interactive folder path validation.
    /// </summary>
    public class DeviceHub : Hub
    {
        // Thread-safe map of ConnectionId -> (DeviceGuid, UserId) for offline detection
        private static readonly ConcurrentDictionary<string, Tuple<Guid, long>> _connectionDeviceMap =
            new ConcurrentDictionary<string, Tuple<Guid, long>>();

        // Thread-safe map of DeviceGuid -> ConnectionId for routing commands to specific PC Agents
        private static readonly ConcurrentDictionary<Guid, string> _deviceConnectionMap =
            new ConcurrentDictionary<Guid, string>();

        // Thread-safe map of requestId -> TaskCompletionSource for awaiting directory listing responses
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<DirectoryListingResult>> _pendingDirectoryRequests =
            new ConcurrentDictionary<string, TaskCompletionSource<DirectoryListingResult>>();

        /// <summary>
        /// WHAT: Called by the web browser dashboard when user logs in.
        /// REASON: Joins the user to a private SignalR group (User_{userId}) to receive targeted device & folder updates.
        /// </summary>
        public async Task JoinDashboardGroup(long userId)
        {
            if (userId > 0)
            {
                await Groups.Add(Context.ConnectionId, "User_" + userId);
            }
        }

        /// <summary>
        /// WHAT: Called by PC Agent upon establishing SignalR connection.
        /// REASON: Authenticates agent credentials, marks device online in SQL Server,
        /// stores connection mapping, and broadcasts real-time status to the owner's dashboard.
        /// </summary>
        public async Task<bool> RegisterAgent(string deviceGuidStr, string deviceToken)
        {
            if (string.IsNullOrWhiteSpace(deviceGuidStr) || string.IsNullOrWhiteSpace(deviceToken))
            {
                return false;
            }

            if (!Guid.TryParse(deviceGuidStr, out Guid deviceGuid))
            {
                return false;
            }

            string status, msg;
            DeviceModel device = DeviceManager.AuthenticateDevice(deviceGuid, deviceToken, out status, out msg);

            if (device != null && status == "success")
            {
                // Update device status to 'online' with connection ID
                DeviceManager.UpdateStatus(deviceGuid, "online", Context.ConnectionId, out status, out msg);

                // Store connection mappings for routing and disconnect cleanup
                _connectionDeviceMap[Context.ConnectionId] = Tuple.Create(deviceGuid, device.user_id);
                _deviceConnectionMap[deviceGuid] = Context.ConnectionId;

                // Broadcast real-time status update to user's dashboard group
                Clients.Group("User_" + device.user_id).deviceStatusChanged(new
                {
                    deviceGuid = deviceGuid.ToString(),
                    status = "online",
                    lastSeen = "Just now"
                });

                return true;
            }

            return false;
        }

        /// <summary>
        /// WHAT: Called by PC Agent to retrieve its active shared folders.
        /// REASON: Enables FileAccessAgent to dynamically mount "Remote Drive (R:)" containing these folders.
        /// </summary>
        public async Task<List<AgentSharedFolderDto>> GetDeviceSharedFolders(string deviceGuidStr, string deviceToken)
        {
            if (string.IsNullOrWhiteSpace(deviceGuidStr) || string.IsNullOrWhiteSpace(deviceToken))
            {
                return new List<AgentSharedFolderDto>();
            }

            if (!Guid.TryParse(deviceGuidStr, out Guid deviceGuid))
            {
                return new List<AgentSharedFolderDto>();
            }

            string status, msg;
            DeviceModel device = DeviceManager.AuthenticateDevice(deviceGuid, deviceToken, out status, out msg);
            if (device == null || status != "success")
            {
                return new List<AgentSharedFolderDto>();
            }

            List<SharedFolderModel> folders = FolderManager.GetDeviceFolders(device.user_id, deviceGuid.ToString(), out status, out msg);
            if (folders == null) return new List<AgentSharedFolderDto>();

            var result = new List<AgentSharedFolderDto>();
            foreach (var f in folders)
            {
                if (f.is_active)
                {
                    result.Add(new AgentSharedFolderDto
                    {
                        folder_id = f.folder_id,
                        folder_name = f.folder_name,
                        local_path = f.local_path,
                        description = f.description
                    });
                }
            }
            return result;
        }

        /// <summary>
        /// WHAT: Dispatches a sync notification to a connected PC Agent when its shared folders change.
        /// REASON: Updates the local "Remote Drive (R:)" directory junctions in real-time without reconnection.
        /// </summary>
        public static void NotifyAgentToSyncFolders(string deviceGuidStr)
        {
            if (Guid.TryParse(deviceGuidStr, out Guid deviceGuid) && _deviceConnectionMap.TryGetValue(deviceGuid, out string agentConnectionId))
            {
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<DeviceHub>();
                hubContext.Clients.Client(agentConnectionId).syncSharedFolders();
            }
        }

        /// <summary>
        /// WHAT: Called by PC Agent periodically every 30 seconds.
        /// REASON: Heartbeat mechanism to refresh last_seen timestamp in database.
        /// </summary>
        public bool SendHeartbeat(string deviceGuidStr)
        {
            if (Guid.TryParse(deviceGuidStr, out Guid deviceGuid))
            {
                return DeviceManager.RecordHeartbeat(deviceGuid);
            }
            return false;
        }

        /// <summary>
        /// WHAT: Called by web browser to validate a local folder path on a specific connected PC.
        /// REASON: Dispatches validation to the actual PC Agent via SignalR. Never trust browser paths directly.
        /// </summary>
        public void ValidateFolderOnAgent(string deviceGuidStr, string folderPath, string requestId)
        {
            if (string.IsNullOrWhiteSpace(deviceGuidStr) || string.IsNullOrWhiteSpace(folderPath))
            {
                Clients.Caller.folderValidationResult(requestId, false, "Device GUID and Folder Path are required.", folderPath);
                return;
            }

            if (!Guid.TryParse(deviceGuidStr, out Guid deviceGuid))
            {
                Clients.Caller.folderValidationResult(requestId, false, "Invalid Device GUID.", folderPath);
                return;
            }

            if (_deviceConnectionMap.TryGetValue(deviceGuid, out string agentConnectionId))
            {
                // Dispatch command directly to the PC Agent
                Clients.Client(agentConnectionId).validateFolder(requestId, folderPath);
            }
            else
            {
                // Device Agent is offline or not connected
                Clients.Caller.folderValidationResult(requestId, false, "Device Agent is offline. Please start FileAccessAgent on the PC.", folderPath);
            }
        }

        /// <summary>
        /// WHAT: Called by PC Agent with local directory validation result.
        /// REASON: Forwards the result to the device owner's dashboard to unlock the Save button.
        /// </summary>
        public void FolderValidationResult(string requestId, bool isValid, string message, string normalizedPath)
        {
            if (_connectionDeviceMap.TryGetValue(Context.ConnectionId, out Tuple<Guid, long> mapping))
            {
                long userId = mapping.Item2;
                Clients.Group("User_" + userId).folderValidationResult(requestId, isValid, message, normalizedPath);
            }
            else
            {
                Clients.All.folderValidationResult(requestId, isValid, message, normalizedPath);
            }
        }

        /// <summary>
        /// WHAT: Triggered when an Agent or Dashboard client disconnects.
        /// REASON: Detects Agent disconnection immediately and marks device offline in database
        /// and notifies the owner's web dashboard in real time.
        /// </summary>
        /// <summary>
        /// WHAT: Dispatches a directory reading request to a connected PC Agent and asynchronously awaits the result.
        /// REASON: Step 4 Remote File Browser - Clean async REST integration. 
        /// Avoids browser WebSocket complexity while enforcing strict 10-second timeout.
        /// </summary>
        public static async Task<DirectoryListingResult> RequestDirectoryListingAsync(Guid deviceGuid, string requestId, string rootLocalPath, string relativePath, int timeoutSeconds = 10)
        {
            if (!_deviceConnectionMap.TryGetValue(deviceGuid, out string agentConnectionId) || string.IsNullOrWhiteSpace(agentConnectionId))
            {
                return new DirectoryListingResult
                {
                    request_id = requestId,
                    is_success = false,
                    error_message = "The target device is offline or disconnected. Please ensure the File Access Agent is running on the host PC."
                };
            }

            var tcs = new TaskCompletionSource<DirectoryListingResult>();
            _pendingDirectoryRequests[requestId] = tcs;

            try
            {
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<DeviceHub>();
                hubContext.Clients.Client(agentConnectionId).readDirectory(requestId, rootLocalPath, relativePath);

                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
                if (completedTask == tcs.Task)
                {
                    return await tcs.Task;
                }
                else
                {
                    return new DirectoryListingResult
                    {
                        request_id = requestId,
                        is_success = false,
                        error_message = "Request timed out. The PC Agent took longer than 10 seconds to read the directory."
                    };
                }
            }
            finally
            {
                _pendingDirectoryRequests.TryRemove(requestId, out _);
            }
        }

        /// <summary>
        /// WHAT: Invoked by the PC Agent with directory contents.
        /// REASON: Resolves the waiting TaskCompletionSource so the API can return a standard HTTP JSON response.
        /// </summary>
        public void DirectoryContentsResult(string requestId, bool isSuccess, string errorMessage, string currentRelativePath, List<FileItemDto> items)
        {
            if (string.IsNullOrWhiteSpace(requestId)) return;

            if (_pendingDirectoryRequests.TryGetValue(requestId, out TaskCompletionSource<DirectoryListingResult> tcs))
            {
                items = items ?? new List<FileItemDto>();
                int totalFolders = items.Count(i => i.is_folder);
                int totalFiles = items.Count(i => !i.is_folder);
                long totalBytes = items.Where(i => !i.is_folder).Sum(i => i.size_bytes);

                var result = new DirectoryListingResult
                {
                    request_id = requestId,
                    is_success = isSuccess,
                    error_message = errorMessage,
                    current_relative_path = currentRelativePath ?? "",
                    items = items,
                    total_folders = totalFolders,
                    total_files = totalFiles,
                    total_size_bytes = totalBytes
                };

                tcs.TrySetResult(result);
            }
        }


        // ==========================================
        // STEP 5: SECURE FILE DOWNLOAD — Chunk Buffers & Transfer TCS
        // ==========================================

        // Thread-safe map of requestId -> TaskCompletionSource for awaiting file transfer responses
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<FileTransferResult>> _pendingFileRequests =
            new ConcurrentDictionary<string, TaskCompletionSource<FileTransferResult>>();

        // Thread-safe chunk buffer: requestId -> accumulated byte list
        private static readonly ConcurrentDictionary<string, System.Collections.Generic.List<byte>> _fileChunkBuffers =
            new ConcurrentDictionary<string, System.Collections.Generic.List<byte>>();

        /// <summary>
        /// WHAT: Dispatches a file read request to a connected PC Agent and asynchronously awaits all chunks.
        /// REASON: Step 5 Secure File Download — transfers file data in 64KB Base64-encoded chunks via SignalR.
        /// </summary>
        public static async Task<FileTransferResult> RequestFileTransferAsync(Guid deviceGuid, string requestId, string rootLocalPath, string relativePath, int timeoutSeconds = 120)
        {
            if (!_deviceConnectionMap.TryGetValue(deviceGuid, out string agentConnectionId) || string.IsNullOrWhiteSpace(agentConnectionId))
            {
                return new FileTransferResult
                {
                    request_id = requestId,
                    is_success = false,
                    error_message = "The target device is offline or disconnected. Please ensure the File Access Agent is running on the host PC."
                };
            }

            var tcs = new TaskCompletionSource<FileTransferResult>();
            _pendingFileRequests[requestId] = tcs;
            _fileChunkBuffers[requestId] = new System.Collections.Generic.List<byte>();

            try
            {
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<DeviceHub>();
                hubContext.Clients.Client(agentConnectionId).readFile(requestId, rootLocalPath, relativePath);

                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
                if (completedTask == tcs.Task)
                {
                    return await tcs.Task;
                }
                else
                {
                    return new FileTransferResult
                    {
                        request_id = requestId,
                        is_success = false,
                        error_message = $"Request timed out. The PC Agent took longer than {timeoutSeconds} seconds to transfer the file."
                    };
                }
            }
            finally
            {
                _pendingFileRequests.TryRemove(requestId, out _);
                _fileChunkBuffers.TryRemove(requestId, out _);
            }
        }

        /// <summary>
        /// WHAT: Called by PC Agent with a single Base64-encoded chunk of file data.
        /// REASON: Step 5 — Agent sends file in 64KB pieces; this accumulates them in the buffer.
        /// </summary>
        public void FileChunkResult(string requestId, int chunkIndex, int totalChunks, string base64Data)
        {
            if (string.IsNullOrWhiteSpace(requestId)) return;

            if (_fileChunkBuffers.TryGetValue(requestId, out var buffer))
            {
                try
                {
                    byte[] chunkBytes = Convert.FromBase64String(base64Data);
                    lock (buffer)
                    {
                        buffer.AddRange(chunkBytes);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"[FileChunkResult] Chunk decode error for {requestId}: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// WHAT: Called by PC Agent when all chunks have been sent (success or failure).
        /// REASON: Step 5 — Resolves the waiting TaskCompletionSource so the API can return the file stream.
        /// </summary>
        public void FileTransferComplete(string requestId, bool isSuccess, string errorMessage, string fileName, string contentType, long fileSizeBytes)
        {
            if (string.IsNullOrWhiteSpace(requestId)) return;

            if (_pendingFileRequests.TryGetValue(requestId, out var tcs))
            {
                _fileChunkBuffers.TryGetValue(requestId, out var buffer);
                byte[] fileData = buffer != null ? buffer.ToArray() : new byte[0];

                var result = new FileTransferResult
                {
                    request_id = requestId,
                    is_success = isSuccess,
                    error_message = errorMessage,
                    file_name = fileName,
                    content_type = contentType,
                    file_size_bytes = fileSizeBytes,
                    data = fileData
                };

                tcs.TrySetResult(result);
            }
        }

        // ==========================================
        // STEP 6: SECURE REMOTE FILE UPLOAD - Transfer Coordination
        // ==========================================

        // Thread-safe map of requestId -> TaskCompletionSource for awaiting file upload completion
        private static readonly ConcurrentDictionary<string, TaskCompletionSource<UploadTransferResult>> _pendingUploadRequests =
            new ConcurrentDictionary<string, TaskCompletionSource<UploadTransferResult>>();

        /// <summary>
        /// WHAT: Dispatches a file upload command to a connected PC Agent and awaits confirmation of file write.
        /// REASON: Step 6 Secure Remote File Upload - notifies the agent to stream the file from server via HTTP,
        /// write to a temporary file, and atomically move it to the target directory.
        /// </summary>
        public static async Task<UploadTransferResult> RequestUploadTransferAsync(
            Guid deviceGuid,
            string requestId,
            string rootLocalPath,
            string relativePath,
            string fileName,
            string conflictAction,
            string uploadToken,
            long fileSizeBytes,
            int timeoutSeconds = 180)
        {
            if (!_deviceConnectionMap.TryGetValue(deviceGuid, out string agentConnectionId) || string.IsNullOrWhiteSpace(agentConnectionId))
            {
                return new UploadTransferResult
                {
                    request_id = requestId,
                    is_success = false,
                    error_message = "The target device is offline or disconnected. Please ensure the File Access Agent is running on the host PC."
                };
            }

            var tcs = new TaskCompletionSource<UploadTransferResult>();
            _pendingUploadRequests[requestId] = tcs;

            try
            {
                var hubContext = GlobalHost.ConnectionManager.GetHubContext<DeviceHub>();
                hubContext.Clients.Client(agentConnectionId).receiveUpload(
                    requestId,
                    rootLocalPath,
                    relativePath,
                    fileName,
                    conflictAction,
                    uploadToken,
                    fileSizeBytes
                );

                var completedTask = await Task.WhenAny(tcs.Task, Task.Delay(TimeSpan.FromSeconds(timeoutSeconds)));
                if (completedTask == tcs.Task)
                {
                    return await tcs.Task;
                }
                else
                {
                    return new UploadTransferResult
                    {
                        request_id = requestId,
                        is_success = false,
                        error_message = $"Upload timed out. The PC Agent took longer than {timeoutSeconds} seconds to write the file."
                    };
                }
            }
            finally
            {
                _pendingUploadRequests.TryRemove(requestId, out _);
            }
        }

        /// <summary>
        /// WHAT: Called by PC Agent when a file upload transfer and atomic write has finished (success or failure).
        /// REASON: Step 6 - Resolves the waiting TaskCompletionSource so the API can return the upload response.
        /// </summary>
        public void UploadTransferComplete(string requestId, bool isSuccess, string errorMessage, string finalFileName, long fileSizeBytes)
        {
            if (string.IsNullOrWhiteSpace(requestId)) return;

            if (_pendingUploadRequests.TryGetValue(requestId, out var tcs))
            {
                var result = new UploadTransferResult
                {
                    request_id = requestId,
                    is_success = isSuccess,
                    error_message = errorMessage,
                    final_file_name = finalFileName,
                    file_size_bytes = fileSizeBytes
                };

                tcs.TrySetResult(result);
            }
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            if (_connectionDeviceMap.TryRemove(Context.ConnectionId, out Tuple<Guid, long> mapping))
            {
                Guid deviceGuid = mapping.Item1;
                long userId = mapping.Item2;

                _deviceConnectionMap.TryRemove(deviceGuid, out _);

                string status, msg;
                DeviceManager.UpdateStatus(deviceGuid, "offline", null, out status, out msg);

                // Notify dashboard group that device went offline
                Clients.Group("User_" + userId).deviceStatusChanged(new
                {
                    deviceGuid = deviceGuid.ToString(),
                    status = "offline",
                    lastSeen = "Just now"
                });
            }

            return base.OnDisconnected(stopCalled);
        }
    }
}

