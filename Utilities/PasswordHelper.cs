using System.Security.Cryptography;

namespace IPOClient.Utilities
{
    public static class PasswordHelper
    {
        /// <summary>
        /// Hash a password using PBKDF2
        /// </summary>
        public static string HashPassword(string password)
        {
            using (var rng = new Rfc2898DeriveBytes(password, 16, 10000, HashAlgorithmName.SHA256))
            {
                string hashString = Convert.ToBase64String(rng.GetBytes(20));
                string saltString = Convert.ToBase64String(rng.Salt);
                return $"{hashString}:{saltString}";
            }
        }

        /// <summary>
        /// Verify a password against its hash
        /// </summary>
        public static bool VerifyPassword(string password, string hash)
        { 
            try
            {
                var parts = hash.Split(':');
                if (parts.Length != 2)
                    return false;

                var hashBytes = Convert.FromBase64String(parts[0]);
                var saltBytes = Convert.FromBase64String(parts[1]);

                using (var rng = new Rfc2898DeriveBytes(password, saltBytes, 10000, HashAlgorithmName.SHA256))
                {
                    var computedHash = rng.GetBytes(20);
                    return hashBytes.SequenceEqual(computedHash);
                }
            }
            catch
            {
                return false;
            }
        }
    }
}
