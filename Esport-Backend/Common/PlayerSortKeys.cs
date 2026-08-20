namespace TForge.Common
{
    /// <summary>
    /// Дозволені ключі сортування списку гравців. Значення повинні збігатися
    /// з тими, що надсилає фронтенд (Esport-Frontend/src/constants/sortKeys.ts).
    ///
    /// Білий список, а не рефлексія по властивостях сутності: сортувати треба
    /// і за вкладеним полем (назва команди), і за обчисленими показниками
    /// (KDA, відсоток перемог), яких у Player немає. До того ж ApplySorting
    /// на невідоме імʼя тихо повертає невідсортований запит — тобто друкарська
    /// помилка в URL давала б неправильну сторінку замість помилки.
    /// </summary>
    public static class PlayerSortKeys
    {
        public const string Nickname = "nickname";
        public const string Country = "country";
        public const string Team = "team";
        public const string Matches = "matches";
        public const string Wins = "wins";
        public const string WinRate = "winRate";
        public const string Kda = "kda";
        public const string Rating = "rating";

        // Позиції тут немає навмисно: роль залежить від дисципліни
        // (Common/GamePositions.cs), тож сортувати за однією колонкою
        // «позиція» більше нема сенсу — у списку замість неї дисципліни.
        public static readonly string[] All =
        {
            Nickname, Country, Team, Matches, Wins, WinRate, Kda, Rating
        };

        public static bool IsValid(string? key) => key != null && All.Contains(key);
    }
}
