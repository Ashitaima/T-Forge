using System.Security.Cryptography;
using System.Text;

namespace TForge.Services
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);
    }

    /// <summary>
    /// УВАГА: SHA-256 зі спільною сіллю — слабкий алгоритм для паролів
    /// (немає per-user солі та work factor). Винесено в окремий сервіс саме для того,
    /// щоб заміна на BCrypt/Argon2 була зміною одного файлу.
    /// Хеші обчислюються під час виконання, тому сідер не потребуватиме змін.
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        private const string LegacySalt = "SALT_KEY";

        public string Hash(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + LegacySalt));
            return Convert.ToBase64String(hashedBytes);
        }

        public bool Verify(string password, string hash)
        {
            var computedHash = Hash(password);
            return CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(computedHash),
                Encoding.UTF8.GetBytes(hash));
        }
    }
}
