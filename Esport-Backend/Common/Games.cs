namespace TForge.Common
{
    /// <summary>
    /// Канонічні дисципліни. Значення повинні збігатися з тими, що використовує
    /// фронтенд (Esport-Frontend/src/constants/games.ts) — інакше форма надішле
    /// значення, яке валідатор відхилить.
    /// </summary>
    public static class Games
    {
        public const string CS2 = "CS2";
        public const string Valorant = "Valorant";
        public const string Dota2 = "Dota2";
        public const string LeagueOfLegends = "LeagueOfLegends";

        public static readonly string[] All = { CS2, Valorant, Dota2, LeagueOfLegends };

        public static bool IsValid(string? game) => game != null && All.Contains(game);
    }
}
