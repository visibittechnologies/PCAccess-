using System;
using System.Collections.Generic;

namespace PCAccess.Models
{
    /// <summary>
    /// Represents a registered shared folder on a connected PC.
    /// WHAT: Entity model mapping tbl_shared_folder in SQL Server.
    /// REASON: Encapsulates folder metadata, parent device info, and permission counts.
    /// </summary>
    public class SharedFolderModel
    {
        public long folder_id { get; set; }
        public Guid folder_guid { get; set; }
        public int device_id { get; set; }
        public string device_name { get; set; }
        public Guid device_guid { get; set; }
        public string device_status { get; set; }
        public string folder_name { get; set; }
        public string local_path { get; set; }
        public string description { get; set; }
        public bool is_active { get; set; }
        public DateTime created_date { get; set; }
        public DateTime modified_date { get; set; }
        public int permission_count { get; set; }

        public string CreatedDateFormatted
        {
            get
            {
                var diff = DateTime.Now - created_date;
                if (diff.TotalMinutes < 1) return "Just now";
                if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
                if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
                return created_date.ToString("dd MMM yyyy");
            }
        }
    }

    /// <summary>
    /// Represents a user's access rights on a shared folder.
    /// WHAT: Entity model mapping tbl_shared_folder_permission joined with tbl_user.
    /// REASON: Granular permission foundation for View, Download, Upload, and Delete.
    /// </summary>
    public class FolderPermissionModel
    {
        public long permission_id { get; set; }
        public long folder_id { get; set; }
        public long user_id { get; set; }
        public string user_name { get; set; }
        public string user_display_name { get; set; }
        public string email { get; set; }
        public bool can_view { get; set; }
        public bool can_download { get; set; }
        public bool can_upload { get; set; }
        public bool can_delete { get; set; }
        public bool is_owner { get; set; }
    }

    /// <summary>
    /// DTO for saving or updating a shared folder.
    /// </summary>
    public class SaveSharedFolderRequest
    {
        public long? folder_id { get; set; }
        public string device_guid { get; set; }
        public string folder_name { get; set; }
        public string local_path { get; set; }
        public string description { get; set; }
        public bool? is_active { get; set; }
    }

    /// <summary>
    /// DTO for updating access permissions for a user on a folder.
    /// </summary>
    public class UpdatePermissionRequest
    {
        public long folder_id { get; set; }
        public long target_user_id { get; set; }
        public bool can_view { get; set; }
        public bool can_download { get; set; }
        public bool can_upload { get; set; }
        public bool can_delete { get; set; }
    }

    /// <summary>
    /// Represents an individual directory item (folder or file) within a remote shared folder.
    /// WHAT: DTO received from the PC Agent via SignalR and returned to the browser.
    /// </summary>
    public class FileItemDto
    {
        public string name { get; set; }
        public string type { get; set; }
        public bool is_folder { get; set; }
        public long size_bytes { get; set; }
        public string size_formatted { get; set; }
        public string extension { get; set; }
        public DateTime last_modified { get; set; }
        public string last_modified_formatted { get; set; }
        public string relative_path { get; set; }
        public string icon_type { get; set; }
    }

    /// <summary>
    /// Encapsulates the complete directory listing result from the PC Agent.
    /// </summary>
    public class DirectoryListingResult
    {
        public string request_id { get; set; }
        public bool is_success { get; set; }
        public string error_message { get; set; }
        public string root_folder_name { get; set; }
        public string current_relative_path { get; set; }
        public List<FileItemDto> items { get; set; } = new List<FileItemDto>();
        public int total_folders { get; set; }
        public int total_files { get; set; }
        public long total_size_bytes { get; set; }
    }

    /// <summary>
    /// Request payload for fetching directory contents.
    /// </summary>
    public class GetDirectoryContentsRequest
    {
        public string folder_guid { get; set; }
        public string relative_path { get; set; }
    }

    /// <summary>
    /// View Model passed to Views/admin/FileBrowser.cshtml.
    /// </summary>
    public class FolderBrowseViewModel
    {
        public long folder_id { get; set; }
        public Guid folder_guid { get; set; }
        public string folder_name { get; set; }
        public string local_path { get; set; }
        public string description { get; set; }
        public int device_id { get; set; }
        public Guid device_guid { get; set; }
        public string device_name { get; set; }
        public string device_status { get; set; }
        public bool is_device_online { get; set; }
        public bool is_owner { get; set; }
        public bool can_view { get; set; }
        public bool can_download { get; set; }
        public bool can_upload { get; set; }
        public bool can_delete { get; set; }
        public string initial_path { get; set; }
    }

    /// <summary>
    /// WHAT: Request payload for downloading a single file from a remote shared folder.
    /// REASON: Step 5 Secure File Download — carries folder identity and relative file path.
    /// </summary>
    public class DownloadFileRequest
    {
        public string folder_guid { get; set; }
        public string relative_path { get; set; }
    }

    /// <summary>
    /// WHAT: Result of a full chunked file transfer from the PC Agent.
    /// REASON: Step 5 Secure File Download — carries assembled file bytes, name, content type.
    /// </summary>
    public class FileTransferResult
    {
        public string request_id { get; set; }
        public bool is_success { get; set; }
        public string error_message { get; set; }
        public string file_name { get; set; }
        public string content_type { get; set; }
        public long file_size_bytes { get; set; }
        public byte[] data { get; set; }
    }
    /// <summary>
    /// WHAT: Result of a file upload transfer to the PC Agent.
    /// REASON: Step 6 Secure Remote File Upload - carries transfer status, error message, final file name, and file size.
    /// </summary>
    public class UploadTransferResult
    {
        public string request_id { get; set; }
        public bool is_success { get; set; }
        public string error_message { get; set; }
        public string final_file_name { get; set; }
        public long file_size_bytes { get; set; }
    }

    /// <summary>
    /// WHAT: Lightweight data contract for shared folder metadata delivered to FileAccessAgent.
    /// REASON: Enables FileAccessAgent to dynamically mount and populate local directory junctions under "Remote Drive (R:)".
    /// </summary>
    public class AgentSharedFolderDto
    {
        public long folder_id { get; set; }
        public string folder_name { get; set; }
        public string local_path { get; set; }
        public string description { get; set; }
    }
}

