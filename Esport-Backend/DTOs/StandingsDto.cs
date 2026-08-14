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

    /// <summary>Рядок загальної таблиці команд по всіх турнірах.</summary>
    public class TeamStandingDto
    {
        public int Rank { get; set; }
        public TeamSummaryDto? Team { get; set; }
        public int Played { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal WinRate { get; set; }

        /// <summary>Скільки разів команда вигравала фінал.</summary>
        public int Titles { get; set; }
    }

    /// <summary>Рядок таблиці гравців: агрегована статистика з ростерів матчів.</summary>
    public class PlayerStandingDto
    {
        public int Rank { get; set; }
        public PlayerSummaryDto? Player { get; set; }
        public string? TeamName { get; set; }
        public int Matches { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }

        /// <summary>(Вбивства + асисти) / смерті. За нуля смертей рахуємо як за одну.</summary>
        public double Kda { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal WinRate { get; set; }
    }
}
