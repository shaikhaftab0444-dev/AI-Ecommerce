using System;
using System.Security.Cryptography;
using System.Text;

namespace BrandsStore.Helpers
{
    public static class PasswordResetHelper
    {
        // Generate a 6-digit OTP
        public static string GenerateOtp()
        {
            Random random = new Random();
            return random.Next(100000, 999999).ToString();
        }

        // Hash OTP for secure storage
        public static string HashOtp(string otp)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(otp));
                return Convert.ToBase64String(bytes);
            }
        }

        // Verify OTP against hash
        public static bool VerifyOtp(string otp, string hash)
        {
            if (string.IsNullOrEmpty(otp) || string.IsNullOrEmpty(hash))
                return false;

            var otpHash = HashOtp(otp);
            return otpHash == hash;
        }

        // Generate a secure reset token
        public static string GenerateResetToken()
        {
            return Guid.NewGuid().ToString("N") + Guid.NewGuid().ToString("N");
        }

        // Check if timestamp has expired
        public static bool IsExpired(DateTime? expiryTime)
        {
            return !expiryTime.HasValue || expiryTime.Value < DateTime.UtcNow;
        }

        // Check if account is locked out
        public static bool IsLockedOut(DateTime? lockoutEnd)
        {
            return lockoutEnd.HasValue && lockoutEnd.Value > DateTime.UtcNow;
        }

        // Get remaining lockout minutes
        public static int GetRemainingLockoutMinutes(DateTime? lockoutEnd)
        {
            if (!lockoutEnd.HasValue) return 0;
            var remaining = (lockoutEnd.Value - DateTime.UtcNow).TotalMinutes;
            return Math.Max(0, (int)Math.Ceiling(remaining));
        }

        // Hash password with salt (PBKDF2)
        public static string HashPassword(string password)
        {
            // Generate a random salt
            byte[] salt = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }

            // Hash the password with PBKDF2
            using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
            {
                byte[] hash = pbkdf2.GetBytes(16);

                // Combine salt and hash: salt:hash (both base64 encoded)
                return Convert.ToBase64String(salt) + ":" + Convert.ToBase64String(hash);
            }
        }

        // Verify password against stored hash
        public static bool VerifyPassword(string password, string storedHash)
        {
            try
            {
                // Check if hash contains separator
                if (!storedHash.Contains(":"))
                {
                    // Old plain text password - direct comparison (for migration)
                    return storedHash == password;
                }

                // Split the stored hash into salt and hash
                var parts = storedHash.Split(':');
                if (parts.Length != 2)
                    return false;

                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] storedPasswordHash = Convert.FromBase64String(parts[1]);

                // Hash the input password with the same salt
                using (var pbkdf2 = new Rfc2898DeriveBytes(password, salt, 10000, HashAlgorithmName.SHA256))
                {
                    byte[] hash = pbkdf2.GetBytes(16);

                    // Compare the hashes
                    return CompareByteArrays(hash, storedPasswordHash);
                }
            }
            catch
            {
                return false;
            }
        }

        // Secure byte array comparison
        private static bool CompareByteArrays(byte[] a, byte[] b)
        {
            if (a.Length != b.Length)
                return false;

            int result = 0;
            for (int i = 0; i < a.Length; i++)
            {
                result |= a[i] ^ b[i];
            }

            return result == 0;
        }

        // Validate password strength
        public static (bool isValid, string errorMessage) ValidatePasswordStrength(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
                return (false, "Password is required");

            if (password.Length < 6)
                return (false, "Password must be at least 6 characters long");

            if (!password.Any(char.IsUpper))
                return (false, "Password must contain at least one uppercase letter");

            if (!password.Any(char.IsLower))
                return (false, "Password must contain at least one lowercase letter");

            if (!password.Any(char.IsDigit))
                return (false, "Password must contain at least one number");

            return (true, string.Empty);
        }
    }
}