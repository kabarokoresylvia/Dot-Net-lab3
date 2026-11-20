using System.Security.Cryptography;

namespace GuestHouseMS.Data;

public static class Security
{
    public static string NewSalt() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16));

    public static string Hash(string password, string salt) =>
        Convert.ToHexString(Rfc2898DeriveBytes.Pbkdf2(
            password, Convert.FromHexString(salt), 100_000, HashAlgorithmName.SHA256, 32));
}
