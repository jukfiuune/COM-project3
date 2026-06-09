using System.Security.Cryptography;
using System.Text;

namespace Core.Services;

public sealed class PasswordService : IPasswordService
{
    private const int Iterations = 100_000;
    private const int SaltSize = 16;
    private const int HashSize = 32;

    public (bool IsValid, string? Error) ValidateComplexity(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
            return (false, "Password must be at least 8 characters.");

        if (!password.Any(char.IsLetter) || !password.Any(char.IsDigit))
            return (false, "Password must contain letters and numbers.");

        return (true, null);
    }

    public string Hash(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA512, HashSize);
        return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
    }

    public bool Verify(string password, string hash)
    {
        if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hash))
            return false;

        var parts = hash.Split('.', 2);
        if (parts.Length != 2)
            return false;

        var salt = Convert.FromBase64String(parts[0]);
        var expectedHash = Convert.FromBase64String(parts[1]);

        var actualHash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA512, HashSize);
        return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
    }
}
