namespace TForge.Common
{
    /// <summary>
    /// Пул карт за дисципліною. Той самий поділ, що в Games і Countries: тут
    /// значення, підписи — на клієнті (Esport-Frontend/src/constants/gameMaps.ts).
    ///
    /// У Dota 2 і League of Legends карта одна на всіх, тож пул порожній — і це
    /// не «ще не заповнили», а властивість дисципліни. Драфт у них іде по
    /// героях (Common/GameHeroes.cs).
    ///
    /// Сім карт — не випадковість: послідовності вето в
    /// Common/DraftSequences.cs розраховані рівно на сім, і DraftSequencesTests
    /// це закріплює. Змінити розмір пулу без правки послідовностей не можна.
    ///
    /// Обидва пули — знімок на серпень 2026 року. Valve міняє Active Duty раз
    /// на кілька сезонів, Riot — щоактово, тож список тут доводиться правити
    /// руками. Уже зіграні драфти від цього не псуються: вони зберігають свої
    /// значення рядками.
    /// </summary>
    public static class GameMaps
    {
        /// <summary>Active Duty. Anubis замінив Train у січні 2026.</summary>
        public static readonly string[] CS2 =
        {
            "Ancient", "Anubis", "Dust2", "Inferno", "Mirage", "Nuke", "Overpass"
        };

        /// <summary>Змагальна ротація. Summit додано в патчі 13.00 (червень 2026).</summary>
        public static readonly string[] Valorant =
        {
            "Abyss", "Ascent", "Haven", "Lotus", "Split", "Summit", "Sunset"
        };

        private static readonly string[] None = Array.Empty<string>();

        public static string[] ForGame(string? game) => game switch
        {
            Games.CS2 => CS2,
            Games.Valorant => Valorant,
            _ => None
        };

        public static bool HasMaps(string? game) => ForGame(game).Length > 0;

        public static bool IsValid(string? game, string? map) =>
            map != null && ForGame(game).Contains(map);
    }
}
