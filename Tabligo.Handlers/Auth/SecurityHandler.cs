using System.Security.Cryptography;

namespace Tabligo.Handlers.Auth;

public class SecurityHandler
{
    public byte[] GenerateSalt(int size = 16)
    {
        var salt = new byte[size];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(salt);
        return salt;
    }

    // Hash the password using PBKDF2 (Rfc2898)
    public byte[] HashPassword(string password, byte[] salt, int iterations = 100_000, int hashLength = 32)
    {
        using var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        return pbkdf2.GetBytes(hashLength);
    }

    // Convert to Base64 for storing in DB
    public string GenerateSaltedHash(string password, out string saltBase64)
    {
        var salt = GenerateSalt();
        saltBase64 = Convert.ToBase64String(salt);

        var hash = HashPassword(password, salt);
        return Convert.ToBase64String(hash);
    }

    // Verify password
    public bool VerifyPassword(string enteredPassword, string storedSaltBase64, string storedHashBase64)
    {
        var salt = Convert.FromBase64String(storedSaltBase64);
        var expectedHash = Convert.FromBase64String(storedHashBase64);

        var enteredHash = HashPassword(enteredPassword, salt);
        return CryptographicOperations.FixedTimeEquals(expectedHash, enteredHash);
    }
}