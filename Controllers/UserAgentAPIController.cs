using System;
using System.IO;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using PCAccess.BAL;
using PCAccess.Hubs;
using PCAccess.Models;

namespace PCAccess.Controllers
{
    /// <summary>
    /// API Controller for UserLoginAgent Desktop Client.
    /// WHAT: Exposes endpoints for user/employee authentication and folder synchronization.
    /// REASON: Follows the established project pattern (DeviceAPIController / AccountAPIController),
    /// returning standardized JSON: { status = "success"|"error", message = "...", data = ... }
    /// </summary>
    public class UserAgentAPIController : Controller
    {
        /// <summary>
        /// Direct authentication endpoint for UserLoginAgent.
        /// WHAT: Authenticates user by username/email and password, returning accessible folders.
        /// REASON: Allows users, employees, and admins to log in directly from the desktop agent
        /// without opening a web browser.
        /// </summary>
        [HttpPost]
        public JsonResult Login(UserAgentLoginRequest req)
        {
            try
            {
                if (req == null || string.IsNullOrWhiteSpace(req.username_or_email) || string.IsNullOrWhiteSpace(req.password))
                {
                    return Json(new
                    {
                        status = "error",
                        message = "Username/Email and Password are required."
                    }, JsonRequestBehavior.AllowGet);
                }

                string status, msg;
                UserAgentLoginResponse response = UserAgentManager.Authenticate(
                    req.username_or_email.Trim(),
                    req.password,
                    req.client_machine,
                    out status,
                    out msg
                );

                if (status == "success" && response != null)
                {
                    return Json(new
                    {
                        status = "success",
                        message = "Login successful.",
                        data = response
                    }, JsonRequestBehavior.AllowGet);
                }

                return Json(new
                {
                    status = "error",
                    message = !string.IsNullOrWhiteSpace(msg) ? msg : "Invalid credentials."
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    status = "error",
                    message = "Server error during authentication: " + ex.Message
                }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Lightweight connection test endpoint.
        /// WHAT: Verifies server connectivity from the UserLoginAgent before prompting for credentials.
        /// </summary>
        [HttpGet]
        public JsonResult Ping()
        {
            return Json(new
            {
                status = "success",
                message = "PCAccess Server is online and ready.",
                timestamp = DateTime.UtcNow
            }, JsonRequestBehavior.AllowGet);
        }

        /// <summary>
        /// Retrieves directory listing for UserLoginAgent client sessions.
        /// WHAT: Verifies user permissions and asks the host FileAccessAgent via SignalR for files & folders.
        /// REASON: Task 3.2 - Enables desktop client agents to browse remote folders directly.
        /// </summary>
        [HttpPost]
        public async Task<JsonResult> GetFolderContents(UserAgentFolderContentsRequest req)
        {
            try
            {
                if (req == null || req.user_id <= 0 || string.IsNullOrWhiteSpace(req.folder_guid))
                {
                    return Json(new { status = "error", message = "User ID and Folder GUID are required." }, JsonRequestBehavior.AllowGet);
                }

                if (!Guid.TryParse(req.folder_guid, out Guid folderGuid))
                {
                    return Json(new { status = "error", message = "Invalid Folder GUID format." }, JsonRequestBehavior.AllowGet);
                }

                // Verify permissions in SQL Server
                string status, msg;
                FolderBrowseViewModel folder = FolderManager.VerifyFolderViewPermission(req.user_id, folderGuid, out status, out msg);
                if (status != "success" || folder == null)
                {
                    return Json(new { status = "error", message = msg ?? "Access Denied: You do not have permission to view this shared folder." }, JsonRequestBehavior.AllowGet);
                }

                // Check device online state
                if (!folder.is_device_online)
                {
                    return Json(new
                    {
                        status = "error",
                        is_device_offline = true,
                        message = $"'{folder.device_name}' is currently offline. Please start FileAccessAgent on that computer."
                    }, JsonRequestBehavior.AllowGet);
                }

                string safeRelativePath = (req.relative_path ?? "").Trim().TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
                if (safeRelativePath.Contains(".."))
                {
                    return Json(new { status = "error", message = "Access Denied: Path traversal sequences ('..') are prohibited." }, JsonRequestBehavior.AllowGet);
                }

                string requestId = "UA_DIR_" + Guid.NewGuid().ToString("N").Substring(0, 10);
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
                    data = result
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { status = "error", message = "Error loading directory: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        /// <summary>
        /// Downloads a file for UserLoginAgent client session.
        /// WHAT: Verifies download permission and streams the remote file directly to the client agent.
        /// REASON: Task 3.2 - Enables client agent to synchronize or download remote files to local U:\ drive.
        /// </summary>
        [HttpGet]
        public async Task<ActionResult> DownloadFile(long userId, string folderGuid, string relativePath)
        {
            try
            {
                if (userId <= 0 || string.IsNullOrWhiteSpace(folderGuid) || string.IsNullOrWhiteSpace(relativePath))
                {
                    return new HttpStatusCodeResult(400, "Missing required parameters (userId, folderGuid, relativePath).");
                }

                if (!Guid.TryParse(folderGuid, out Guid parsedGuid))
                {
                    return new HttpStatusCodeResult(400, "Invalid Folder GUID format.");
                }

                string safeRelPath = relativePath.Trim().TrimStart('/', '\\').Replace('/', Path.DirectorySeparatorChar);
                if (safeRelPath.Contains(".."))
                {
                    return new HttpStatusCodeResult(400, "Path traversal sequence blocked.");
                }

                string status, msg;
                FolderBrowseViewModel folder = FolderManager.VerifyFolderViewPermission(userId, parsedGuid, out status, out msg);
                if (status != "success" || folder == null || !folder.can_download)
                {
                    return new HttpStatusCodeResult(403, "Access Denied: Download permission required.");
                }

                if (!folder.is_device_online)
                {
                    return new HttpStatusCodeResult(503, $"'{folder.device_name}' is offline.");
                }

                string requestId = "UA_FILE_" + Guid.NewGuid().ToString("N").Substring(0, 12);
                string downloadToken = Guid.NewGuid().ToString("N");

                string tempDownloadsDir = Server.MapPath("~/App_Data/TempDownloads");
                if (!Directory.Exists(tempDownloadsDir))
                {
                    Directory.CreateDirectory(tempDownloadsDir);
                }
                string stagingFilePath = Path.Combine(tempDownloadsDir, downloadToken + ".tmp");

                FileTransferResult result;
                try
                {
                    result = await DeviceHub.RequestDownloadStreamAsync(
                        folder.device_guid,
                        requestId,
                        downloadToken,
                        folder.local_path,
                        safeRelPath,
                        120
                    );
                }
                catch
                {
                    result = await DeviceHub.RequestFileTransferAsync(
                        folder.device_guid,
                        requestId,
                        folder.local_path,
                        safeRelPath,
                        120
                    );
                }

                if (!result.is_success)
                {
                    try { if (System.IO.File.Exists(stagingFilePath)) System.IO.File.Delete(stagingFilePath); } catch { }
                    return new HttpStatusCodeResult(500, result.error_message ?? "File transfer failed.");
                }

                string downloadName = result.file_name ?? Path.GetFileName(safeRelPath);
                string mimeType = result.content_type ?? "application/octet-stream";
                try { mimeType = MimeMapping.GetMimeMapping(downloadName); } catch { }

                Response.AddHeader("Content-Disposition", $"attachment; filename=\"{downloadName}\"");

                if (System.IO.File.Exists(stagingFilePath) && new FileInfo(stagingFilePath).Length > 0)
                {
                    var fileStream = new FileStream(stagingFilePath, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete, 64 * 1024, true);
                    return new FileStreamResult(fileStream, mimeType);
                }
                else if (result.data != null && result.data.Length > 0)
                {
                    return File(result.data, mimeType);
                }

                return new HttpStatusCodeResult(500, "File content is empty.");
            }
            catch (Exception ex)
            {
                return new HttpStatusCodeResult(500, "Download error: " + ex.Message);
            }
        }
    }
}
