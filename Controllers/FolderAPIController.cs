using System.Web;
using System.Collections.Concurrent;
using System.Configuration;
﻿using System.Threading.Tasks;
using PCAccess.Hubs;
using System;
using System.Collections.Generic;
using System.Data;
using System.Web.Mvc;
using PCAccess.BAL;
using PCAccess.Models;
using PCAccess.MyFun;

namespace PCAccess.Controllers
{
    /// <summary>
    /// API Controller for Shared Folder Management & Permissions.
    /// WHAT: Exposes endpoints for folder CRUD and granular user access rights.
    /// REASON: Follows the established project pattern (AccountAPI / DeviceAPI),
    /// strictly enforcing server-side authenticated user verification.
    /// </summary>
    public class FolderAPIController : Controller
    {
        /// <summary>
        /// Saves a new shared folder for a connected device.
        /// </summary>
        [HttpPost]
        public JsonResult SaveSharedFolder(SaveSharedFolderRequest req)
        {
            try
            {
                OperatorModel user = CommanUtilities.Provider.GetCurrent();
                if (user == null || user.user_id <= 0)
                {
                    return Json(new { status = "error", message = "Unauthorized. Please login again." }, JsonRequestBehavior.AllowGet);
                }

                if (req == null || string.IsNullOrWhiteSpace(req.device_guid) || string.IsNullOrWhiteSpace(req.folder_name) || string.IsNullOrWhiteSpace(req.local_path))
                {
                    return Json(new { status = "error", message = "Folder Name and Local Path are required." }, JsonRequestBehavior.AllowGet);
                }

                string status, msg;
                SharedFolderModel folder = FolderManager.AddSharedFolder(user.user_id, req.device_guid, req.folder_name, req.local_path, req.description, out status, out msg);

                if (status == "success" && folder != null)
                {
                    // Real-time synchronization: notify connected agent to refresh its Remote Drive (R:) junctions
                    DeviceHub.NotifyAgentToSyncFolders(req.device_guid);

                    return Json(new
                    {
                        status = "success",
                        message = "Shared folder registered successfully.",
                        data = folder
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { status = "error", message = msg ?? "Failed to register shared folder." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Updates an existing shared folder metadata.
        /// </summary>
        [HttpPost]
        public JsonResult UpdateSharedFolder(SaveSharedFolderRequest req)
        {
            try
            {
                OperatorModel user = CommanUtilities.Provider.GetCurrent();
                if (user == null || user.user_id <= 0)
                {
                    return Json(new { status = "error", message = "Unauthorized. Please login again." }, JsonRequestBehavior.AllowGet);
                }

                if (req == null || !req.folder_id.HasValue || req.folder_id.Value <= 0)
                {
                    return Json(new { status = "error", message = "Folder ID is required for update." }, JsonRequestBehavior.AllowGet);
                }

                string status, msg;
                bool success = FolderManager.UpdateSharedFolder(
                    user.user_id, 
                    req.folder_id.Value, 
                    req.folder_name, 
                    req.description, 
                    req.is_active ?? true, 
                    out status, 
                    out msg
                );

                if (success)
                {
                    return Json(new { status = "success", message = "Shared folder updated successfully." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { status = "error", message = msg ?? "Failed to update shared folder." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Deactivates a shared folder record (Soft Delete).
        /// Physical folder on PC is never deleted.
        /// </summary>
        [HttpPost]
        public JsonResult DeleteSharedFolder(long folderId)
        {
            try
            {
                OperatorModel user = CommanUtilities.Provider.GetCurrent();
                if (user == null || user.user_id <= 0)
                {
                    return Json(new { status = "error", message = "Unauthorized. Please login again." }, JsonRequestBehavior.AllowGet);
                }

                if (folderId <= 0)
                {
                    return Json(new { status = "error", message = "Invalid Folder ID." }, JsonRequestBehavior.AllowGet);
                }

                string status, msg;
                bool success = FolderManager.DeleteSharedFolder(user.user_id, folderId, out status, out msg);

                if (success)
                {
                    return Json(new { status = "success", message = "Shared folder removed successfully." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { status = "error", message = msg ?? "Failed to remove shared folder." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Retrieves all active shared folders for a connected device.
        /// </summary>
        [HttpGet]
        public JsonResult GetDeviceFolders(string deviceGuid)
        {
            try
            {
                OperatorModel user = CommanUtilities.Provider.GetCurrent();
                if (user == null || user.user_id <= 0)
                {
                    return Json(new { status = "error", message = "Unauthorized." }, JsonRequestBehavior.AllowGet);
                }

                if (string.IsNullOrWhiteSpace(deviceGuid))
                {
                    return Json(new { status = "error", message = "Device GUID is required." }, JsonRequestBehavior.AllowGet);
                }

                string status, msg;
                List<SharedFolderModel> folders = FolderManager.GetDeviceFolders(user.user_id, deviceGuid, out status, out msg);

                return Json(new
                {
                    status = "success",
                    message = msg,
                    data = folders
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Retrieves the user permissions matrix for a shared folder.
        /// </summary>
        [HttpGet]
        public JsonResult GetFolderPermissions(long folderId)
        {
            try
            {
                OperatorModel user = CommanUtilities.Provider.GetCurrent();
                if (user == null || user.user_id <= 0)
                {
                    return Json(new { status = "error", message = "Unauthorized." }, JsonRequestBehavior.AllowGet);
                }

                if (folderId <= 0)
                {
                    return Json(new { status = "error", message = "Invalid Folder ID." }, JsonRequestBehavior.AllowGet);
                }

                string status, msg;
                List<FolderPermissionModel> perms = FolderManager.GetFolderPermissions(user.user_id, folderId, out status, out msg);

                return Json(new
                {
                    status = "success",
                    message = msg,
                    data = perms
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Upserts permissions for a user on a shared folder.
        /// </summary>
        [HttpPost]
        public JsonResult UpdatePermission(UpdatePermissionRequest req)
        {
            try
            {
                OperatorModel user = CommanUtilities.Provider.GetCurrent();
                if (user == null || user.user_id <= 0)
                {
                    return Json(new { status = "error", message = "Unauthorized." }, JsonRequestBehavior.AllowGet);
                }

                if (req == null || req.folder_id <= 0 || req.target_user_id <= 0)
                {
                    return Json(new { status = "error", message = "Folder ID and User ID are required." }, JsonRequestBehavior.AllowGet);
                }

                string status, msg;
                bool success = FolderManager.UpdatePermission(
                    user.user_id, 
                    req.folder_id, 
                    req.target_user_id, 
                    req.can_view, 
                    req.can_download, 
                    req.can_upload, 
                    req.can_delete, 
                    out status, 
                    out msg
                );

                if (success)
                {
                    return Json(new { status = "success", message = "Permission updated successfully." }, JsonRequestBehavior.AllowGet);
                }

                return Json(new { status = "error", message = msg ?? "Failed to update permission." }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }
/// <summary>
        /// WHAT: Reads directory contents of a remote shared folder on demand.
        /// REASON: Step 4 Remote File Browser - Verifies user authentication,
        /// validates CanView permission in SQL Server, dispatches command to Agent via SignalR,
        /// and returns structured file and folder metadata.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> GetDirectoryContents(GetDirectoryContentsRequest req)
        {
            try
            {
                OperatorModel user = CommanUtilities.Provider.GetCurrent();
                if (user == null || user.user_id <= 0)
                {
                    return Json(new { status = "error", message = "Unauthorized. Please login again." }, JsonRequestBehavior.AllowGet);
                }

                if (req == null || string.IsNullOrWhiteSpace(req.folder_guid))
                {
                    return Json(new { status = "error", message = "Folder GUID is required." }, JsonRequestBehavior.AllowGet);
                }

                if (!Guid.TryParse(req.folder_guid, out Guid folderGuid))
                {
                    return Json(new { status = "error", message = "Invalid Folder GUID format." }, JsonRequestBehavior.AllowGet);
                }

                // 1. Verify User Access & CanView Permission in SQL Server
                string status, msg;
                FolderBrowseViewModel folder = FolderManager.VerifyFolderViewPermission(user.user_id, folderGuid, out status, out msg);

                if (status != "success" || folder == null)
                {
                    return Json(new { status = "error", message = msg ?? "Access Denied: You do not have permission to view this shared folder." }, JsonRequestBehavior.AllowGet);
                }

                // 2. Sanitize and validate relative path
                string safeRelativePath = (req.relative_path ?? "").Trim().TrimStart('/', '\\').Replace('/', System.IO.Path.DirectorySeparatorChar);
                if (safeRelativePath.Contains(".."))
                {
                    return Json(new { status = "error", message = "Access Denied: Path traversal sequences ('..') are prohibited." }, JsonRequestBehavior.AllowGet);
                }

                // 3. Proactively check if device is online
                if (!folder.is_device_online)
                {
                    return Json(new
                    {
                        status = "error",
                        is_device_offline = true,
                        message = $"'{folder.device_name}' is currently offline. Please start the File Access Agent on the computer to browse files."
                    }, JsonRequestBehavior.AllowGet);
                }

                // 4. Dispatch directory read command to Agent via SignalR and await response
                string requestId = "DIR_" + Guid.NewGuid().ToString("N").Substring(0, 10);
                DirectoryListingResult result = await DeviceHub.RequestDirectoryListingAsync(
                    folder.device_guid, 
                    requestId, 
                    folder.local_path, 
                    safeRelativePath, 
                    10
                );

                if (!result.is_success)
                {
                    return Json(new { status = "error", message = result.error_message }, JsonRequestBehavior.AllowGet);
                }

                result.root_folder_name = folder.folder_name;

                return Json(new
                {
                    status = "success",
                    message = "Directory loaded successfully.",
                    folder_info = new
                    {
                        folder_id = folder.folder_id,
                        folder_guid = folder.folder_guid,
                        folder_name = folder.folder_name,
                        device_name = folder.device_name,
                        device_guid = folder.device_guid,
                        is_owner = folder.is_owner,
                        can_view = folder.can_view,
                        can_download = folder.can_download,
                        can_upload = folder.can_upload,
                        can_delete = folder.can_delete
                    },
                    data = result
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = "Server error while reading directory: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// WHAT: Securely downloads a single file from a remote shared folder via the PC Agent.
        /// REASON: Step 5 Secure File Download — verifies can_download permission, dispatches chunked
        /// file transfer via SignalR to the PC Agent, and streams the assembled file to the browser.
        /// Security: Path traversal blocked, permission enforced server-side, file size limited to 500MB.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> DownloadFile(string folderGuid, string relativePath, bool inline = false)
        {
            try
            {
                OperatorModel user = CommanUtilities.Provider.GetCurrent();
                if (user == null || user.user_id <= 0)
                {
                    return new HttpStatusCodeResult(401, "Unauthorized. Please login again.");
                }

                if (string.IsNullOrWhiteSpace(folderGuid))
                {
                    return new HttpStatusCodeResult(400, "Folder GUID is required.");
                }

                if (!Guid.TryParse(folderGuid, out Guid parsedGuid))
                {
                    return new HttpStatusCodeResult(400, "Invalid Folder GUID format.");
                }

                // 1. Block path traversal at controller level
                string safeRelPath = (relativePath ?? "").Trim().TrimStart('/', '\\').Replace('/', System.IO.Path.DirectorySeparatorChar);
                if (safeRelPath.Contains("..") || string.IsNullOrWhiteSpace(safeRelPath))
                {
                    return new HttpStatusCodeResult(400, "Access Denied: Invalid or missing file path.");
                }

                // 2. Verify can_download permission in SQL Server
                string status, msg;
                FolderBrowseViewModel folder = FolderManager.VerifyFolderViewPermission(user.user_id, parsedGuid, out status, out msg);

                if (status != "success" || folder == null)
                {
                    return new HttpStatusCodeResult(403, msg ?? "Access Denied: You do not have permission to access this shared folder.");
                }

                if (!folder.can_download)
                {
                    return new HttpStatusCodeResult(403, "Access Denied: You do not have download permission for this shared folder.");
                }

                // 3. Check device is online
                if (!folder.is_device_online)
                {
                    return new HttpStatusCodeResult(503, $"'{folder.device_name}' is currently offline. Please start the File Access Agent on the computer.");
                }

                // 4. Dispatch chunked file transfer to PC Agent via SignalR
                string requestId = "FILE_" + Guid.NewGuid().ToString("N").Substring(0, 12);
                FileTransferResult result = await DeviceHub.RequestFileTransferAsync(
                    folder.device_guid,
                    requestId,
                    folder.local_path,
                    safeRelPath,
                    120 // 120-second timeout for large files
                );

                if (!result.is_success)
                {
                    return new HttpStatusCodeResult(500, result.error_message ?? "File transfer failed.");
                }

                // 5. Stream assembled file to browser with original filename and content-type
                string downloadName = result.file_name ?? System.IO.Path.GetFileName(safeRelPath);
                string mimeType = result.content_type ?? "application/octet-stream";
                if (string.IsNullOrWhiteSpace(mimeType) || mimeType == "application/octet-stream")
                {
                    try
                    {
                        mimeType = System.Web.MimeMapping.GetMimeMapping(downloadName);
                    }
                    catch { }
                }

                string disposition = inline ? "inline" : "attachment";
                Response.AddHeader("Content-Disposition", $"{disposition}; filename=\"{downloadName}\"");
                return File(result.data, mimeType);
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Server error during file download: " + ex.Message);
            }
        }
    
        // In-memory token registry for temporary file streaming between Server and Agent
        private static readonly ConcurrentDictionary<string, UploadStreamInfo> _pendingUploadStreams =
            new ConcurrentDictionary<string, UploadStreamInfo>();

        /// <summary>
        /// WHAT: Handles secure remote file upload from browser to PC Agent.
        /// REASON: Step 6 Secure Remote File Upload - verifies authentication and can_upload permission,
        /// validates file extension allowlist, file size limit, and sanitizes filename. Streams incoming file
        /// to an isolated staging file on server in 64KB chunks (zero full-file RAM buffering), then coordinates
        /// with the connected PC Agent via SignalR to stream, write temporary file, and atomically move it to target folder.
        /// </summary>
        [HttpPost]
        public async Task<ActionResult> UploadFile()
        {
            string tempFilePath = null;
            string uploadToken = null;

            try
            {
                // 1. Authenticate user
                OperatorModel user = CommanUtilities.Provider.GetCurrent();
                if (user == null || user.user_id <= 0)
                {
                    Response.StatusCode = 401;
                    return Json(new { status = "error", message = "Unauthorized. Please login again." }, JsonRequestBehavior.AllowGet);
                }

                // 2. Validate form inputs
                string folderGuid = Request.Form["folderGuid"];
                string relativePath = Request.Form["relativePath"];
                string conflictAction = (Request.Form["conflictAction"] ?? "autorename").Trim().ToLowerInvariant(); // "autorename" or "replace"
                if (conflictAction != "replace") conflictAction = "autorename";

                if (string.IsNullOrWhiteSpace(folderGuid))
                {
                    Response.StatusCode = 400;
                    return Json(new { status = "error", message = "Folder GUID is required." }, JsonRequestBehavior.AllowGet);
                }

                if (!Guid.TryParse(folderGuid, out Guid parsedGuid))
                {
                    Response.StatusCode = 400;
                    return Json(new { status = "error", message = "Invalid Folder GUID format." }, JsonRequestBehavior.AllowGet);
                }

                // 3. Validate uploaded file presence
                if (Request.Files.Count == 0 || Request.Files[0] == null || Request.Files[0].ContentLength == 0)
                {
                    Response.StatusCode = 400;
                    return Json(new { status = "error", message = "Please select a valid file to upload." }, JsonRequestBehavior.AllowGet);
                }

                HttpPostedFileBase uploadedFile = Request.Files[0];

                // 4. Sanitize and validate filename (block traversal in filename itself)
                string rawFileName = System.IO.Path.GetFileName(uploadedFile.FileName);
                if (string.IsNullOrWhiteSpace(rawFileName) || rawFileName.IndexOfAny(System.IO.Path.GetInvalidFileNameChars()) >= 0)
                {
                    Response.StatusCode = 400;
                    return Json(new { status = "error", message = "Invalid file name. It contains illegal characters." }, JsonRequestBehavior.AllowGet);
                }

                // 5. Validate file extension against configurable allowlist
                string ext = System.IO.Path.GetExtension(rawFileName).ToLowerInvariant();
                string allowedExtsConfig = ConfigurationManager.AppSettings["AllowedUploadExtensions"] 
                    ?? ".pdf,.doc,.docx,.xls,.xlsx,.csv,.txt,.jpg,.jpeg,.png,.gif,.zip,.mp4,.mp3,.json,.xml";
                var allowedList = new HashSet<string>(allowedExtsConfig.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries), StringComparer.OrdinalIgnoreCase);

                if (string.IsNullOrEmpty(ext) || !allowedList.Contains(ext))
                {
                    Response.StatusCode = 400;
                    return Json(new { status = "error", message = $"This file type ('{ext}') is not allowed for upload." }, JsonRequestBehavior.AllowGet);
                }

                // 6. Validate file size against configurable limit (default 100MB)
                int maxUploadSizeMB = 100;
                if (int.TryParse(ConfigurationManager.AppSettings["MaxUploadSizeMB"], out int configuredMB) && configuredMB > 0)
                {
                    maxUploadSizeMB = configuredMB;
                }
                long maxUploadSizeBytes = (long)maxUploadSizeMB * 1024 * 1024;

                if (uploadedFile.ContentLength > maxUploadSizeBytes)
                {
                    Response.StatusCode = 413;
                    return Json(new { status = "error", message = $"The file exceeds the maximum allowed upload size of {maxUploadSizeMB} MB." }, JsonRequestBehavior.AllowGet);
                }

                // 7. Validate path traversal in relative path
                string safeRelPath = (relativePath ?? "").Trim().TrimStart('/', '\\').Replace('/', System.IO.Path.DirectorySeparatorChar);
                if (safeRelPath.Contains(".."))
                {
                    Response.StatusCode = 400;
                    return Json(new { status = "error", message = "Access Denied: Path traversal sequences ('..') are prohibited." }, JsonRequestBehavior.AllowGet);
                }

                // 8. Verify user permission & CanUpload in SQL Server
                string status, msg;
                FolderBrowseViewModel folder = FolderManager.VerifyFolderViewPermission(user.user_id, parsedGuid, out status, out msg);

                if (status != "success" || folder == null)
                {
                    Response.StatusCode = 403;
                    return Json(new { status = "error", message = msg ?? "Access Denied: You do not have permission to access this shared folder." }, JsonRequestBehavior.AllowGet);
                }

                if (!folder.can_upload)
                {
                    Response.StatusCode = 403;
                    return Json(new { status = "error", message = "You do not have permission to upload files here." }, JsonRequestBehavior.AllowGet);
                }

                // 9. Check device online status
                if (!folder.is_device_online)
                {
                    Response.StatusCode = 503;
                    return Json(new { status = "error", message = $"The device '{folder.device_name}' is currently offline. Please ensure the File Access Agent is running." }, JsonRequestBehavior.AllowGet);
                }

                // 10. Stream incoming upload to short-lived isolated staging file in App_Data/TempUploads
                string tempUploadsDir = Server.MapPath("~/App_Data/TempUploads");
                if (!System.IO.Directory.Exists(tempUploadsDir))
                {
                    System.IO.Directory.CreateDirectory(tempUploadsDir);
                }

                uploadToken = Guid.NewGuid().ToString("N");
                tempFilePath = System.IO.Path.Combine(tempUploadsDir, uploadToken + ".tmp");

                using (var fs = new System.IO.FileStream(tempFilePath, System.IO.FileMode.Create, System.IO.FileAccess.Write, System.IO.FileShare.None, 64 * 1024, true))
                {
                    await uploadedFile.InputStream.CopyToAsync(fs, 64 * 1024);
                }

                // Register upload stream token for agent HTTP streaming retrieval
                var streamInfo = new UploadStreamInfo
                {
                    UploadToken = uploadToken,
                    TempFilePath = tempFilePath,
                    DeviceGuid = folder.device_guid.ToString(),
                    FileName = rawFileName,
                    CreatedTime = DateTime.UtcNow
                };
                _pendingUploadStreams[uploadToken] = streamInfo;

                // 11. Dispatch upload command to PC Agent via SignalR and await atomic file write
                string requestId = "UPL_" + Guid.NewGuid().ToString("N").Substring(0, 12);
                UploadTransferResult result = await DeviceHub.RequestUploadTransferAsync(
                    folder.device_guid,
                    requestId,
                    folder.local_path,
                    safeRelPath,
                    rawFileName,
                    conflictAction,
                    uploadToken,
                    uploadedFile.ContentLength,
                    180 // 180 seconds timeout for up to 100MB transfer
                );

                if (!result.is_success)
                {
                    Response.StatusCode = 500;
                    return Json(new { status = "error", message = result.error_message ?? "The upload could not be completed. Please try again." }, JsonRequestBehavior.AllowGet);
                }

                // 12. Record audit log
                try
                {
                    ActivityLogHelper.InsertActivityLog(
                        user.user_id,
                        1,
                        "/FolderAPI/UploadFile",
                        "SharedFolder",
                        $"Uploaded '{result.final_file_name}' ({result.file_size_bytes} bytes) to {folder.folder_name}/{safeRelPath} on device {folder.device_name}"
                    );
                }
                catch (Exception logEx)
                {
                    System.Diagnostics.Debug.WriteLine("[UploadFile] Activity log failed: " + logEx.Message);
                }

                return Json(new
                {
                    status = "success",
                    message = "File uploaded successfully.",
                    fileName = result.final_file_name,
                    sizeBytes = result.file_size_bytes
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return Json(new { status = "error", message = "The upload could not be completed: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
            finally
            {
                // Clean up server staging file immediately
                if (!string.IsNullOrEmpty(uploadToken))
                {
                    _pendingUploadStreams.TryRemove(uploadToken, out _);
                }
                if (!string.IsNullOrEmpty(tempFilePath) && System.IO.File.Exists(tempFilePath))
                {
                    try { System.IO.File.Delete(tempFilePath); } catch { }
                }
            }
        }

        /// <summary>
        /// WHAT: Streams staged upload data to the PC Agent over secure HTTP.
        /// REASON: Step 6 Secure Remote File Upload - PC Agent calls this endpoint with a one-time
        /// token to stream the file data directly into its local FileStream (no SignalR Base64 bloat).
        /// </summary>
        [HttpGet]
        public ActionResult GetUploadStream(string uploadToken, string deviceGuid)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(uploadToken) || string.IsNullOrWhiteSpace(deviceGuid))
                {
                    return new HttpStatusCodeResult(400, "Upload token and Device GUID are required.");
                }

                if (!_pendingUploadStreams.TryGetValue(uploadToken, out var info))
                {
                    return new HttpStatusCodeResult(404, "Upload stream not found or expired.");
                }

                if (!string.Equals(info.DeviceGuid, deviceGuid, StringComparison.OrdinalIgnoreCase))
                {
                    return new HttpStatusCodeResult(403, "Access Denied: Device GUID mismatch.");
                }

                if (!System.IO.File.Exists(info.TempFilePath))
                {
                    return new HttpStatusCodeResult(404, "Staged upload file not found.");
                }

                var fs = new System.IO.FileStream(info.TempFilePath, System.IO.FileMode.Open, System.IO.FileAccess.Read, System.IO.FileShare.Read, 64 * 1024, true);
                return new FileStreamResult(fs, "application/octet-stream");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Error streaming upload: " + ex.Message);
            }
        }

    }

    /// <summary>
    /// Metadata for a short-lived staging file being streamed to the PC Agent.
    /// </summary>
    public class UploadStreamInfo
    {
        public string UploadToken { get; set; }
        public string TempFilePath { get; set; }
        public string DeviceGuid { get; set; }
        public string FileName { get; set; }
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    }

}
