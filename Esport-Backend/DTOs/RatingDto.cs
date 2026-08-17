namespace TForge.DTOs
{
    /// <summary>
    /// Рейтинг в одній дисципліні. Команда чи гравець без турнірних матчів
    /// не має жодного такого рядка й показується як «без рейтингу», а не як
    /// фальшива тисяча.
    /// </summary>
    public class RatingDto
    {
        public string Game { get; set; } = string.Empty;
        public int Rating { get; set; }
        public int Peak { get; set; }
        public int MatchesRated { get; set; }

        /// <summary>Ліга, виведена з рейтингу тим самим калькулятором.</summary>
        public string Tier { get; set; } = string.Empty;

        public DateTime UpdatedAt { get; set; }
    }

    /// <summary>
    /// Точка на графіку рейтингу — одна зміна за один матч. З неї ж береться
    /// «+18» на картці матчу.
    /// </summary>
    public class RatingChangeDto
    {
        public int MatchId { get; set; }
        public string Game { get; set; } = string.Empty;
        public int Delta { get; set; }
        public int RatingBefore { get; set; }
        public int RatingAfter { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Контекст точки на графіку: з ким і в чому грали.</summary>
        public string? OpponentName { get; set; }
        public string? TournamentName { get; set; }
        public string MatchType { get; set; } = string.Empty;
    }

    /// <summary>Зміна рейтингу обох команд у конкретному матчі.</summary>
    public class MatchRatingDeltaDto
    {
        public int MatchId { get; set; }
        public string Game { get; set; } = string.Empty;
        public int? HomeDelta { get; set; }
        public int? AwayDelta { get; set; }
    }
}
