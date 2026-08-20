namespace TForge.Common
{
    /// <summary>
    /// Позиції, розібрані за дисциплінами — чиста функція, як і решта правил
    /// у Common.
    ///
    /// PlayerPositions тримає плаский перелік усіх позицій одразу, і цього
    /// вистачало, доки гравець мав рівно одну. Але «Support» у Dota 2 і
    /// «Support» у League of Legends — різні ролі, а «AWPer» у Valorant не
    /// існує взагалі: без прив'язки до дисципліни список пропонував гравцеві
    /// те, чого в його грі немає.
    ///
    /// Дзеркало на клієнті — Esport-Frontend/src/constants/gamePositions.ts,
    /// той самий поділ, що у Games та Countries: тут перелік, там підписи.
    /// </summary>
    public static class GamePositions
    {
        public static readonly string[] Valorant =
        {
            "Duelist", "Initiator", "Sentinel", "Controller"
        };

        public static readonly string[] CS2 =
        {
            "Rifler", "Entry", "Lurker", "AWPer", "IGL"
        };

        // Dota 2 позначає ролі номерами позицій — саме так їх називають самі
        // гравці, тож підпис у списку показує і номер, і звичну назву.
        public static readonly string[] Dota2 =
        {
            "Carry", "Midlaner", "Offlaner", "SoftSupport", "HardSupport"
        };

        public static readonly string[] LeagueOfLegends =
        {
            "Top", "Jungle", "Mid", "ADC", "Support"
        };

        /// <summary>Позиції для дисципліни; порожній масив, якщо дисципліна невідома.</summary>
        public static string[] For(string? game) => game switch
        {
            Games.Valorant => Valorant,
            Games.CS2 => CS2,
            Games.Dota2 => Dota2,
            Games.LeagueOfLegends => LeagueOfLegends,
            _ => Array.Empty<string>()
        };

        /// <summary>
        /// Чи підходить позиція саме цій дисципліні. Порожня позиція дозволена:
        /// гравець може вказати дисципліну, не уточнюючи роль.
        /// </summary>
        public static bool IsValidFor(string? game, string? position) =>
            string.IsNullOrEmpty(position) || For(game).Contains(position);
    }
}
