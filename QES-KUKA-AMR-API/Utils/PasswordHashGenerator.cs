using BCrypt.Net;

namespace QES_KUKA_AMR_API.Utils;

/// <summary>
/// Utility class for generating BCrypt password hashes.
/// Use this to generate hashes for SQL scripts or testing.
/// </summary>
public static class PasswordHashGenerator
{
    /// <summary>
    /// Generates a BCrypt hash for the given password.
    /// </summary>
    /// <param name="password">The plaintext password</param>
    /// <returns>BCrypt hash string</returns>
    public static string GenerateHash(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 11);
    }

    /// <summary>
    /// Verifies if a password matches a hash.
    /// </summary>
    public static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }

    // Example usage - run this to generate hash for "admin" password:
    // var hash = PasswordHashGenerator.GenerateHash("admin");
    // Console.WriteLine($"BCrypt hash for 'admin': {hash}");
}

/*
 * QUICK REFERENCE:
 * ================
 * To generate a hash for the password "admin", run this code:
 *
 * var hash = PasswordHashGenerator.GenerateHash("admin");
 * Console.WriteLine($"Hash: {hash}");
 *
 * Then use this hash in your SQL script or seed data.
 *
 * Example hashes (these will differ each time due to salt):
 * Password "admin":  $2a$11$XYZ... (60 characters)
 * Password "Admin":  $2a$11$ABC... (60 characters)
 */
