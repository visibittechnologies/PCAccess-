using System;

namespace FileAccessAgent
{
    /// <summary>
    /// Data Transfer Object representing a shared folder configuration received from PCAccess Server.
    /// WHAT: Lightweight contract describing a registered shared directory on this device.
    /// REASON: Supplies RemoteDriveManager with folder names and actual local file paths to construct
    /// NTFS directory junctions inside the virtual "Remote Drive (R:)".
    /// </summary>
    public class AgentSharedFolderDto
    {
        public long folder_id { get; set; }
        public string folder_name { get; set; } = "";
        public string local_path { get; set; } = "";
        public string description { get; set; } = "";
    }
}
