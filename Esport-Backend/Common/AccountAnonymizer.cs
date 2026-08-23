namespace TForge.Common
{
    /// <summary>
    /// Значення, якими заміщують особисті дані видаленого акаунта. Чиста
    /// функція без EF, як решта правил у Common.
    ///
    /// Видалення тут — знеособлення, а не DELETE рядка, і причина та сама, з
    /// якої MatchService не дає видалити зіграний матч. Ланцюг зовнішніх ключів
    /// User → Player → MatchPlayer каскадний, тож справжнє видалення тихо
    /// стерло б кожну появу гравця в кожному зіграному матчі й частину
    /// append-only журналу рейтингу. Підсумки інших команд після цього
    /// пояснити нічим.
    ///
    /// Username, Email і Player.Nickname несуть унікальні індекси, тому
    /// підстановки виводяться з id: два видалені акаунти з однаковим рядком
    /// просто не збереглися б.
    /// </summary>
    public static class AccountAnonymizer
    {
        /// <summary>Домен .invalid зарезервовано RFC 2606 — лист туди не піде нікому.</summary>
        public const string DeletedEmailDomain = "deleted.invalid";

        private const string UsernamePrefix = "deleted_user_";

        /// <summary>Що показуємо замість імені там, де рядок усе одно видно.</summary>
        public const string DeletedDisplayName = "Видалений акаунт";

        /// <summary>
        /// Порожній хеш. PasswordHasher.Verify повертає на ньому false, не
        /// кидаючи винятку, тож знеособлений акаунт не пускає нікого — і для
        /// цього не потрібно окремої перевірки на вході.
        /// </summary>
        public const string UnusablePasswordHash = "";

        public static string UsernameFor(int userId) => $"{UsernamePrefix}{userId}";

        public static string EmailFor(int userId) => $"deleted_{userId}@{DeletedEmailDomain}";

        /// <summary>
        /// Нікнейм мусить лишатися придатним для звичайної форми, тож він
        /// підпорядковується Validators/PlayerRules: лише літери, цифри й
        /// підкреслення, від 3 до 30 символів.
        /// </summary>
        public static string NicknameFor(int playerId) => $"deleted_player_{playerId}";

        public static bool IsAnonymized(string? username) =>
            username != null && username.StartsWith(UsernamePrefix, StringComparison.Ordinal);
    }
}
