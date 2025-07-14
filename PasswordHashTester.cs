using System.Security.Cryptography;
using System.Text;

namespace ClubManagementApp
{
    /// <summary>
    /// Helper class for testing password hashing and verification
    /// Used for debugging authentication issues
    /// </summary>
    public static class PasswordHashTester
    {
        /// <summary>
        /// Hash a password using the same SHA256 method as the application
        /// </summary>
        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        /// <summary>
        /// Verify a password against a hash
        /// </summary>
        public static bool VerifyPassword(string password, string hashedPassword)
        {
            var hashedInput = HashPassword(password);
            return hashedInput == hashedPassword;
        }

        /// <summary>
        /// Test the common passwords used in the application
        /// </summary>
        public static void RunPasswordTests()
        {
            Console.WriteLine("=== Password Hash Testing ===");

            var testPasswords = new[]
            {
                "admin123",
                "password123"
            };

            foreach (var password in testPasswords)
            {
                var hash = HashPassword(password);
                var verification = VerifyPassword(password, hash);

                Console.WriteLine($"Password: '{password}'");
                Console.WriteLine($"Hash: {hash}");
                Console.WriteLine($"Verification: {verification}");
                Console.WriteLine("---");
            }

            // Test against known hashes from SQL script
            Console.WriteLine("=== Testing against SQL script hashes ===");

            var sqlHashes = new Dictionary<string, string>
            {
                { "admin123", "JAvlGPq9JyTdtvBO6x2llnRI1+gxwIyPqCKAn3THIKk=" },
                { "password123", "XohImNooBHFR0OVvjcYpJ3NgPQ1qq73WKhHvch0VQtg=" }
            };

            foreach (var (password, expectedHash) in sqlHashes)
            {
                var calculatedHash = HashPassword(password);
                var hashMatches = calculatedHash == expectedHash;
                var verification = VerifyPassword(password, expectedHash);

                Console.WriteLine($"Password: '{password}'");
                Console.WriteLine($"Expected Hash (SQL): {expectedHash}");
                Console.WriteLine($"Calculated Hash: {calculatedHash}");
                Console.WriteLine($"Hash Match: {hashMatches}");
                Console.WriteLine($"Verification: {verification}");
                Console.WriteLine("---");
            }
        }

        /// <summary>
        /// Test specific user credentials
        /// </summary>
        public static void TestUserCredentials()
        {
            Console.WriteLine("=== User Credential Testing ===");

            var testCredentials = new Dictionary<string, string>
            {
                { "admin@university.edu", "admin123" },
                { "john.doe@university.edu", "password123" },
                { "jane.smith@university.edu", "password123" }
            };

            foreach (var (email, password) in testCredentials)
            {
                var hash = HashPassword(password);
                Console.WriteLine($"User: {email}");
                Console.WriteLine($"Password: {password}");
                Console.WriteLine($"Hash: {hash}");
                Console.WriteLine("---");
            }
        }
    }
}
