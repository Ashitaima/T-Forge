namespace TForge.Common
{
    /// <summary>
    /// Сторони за дисципліною. Окремо від GameMaps, бо сторона є в усіх
    /// чотирьох дисциплінах, а карта — лише у двох.
    ///
    /// Перше значення — те, що вважається «першою» стороною; саме воно
    /// підставляється, коли сторону обирає таймер і випадок випав на неї.
    /// </summary>
    public static class GameSides
    {
        public static readonly string[] CS2 = { "CT", "T" };
        public static readonly string[] Valorant = { "Attack", "Defense" };
        public static readonly string[] Dota2 = { "Radiant", "Dire" };
        public static readonly string[] LeagueOfLegends = { "Blue", "Red" };

        private static readonly string[] None = Array.Empty<string>();

        public static string[] ForGame(string? game) => game switch
        {
            Games.CS2 => CS2,
            Games.Valorant => Valorant,
            Games.Dota2 => Dota2,
            Games.LeagueOfLegends => LeagueOfLegends,
            _ => None
        };

        public static bool IsValid(string? game, string? side) =>
            side != null && ForGame(game).Contains(side);
    }
}
