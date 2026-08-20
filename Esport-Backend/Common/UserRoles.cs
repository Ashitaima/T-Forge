namespace TForge.Common
{
    /// <summary>
    /// Канонічні ролі користувача. Значення повинні збігатися з тими,
    /// що використовує фронтенд і атрибути [Authorize(Roles = ...)].
    /// </summary>
    public static class UserRoles
    {
        public const string Player = "Player";
        public const string Organizer = "Organizer";
        public const string Admin = "Admin";

        public static readonly string[] All = { Player, Organizer, Admin };

        /// <summary>
        /// Успадкована роль «User» — акаунт без профілю гравця. Її прибрано:
        /// реєстрація однаково створює профіль, тож роль без нього означала
        /// лише «щось недороблене». Наявні рядки переводить у Player
        /// DatabaseInitializer.NormalizeLegacyRolesAsync — один раз, як і з
        /// країнами; тримати константу заради них не потрібно, бо після
        /// нормалізації таких рядків не лишається.
        /// </summary>
        public const string LegacyUser = "User";

        /// <summary>
        /// Ролі, які користувач може отримати самостійно через реєстрацію.
        /// Admin сюди не входить навмисно — інакше будь-хто отримав би повні права.
        /// </summary>
        public static readonly string[] SelfService = { Player, Organizer };

        public static bool IsValid(string? role) => role != null && All.Contains(role);

        public static bool IsSelfService(string? role) => role != null && SelfService.Contains(role);
    }
}
