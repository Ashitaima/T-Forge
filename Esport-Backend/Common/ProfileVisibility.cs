namespace TForge.Common
{
    /// <summary>
    /// Кому видно приховані поля профілю. Чиста функція, як
    /// FriendlyMatchPolicy і TournamentOwnershipPolicy — сервіс читає рядки,
    /// рішення ухвалює цей клас.
    ///
    /// Ховати можна лише те, від чого не залежать таблиці: справжнє імʼя, вік
    /// і країну. Нікнейм, показники й рейтинг лишаються видимими завжди —
    /// без них список гравців перестав би бути списком.
    ///
    /// Приховане поле віддається порожнім, а не пропускається: клієнт має
    /// бачити «не вказано» так само, як для незаповненого поля, і не
    /// здогадуватися про різницю.
    /// </summary>
    public static class ProfileVisibility
    {
        /// <summary>
        /// Власник бачить свій профіль повністю — інакше він не міг би
        /// перевірити, що саме сховав. Адміністратор теж: він стоїть над
        /// правилами власності, як і скрізь у проєкті.
        /// </summary>
        public static bool CanSeeHidden(int subjectUserId, int? viewerUserId, bool isAdmin) =>
            isAdmin || (viewerUserId.HasValue && viewerUserId.Value == subjectUserId);

        /// <summary>Значення поля або порожній рядок, якщо його сховано від цього глядача.</summary>
        public static string Apply(string? value, bool isHidden, bool canSeeHidden) =>
            isHidden && !canSeeHidden ? string.Empty : value ?? string.Empty;

        /// <summary>Те саме для віку: 0 означає «не вказано», як і в самій колонці.</summary>
        public static int Apply(int value, bool isHidden, bool canSeeHidden) =>
            isHidden && !canSeeHidden ? 0 : value;
    }
}
