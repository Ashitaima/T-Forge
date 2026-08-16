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

        /// <summary>Успадкована роль без профілю гравця. Нових таких не створюємо.</summary>
        public const string User = "User";

        public static readonly string[] All = { Player, Organizer, Admin, User };

        /// <summary>
        /// Ролі, які користувач може отримати самостійно через реєстрацію.
        /// Admin сюди не входить навмисно — інакше будь-хто отримав би повні права.
        /// </summary>
        public static readonly string[] SelfService = { Player, Organizer };

        public static bool IsValid(string? role) => role != null && All.Contains(role);

        public static bool IsSelfService(string? role) => role != null && SelfService.Contains(role);
    }
}
