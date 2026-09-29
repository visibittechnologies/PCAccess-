using System;
using System.IO;
using Newtonsoft.Json;

namespace FileAccessAgent
{
    /// <summary>
    /// Configuration model for the File Access Agent.
    /// WHAT: Stores local persistent machine identity, server endpoint, and authentication token.
    /// REASON: Eliminates hardcoded URLs or credentials and ensures stable DeviceGuid across reboots.
    /// </summary>
    public class AgentConfig
    {
        public string ServerUrl { get; set; } = "https://pcaccess.ourdemos.com";
        public Guid DeviceGuid { get; set; } = Guid.Empty;
        public string DeviceName { get; set; } = Environment.MachineName;
        public string DeviceToken { get; set; } = "";

        /// <summary>
        /// WHAT: Virtual / Mapped Drive configuration settings for Windows Explorer ("This PC").
        /// REASON: Allows the local agent to automatically mount drive (default R:) named "Remote Drive"
        /// showing all configured shared folders on connection, and unmounting on disconnect.
        /// </summary>
        public string DriveLetter { get; set; } = "R:";
        public string DriveLabel { get; set; } = "Remote Drive";
        public bool EnableRemoteDrive { get; set; } = true;

        [JsonIgnore]
        public bool IsPaired => DeviceGuid != Guid.Empty && !string.IsNullOrWhiteSpace(DeviceToken);

        private static string ConfigFilePath => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "agent_config.json");

        /// <summary>
        /// WHAT: Loads persistent configuration from agent_config.json if present.
        /// REASON: Restores existing device identity without asking the user for pairing code again.
        /// </summary>
        public static AgentConfig Load()
        {
            try
            {
                if (File.Exists(ConfigFilePath))
                {
                    string json = File.ReadAllText(ConfigFilePath);
                    var cfg = JsonConvert.DeserializeObject<AgentConfig>(json);
                    if (cfg != null) return cfg;
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine("[WARN] Failed to read agent_config.json: " + ex.Message);
                Console.ResetColor();
            }

            return new AgentConfig();
        }

        /// <summary>
        /// WHAT: Saves persistent configuration to agent_config.json.
        /// REASON: Ensures DeviceGuid and DeviceToken persist across system restarts.
        /// </summary>
        public void Save()
        {
            try
            {
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(ConfigFilePath, json);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("[ERROR] Failed to save agent_config.json: " + ex.Message);
                Console.ResetColor();
            }
        }
    }
}
