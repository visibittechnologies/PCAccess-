using System;
using System.ComponentModel.DataAnnotations;

namespace PCAccess.Models
{
    /// <summary>
    /// Represents a registered remote computer/device in the File Access System.
    /// WHAT: Entity model mapping to database table tbl_device and stored procedure proc_device_manager.
    /// REASON: Follows existing PCAccess.Models POCO architecture with clear XML documentation.
    /// </summary>
    public class DeviceModel
    {
        public int id { get; set; }
        public Guid device_guid { get; set; }
        public string device_name { get; set; }
        public long user_id { get; set; }
        public string device_token { get; set; }
        public string status { get; set; } // "online" or "offline"
        public string connection_id { get; set; }
        public DateTime last_seen { get; set; }
        public DateTime created_at { get; set; }
        public int seconds_since_last_seen { get; set; }
        public bool is_active { get; set; }
        public int shared_folder_count { get; set; }

        /// <summary>
        /// WHAT: Formatted relative timestamp for display in UI.
        /// REASON: Provides user-friendly last seen labels matching Step 1 design specs.
        /// </summary>
        public string LastSeenFormatted
        {
            get
            {
                if (status == "online") return "Just now";
                if (seconds_since_last_seen < 60) return seconds_since_last_seen + "s ago";
                if (seconds_since_last_seen < 3600) return (seconds_since_last_seen / 60) + "m ago";
                if (seconds_since_last_seen < 86400) return (seconds_since_last_seen / 3600) + "h ago";
                return last_seen.ToString("dd MMM yyyy");
            }
        }
    }

    /// <summary>
    /// WHAT: Request payload sent by PC Agent on first-time pairing.
    /// REASON: Validates user pairing code and associates device with user account.
    /// </summary>
    public class DevicePairingRequestModel
    {
        [Required]
        public string pairing_code { get; set; }

        [Required]
        public Guid device_guid { get; set; }

        [Required]
        public string device_name { get; set; }
    }

    /// <summary>
    /// WHAT: Request payload sent by PC Agent on subsequent launches to verify credentials.
    /// REASON: Prevents unauthorized device connection attempts without valid token.
    /// </summary>
    public class DeviceVerifyRequestModel
    {
        [Required]
        public Guid device_guid { get; set; }

        [Required]
        public string device_token { get; set; }
    }
}
