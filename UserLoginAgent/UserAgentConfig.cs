using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
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

        /// <summary>
        /// Saved password storage.
        /// WHAT: Stores DPAPI-encrypted password string with 'dpapi:' prefix, or legacy plain Base64.
        /// REASON: Task 2.2 - Prevents plaintext credential leakage on local disk. Protected by Windows DPAPI.
        /// </summary>
        public string SavedPasswordBase64 { get; set; } = "";

        /// <summary>
        /// WHAT: Default drive letter for UserLoginAgent client sessions.
        /// REASON: Task 1.3 - Uses 'U:' ("User Remote Drive") instead of 'R:' to eliminate drive letter
        /// collision when both FileAccessAgent (R:) and UserLoginAgent (U:) run on the same computer.
        /// </summary>
        public string DriveLetter { get; set; } = "U:";
        public string DriveLabel { get; set; } = "User Remote Drive";
        public bool RememberCredentials { get; set; } = true;
        public DateTime LastLoginDate { get; set; } = DateTime.MinValue;

        [JsonIgnore]
        public bool HasSavedPassword => !string.IsNullOrWhiteSpace(SavedPasswordBase64);

        /// <summary>
        /// Retrieves decrypted password using Windows DPAPI (CurrentUser scope) with legacy Base64 fallback.
        /// WHAT: Unprotects ciphertext using OS-managed user keys. Auto-upgrades legacy Base64 to DPAPI.
        /// REASON: Task 2.2 - Eliminates insecure reversible Base64 password storage on Windows endpoints.
        /// </summary>
        public string GetSavedPassword()
        {
            if (string.IsNullOrWhiteSpace(SavedPasswordBase64))
                return "";

            try
            {
                if (SavedPasswordBase64.StartsWith("dpapi:", StringComparison.OrdinalIgnoreCase))
                {
                    string cipherBase64 = SavedPasswordBase64.Substring("dpapi:".Length);
                    byte[] cipherBytes = Convert.FromBase64String(cipherBase64);
                    byte[] plainBytes = ProtectedData.Unprotect(cipherBytes, null, DataProtectionScope.CurrentUser);
                    return Encoding.UTF8.GetString(plainBytes);
                }

                // Legacy Base64 fallback & transparent upgrade
                byte[] legacyBytes = Convert.FromBase64String(SavedPasswordBase64);
                string plain = Encoding.UTF8.GetString(legacyBytes);

                // Re-encrypt with DPAPI and persist immediately
                SetSavedPassword(plain);
                Save();

                return plain;
            }
            catch (Exception ex)
            {
                UserAgentLogger.Warn($"Failed to decrypt saved password: {ex.Message}");
                return "";
            }
        }

        /// <summary>
        /// Protects and stores the plain password using Windows DPAPI (CurrentUser scope).
        /// </summary>
        public void SetSavedPassword(string plainPassword)
        {
            if (string.IsNullOrEmpty(plainPassword))
            {
                SavedPasswordBase64 = "";
                return;
            }

            try
            {
                byte[] plainBytes = Encoding.UTF8.GetBytes(plainPassword);
                byte[] cipherBytes = ProtectedData.Protect(plainBytes, null, DataProtectionScope.CurrentUser);
                SavedPasswordBase64 = "dpapi:" + Convert.ToBase64String(cipherBytes);
            }
            catch (Exception ex)
            {
                UserAgentLogger.Warn($"DPAPI encryption failed, falling back: {ex.Message}");
                SavedPasswordBase64 = Convert.ToBase64String(Encoding.UTF8.GetBytes(plainPassword));
            }
        }

        public void ClearSavedPassword()
        {
            SavedPasswordBase64 = "";
        }

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
