using System;

namespace FileAccessAgent
{
    /// <summary>
    /// Data Transfer Object representing a directory item (file or subfolder).
    /// WHAT: Lightweight file metadata sent from the local PC Agent to the ASP.NET server.
    /// REASON: Shields server and browser from OS-specific filesystem handles; never sends raw file contents.
    /// </summary>
    public class FileItemDto
    {
        public string name { get; set; } = "";
        public string type { get; set; } = "file";
        public bool is_folder { get; set; } = false;
        public long size_bytes { get; set; } = 0;
        public string size_formatted { get; set; } = "-";
        public string extension { get; set; } = "";
        public DateTime last_modified { get; set; } = DateTime.UtcNow;
        public string last_modified_formatted { get; set; } = "";
        public string relative_path { get; set; } = "";
        public string icon_type { get; set; } = "file";
    }
}
