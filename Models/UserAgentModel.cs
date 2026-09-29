using System;
using System.Collections.Generic;

namespace PCAccess.Models
{
    /// <summary>
    /// Request model for UserLoginAgent desktop client authentication.
    /// WHAT: Holds login credentials entered by User/Employee/Admin in the client agent.
    /// REASON: Enables browser-free direct authentication via REST API.
    /// </summary>
    public class UserAgentLoginRequest
    {
        public string username_or_email { get; set; }
        public string password { get; set; }
        public string client_machine { get; set; }
    }

    /// <summary>
    /// Response model for UserLoginAgent desktop client authentication.
    /// WHAT: Returns authenticated user profile, session token, and accessible shared folders.
    /// REASON: Provides client agent with everything needed to mount and manage the Remote Drive.
    /// </summary>
    public class UserAgentLoginResponse
    {
        public long user_id { get; set; }
        public string user_name { get; set; }
        public string name { get; set; }
        public string email { get; set; }
        public int user_type_id { get; set; }
        public string role_name { get; set; }
        public string session_token { get; set; }
        public List<UserAgentFolderDto> folders { get; set; } = new List<UserAgentFolderDto>();
    }

    /// <summary>
    /// Shared folder DTO specific to client agent display and mounting.
    /// WHAT: Encapsulates folder identity, host workstation info, online state, and permission flags.
    /// REASON: Lightweight structure serialized to JSON for UserLoginAgent.
    /// </summary>
    public class UserAgentFolderDto
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
    }
}
