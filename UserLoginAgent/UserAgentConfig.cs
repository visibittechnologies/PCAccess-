using System;
using System.IO;
using Newtonsoft.Json;

namespace UserLoginAgent
{
    /// <summary>
    /// Configuration model for UserLoginAgent.
    /// WHAT: Stores server URL, username/email, drive settings, and saved session token.
    /// REASON: Allows seamless 1-click login without re-typing server URL or credentials every time.
    /// </summary>
    public class UserAgentConfig
    {
        public string ServerUrl { get; set; } = "https://remote.primaryportal.co.uk";
        public string UsernameOrEmail { get; set; } = "";
        public string SavedPasswordBase64 { get; set; } = "";
        public string DriveLetter { get; set; } = "R:";
        public string DriveLabel { get; set; } = "Remote Drive";
        public bool RememberCredentials { get; set; } = true;
        public DateTime LastLoginDate { get; set; } = DateTime.MinValue;

        private static string GetConfigFilePath()
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Combine(appDir, "user_agent_config.json");
        }

        public static UserAgentConfig Load()
        {
            try
            {
                string path = GetConfigFilePath();
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    var config = JsonConvert.DeserializeObject<UserAgentConfig>(json);
                    if (config != null) return config;
                }
            }
            catch (Exception ex)
            {
                UserAgentLogger.Warn($"Failed to load config: {ex.Message}");
            }

            return new UserAgentConfig();
        }

        public void Save()
        {
            try
            {
                string path = GetConfigFilePath();
                string json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(path, json);
            }
            catch (Exception ex)
            {
                UserAgentLogger.Warn($"Failed to save config: {ex.Message}");
            }
        }
    }
}
