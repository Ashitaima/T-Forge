namespace TForge.Common
{
    /// <summary>
    /// Дозволені ключі сортування списку команд. Значення повинні збігатися
    /// з тими, що надсилає фронтенд (Esport-Frontend/src/constants/sortKeys.ts).
    /// </summary>
    public static class TeamSortKeys
    {
        public const string Name = "name";
        public const string Tag = "tag";
        public const string Region = "region";
        public const string Played = "played";
        public const string Wins = "wins";
        public const string WinRate = "winRate";
        public const string Titles = "titles";

        public static readonly string[] All =
        {
            Name, Tag, Region, Played, Wins, WinRate, Titles
        };

        public static bool IsValid(string? key) => key != null && All.Contains(key);
    }
}
