namespace PCAccess.MyFun
{
    using System;
    using System.Data.SqlClient;
    using System.Security.Cryptography;
    using System.Text;
    using PCAccess.DAL;

    /// <summary>
    /// Cryptographic helper for secure password hashing and verification using PBKDF2 with HMAC-SHA256.
    /// WHAT: Hashes passwords with 10,000 iterations and a 128-bit cryptographically random salt.
    /// REASON: Task 2.1 - Transparent auto-upgrade. Eliminates plaintext passwords in tbl_user
    /// without breaking existing logins or requiring forced password resets.
    /// Format: $pbkdf2-sha256$iterations$saltBase64$hashBase64
    /// </summary>
    public static class PasswordSecurity
    {
        private const int SaltByteSize = 16;      // 128 bit salt
        private const int HashByteSize = 32;      // 256 bit hash
        private const int Pbkdf2Iterations = 10000;

        /// <summary>
        /// Creates a salted PBKDF2 hash of the given plain password.
        /// </summary>
        public static string HashPassword(string password)
        {
            if (string.IsNullOrEmpty(password)) return string.Empty;

            byte[] salt = new byte[SaltByteSize];
            using (var rng = new RNGCryptoServiceProvider())
            {
                rng.GetBytes(salt);
            }

            byte[] hash = ComputeHash(password, salt, Pbkdf2Iterations, HashByteSize);
            return $"$pbkdf2-sha256${Pbkdf2Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        /// <summary>
        /// Verifies a plain password against a stored password.
        /// Supports both PBKDF2 hashes and legacy plaintext with auto-upgrade flag.
        /// </summary>
        public static bool VerifyPassword(string inputPassword, string storedPassword, out bool needsUpgrade)
        {
            needsUpgrade = false;
            if (string.IsNullOrEmpty(inputPassword) || string.IsNullOrEmpty(storedPassword))
            {
                return false;
            }

            // Check if stored password is in standard PBKDF2 modular format
            if (storedPassword.StartsWith("$pbkdf2-sha256$"))
            {
                string[] parts = storedPassword.Split('$');
                if (parts.Length == 5 && int.TryParse(parts[2], out int iterations))
                {
                    try
                    {
                        byte[] salt = Convert.FromBase64String(parts[3]);
                        byte[] storedHash = Convert.FromBase64String(parts[4]);
                        byte[] computedHash = ComputeHash(inputPassword, salt, iterations, storedHash.Length);

                        return SlowEquals(storedHash, computedHash);
                    }
                    catch
                    {
                        return false;
                    }
                }
            }

            // Legacy plaintext fallback check
            if (string.Equals(inputPassword, storedPassword, StringComparison.Ordinal))
            {
                needsUpgrade = true; // Signals BAL layer to upgrade to PBKDF2 hash
                return true;
            }

            return false;
        }

        /// <summary>
        /// Updates the stored password hash for a user in tbl_user upon successful verification.
        /// </summary>
        public static bool UpgradeUserPassword(long userId, string plainPassword)
        {
            try
            {
                string newHash = HashPassword(plainPassword);
                if (string.IsNullOrEmpty(newHash) || userId <= 0) return false;

                SqlParameter pUserId = new SqlParameter("@user_id", userId);
                SqlParameter pHash = new SqlParameter("@password", newHash);

                int affected = MySqlHelper.ExecuteNonQuery(
                    "UPDATE tbl_user SET [password] = @password, [modified_date] = GETDATE() WHERE [user_id] = @user_id",
                    pUserId,
                    pHash
                );

                return affected > 0;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("[PasswordSecurity] UpgradeUserPassword error: " + ex.Message);
                return false;
            }
        }

        private static byte[] ComputeHash(string password, byte[] salt, int iterations, int outputBytes)
        {
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations))
            {
                return pbkdf2.GetBytes(outputBytes);
            }
        }

        /// <summary>
        /// Constant-time comparison to prevent timing attacks.
        /// </summary>
        private static bool SlowEquals(byte[] a, byte[] b)
        {
            if (a == null || b == null || a.Length != b.Length) return false;

            uint diff = 0;
            for (int i = 0; i < a.Length; i++)
            {
                diff |= (uint)(a[i] ^ b[i]);
            }
            return diff == 0;
        }
    }
}
