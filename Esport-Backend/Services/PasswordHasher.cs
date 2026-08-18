using System.Security.Cryptography;
using System.Text;

namespace TForge.Services
{
    public interface IPasswordHasher
    {
        string Hash(string password);
        bool Verify(string password, string hash);

        /// <summary>
        /// Чи збережений хеш зроблено застарілим алгоритмом. Дозволяє тихо
        /// перевести акаунт на BCrypt під час наступного входу — єдиного
        /// моменту, коли пароль відомий у відкритому вигляді.
        /// </summary>
        bool NeedsRehash(string hash);
    }

    /// <summary>
    /// BCrypt із власною сіллю на кожен пароль і робочим фактором.
    ///
    /// Старий алгоритм (SHA-256 зі спільною сіллю) лишається тут лише для
    /// перевірки вже збережених хешів: без нього всі акаунти, створені до
    /// переходу, втратили б доступ. Нових таких хешів не з'являється, і коли
    /// в базі не лишиться жодного, приватну частину нижче можна видалити.
    /// </summary>
    public class PasswordHasher : IPasswordHasher
    {
        /// <summary>
        /// Робочий фактор BCrypt: кожна одиниця подвоює час перевірки.
        /// 12 — близько 250 мс на звичайній машині: непомітно для входу
        /// й дорого для перебору.
        /// </summary>
        private const int WorkFactor = 12;

        private const string LegacySalt = "SALT_KEY";

        /// <summary>Будь-який хеш BCrypt починається з $2; Base64 від SHA-256 — ніколи.</summary>
        private const string BCryptPrefix = "$2";

        public string Hash(string password) =>
            BCrypt.Net.BCrypt.HashPassword(password, WorkFactor);

        public bool Verify(string password, string hash)
        {
            if (string.IsNullOrEmpty(hash))
            {
                return false;
            }

            if (!NeedsRehash(hash))
            {
                try
                {
                    return BCrypt.Net.BCrypt.Verify(password, hash);
                }
                catch (Exception ex) when (ex is BCrypt.Net.SaltParseException or ArgumentException)
                {
                    // Зіпсований рядок у базі — це невдалий вхід, а не аварія.
                    // ArgumentException ловимо теж: різні версії BCrypt.Net
                    // повідомляють про непридатний хеш по-різному.
                    return false;
                }
            }

            return VerifyLegacy(password, hash);
        }

        public bool NeedsRehash(string hash) =>
            !hash.StartsWith(BCryptPrefix, StringComparison.Ordinal);

        private static string LegacyHash(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password + LegacySalt));
            return Convert.ToBase64String(hashedBytes);
        }

        private static bool VerifyLegacy(string password, string hash) =>
            CryptographicOperations.FixedTimeEquals(
                Encoding.UTF8.GetBytes(LegacyHash(password)),
                Encoding.UTF8.GetBytes(hash));
    }
}
