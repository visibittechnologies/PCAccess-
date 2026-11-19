using System;
using System.Collections.Generic;

namespace UserLoginAgent
{
    public class UserAgentLoginRequest
    {
        public string username_or_email { get; set; } = "";
        public string password { get; set; } = "";
        public string client_machine { get; set; } = "";
    }

    public class UserAgentApiResponse<T>
    {
        public string status { get; set; } = "";
        public string message { get; set; } = "";
        public T data { get; set; }
    }

    public class UserAgentLoginResponse
    {
        public long user_id { get; set; }
        public string user_name { get; set; } = "";
        public string name { get; set; } = "";
        public string email { get; set; } = "";
        public int user_type_id { get; set; }
        public string role_name { get; set; } = "";
        public string session_token { get; set; } = "";
        public List<UserAgentFolderDto> folders { get; set; } = new List<UserAgentFolderDto>();
    }

    public class UserAgentFolderDto
    {
        public long folder_id { get; set; }
        public Guid folder_guid { get; set; }
        public string folder_name { get; set; } = "";
        public string local_path { get; set; } = "";
        public string description { get; set; } = "";
        public int device_id { get; set; }
        public Guid device_guid { get; set; }
        public string device_name { get; set; } = "";
        public string device_status { get; set; } = "";
        public bool is_device_online { get; set; }
        public bool is_owner { get; set; }
        public bool can_view { get; set; }
        public bool can_download { get; set; }
        public bool can_upload { get; set; }
        public bool can_delete { get; set; }
    }

    /// <summary>
    /// Request payload for UserLoginAgent to browse remote directory contents.
    /// </summary>
    public class UserAgentFolderContentsRequest
    {
        public long user_id { get; set; }
        public string session_token { get; set; } = "";
        public string folder_guid { get; set; } = "";
        public string relative_path { get; set; } = "";
    }

    /// <summary>
    /// Encapsulates the directory listing result from remote PC Agent.
    /// </summary>
    public class DirectoryListingResult
    {
        public string request_id { get; set; } = "";
        public bool is_success { get; set; }
        public string error_message { get; set; } = "";
        public string root_folder_name { get; set; } = "";
        public string current_relative_path { get; set; } = "";
        public List<FileItemDto> items { get; set; } = new List<FileItemDto>();
        public int total_folders { get; set; }
        public int total_files { get; set; }
        public long total_size_bytes { get; set; }
    }

    /// <summary>
    /// Represents an individual directory item (folder or file) within a remote shared folder.
    /// </summary>
    public class FileItemDto
    {
        public string name { get; set; } = "";
        public string type { get; set; } = "";
        public bool is_folder { get; set; }
        public long size_bytes { get; set; }
        public string size_formatted { get; set; } = "";
        public string extension { get; set; } = "";
        public DateTime last_modified { get; set; }
        public string last_modified_formatted { get; set; } = "";
        public string relative_path { get; set; } = "";
        public string icon_type { get; set; } = "";
    }
}
