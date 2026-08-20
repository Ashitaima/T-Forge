namespace TForge.DTOs
{
    public class MatchDto
    {
        public int Id { get; set; }

        /// <summary>
        /// Null — товариський матч. Читається з самої колонки, а не з навігації
        /// Tournament: та буває null і просто тому, що її не підвантажили.
        /// </summary>
        public int? TournamentId { get; set; }

        public DateTime ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int HomeTeamScore { get; set; }
        public int AwayTeamScore { get; set; }
        public string MatchType { get; set; } = string.Empty;
        /// <summary>Дисципліна, успадкована від турніру. Клієнт її не задає.</summary>
        public string Game { get; set; } = string.Empty;
        public int Round { get; set; }
        public string Format { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public string? StreamUrl { get; set; }

        /// <summary>Сторінка матчу в зовнішньому трекері статистики. Необовʼязкове.</summary>
        public string? TrackerUrl { get; set; }

        public DateTime CreatedAt { get; set; }
        public TeamSummaryDto? HomeTeam { get; set; }
        public TeamSummaryDto? AwayTeam { get; set; }

        /// <summary>
        /// Капітани команд. Потрібні клієнту, щоб показати керування товариським
        /// матчем: TeamSummaryDto.Captain у відповідях матчу не підвантажується.
        /// </summary>
        public int HomeTeamCaptainId { get; set; }

        /// <summary>Порожній у відкритому матчі — гостя ще немає.</summary>
        public int? AwayTeamCaptainId { get; set; }

        /// <summary>Власна назва матчу, якщо її дали.</summary>
        public string? Name { get; set; }

        /// <summary>Відкритий матч: приєднатися може капітан будь-якої іншої команди.</summary>
        public bool IsOpen { get; set; }
        public TeamSummaryDto? WinnerTeam { get; set; }
        public TournamentDto? Tournament { get; set; }
        public List<MatchPlayerDto> MatchPlayers { get; set; } = new();
    }

    public class CreateMatchDto
    {
        /// <summary>
        /// Null — товариський матч. Дисципліну тоді задає клієнт (нижче),
        /// бо успадкувати її нема від чого; у турнірному матчі Game завжди
        /// береться з турніру й будь-яке значення клієнта ігнорується.
        /// </summary>
        public int? TournamentId { get; set; }

        /// <summary>
        /// Домашня команда. Null — сервер візьме команду, якою капітанує той,
        /// хто створює матч; якщо таких команд кілька, він попросить уточнити.
        /// </summary>
        public int? HomeTeamId { get; set; }

        /// <summary>Null — відкритий матч: суперника назве той, хто приєднається.</summary>
        public int? AwayTeamId { get; set; }

        /// <summary>Власна назва матчу. Необов'язкова.</summary>
        public string? Name { get; set; }

        public DateTime ScheduledAt { get; set; }

        /// <summary>Лише для товариського матчу — див. Common/Games.cs.</summary>
        public string? Game { get; set; }

        public string MatchType { get; set; } = "GroupStage";
        public string Format { get; set; } = "BO1";
        public string Notes { get; set; } = string.Empty;
        public string? StreamUrl { get; set; }
        public string? TrackerUrl { get; set; }
    }

    public class UpdateMatchDto
    {
        public DateTime ScheduledAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int HomeTeamScore { get; set; }
        public int AwayTeamScore { get; set; }
        public int? WinnerTeamId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string? StreamUrl { get; set; }
        public string? TrackerUrl { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }

    public class CreateMatchPlayerDto
    {
        public int PlayerId { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public string Champion { get; set; } = string.Empty;
        public bool IsStarter { get; set; } = true;
    }

    public class UpdateMatchPlayerDto
    {
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public string Champion { get; set; } = string.Empty;
        public bool IsStarter { get; set; } = true;
    }

    /// <summary>Оновлення рахунку під час матчу.</summary>
    public class UpdateScoreDto
    {
        public int HomeTeamScore { get; set; }
        public int AwayTeamScore { get; set; }
    }

    public class MatchPlayerDto
    {
        public int Id { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public string Champion { get; set; } = string.Empty;
        public bool IsStarter { get; set; }
        public int PlayerId { get; set; }
        public int? TeamId { get; set; }
        public PlayerSummaryDto? Player { get; set; }
    }

    /// <summary>Рядок журналу матчів гравця — з погляду команди, за яку він грав.</summary>
    public class PlayerMatchDto
    {
        public int MatchId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public TeamSummaryDto? PlayedFor { get; set; }
        public TeamSummaryDto? Opponent { get; set; }
        public int TeamScore { get; set; }
        public int OpponentScore { get; set; }
        public string Result { get; set; } = string.Empty;  // "Win" | "Loss" | "Pending"
        public string? TournamentName { get; set; }
        public string MatchType { get; set; } = string.Empty;
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public string Champion { get; set; } = string.Empty;
    }
}
