namespace TForge.DTOs
{
    /// <summary>Місце команди в конкретному турнірі, виведене з турнірної сітки.</summary>
    public class TournamentStandingDto
    {
        public int Place { get; set; }
        public TeamSummaryDto? Team { get; set; }

        /// <summary>Наскільки далеко команда пройшла: «Чемпіон», «Фіналіст», «1/2 фіналу»…</summary>
        public string Outcome { get; set; } = string.Empty;

        public int Played { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public bool StillPlaying { get; set; }
    }

    // Загальні таблиці команд і гравців більше не окремі DTO: ті самі показники
    // тепер рахуються в межах сторінок списків (TeamRowDto, PlayerRowDto), щоб
    // їх можна було сортувати й гортати засобами бази.

    /// <summary>Форма команди: підсумковий рекорд і поточна серія.</summary>
    public class TeamSummaryStatsDto
    {
        public int Played { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal WinRate { get; set; }
        public StreakDto? Streak { get; set; }
    }

    public class StreakDto
    {
        public string Type { get; set; } = string.Empty;  // "Win" | "Loss"
        public int Count { get; set; }
    }
}
