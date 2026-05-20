using System;
using System.Security.Cryptography;
using System.Text;

namespace _05pmuch.Services
{
    public static class PasswordHasher
    {
        public static string GenerateSalt()
        {
            var salt = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            return Convert.ToBase64String(salt);
        }

        public static string ComputeHash(string password, string salt)
        {
            if (password == null) throw new ArgumentNullException(nameof(password));
            if (salt == null) throw new ArgumentNullException(nameof(salt));

            byte[] saltBytes;
            try
            {
                saltBytes = Convert.FromBase64String(salt);
            }
            catch (FormatException ex)
            {
                throw new ArgumentException("Salt is not a valid Base64 string.", nameof(salt), ex);
            }

            var combined = Encoding.UTF8.GetBytes(password);
            var salted = new byte[combined.Length + saltBytes.Length];
            Buffer.BlockCopy(combined, 0, salted, 0, combined.Length);
            Buffer.BlockCopy(saltBytes, 0, salted, combined.Length, saltBytes.Length);

            using (var sha = SHA256.Create())
            {
                var hash = sha.ComputeHash(salted);
                return Convert.ToBase64String(hash);
            }
        }
    }
}
