namespace TForge.DTOs
{
    /// <summary>
    /// Дуель 1 на 1. Показники дуелей навмисно окремі від показників матчів —
    /// див. docs/superpowers/specs/2026-08-19-duel-1v1-design.md.
    /// </summary>
    public class DuelDto
    {
        public int Id { get; set; }
        public PlayerSummaryDto? ChallengerPlayer { get; set; }
        public PlayerSummaryDto? OpponentPlayer { get; set; }
        public string Game { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int ChallengerScore { get; set; }
        public int OpponentScore { get; set; }

        /// <summary>Player.Id переможця. Null у завершеній дуелі — нічия.</summary>
        public int? WinnerPlayerId { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }

        /// <summary>
        /// Акаунти обох сторін. Потрібні клієнту, щоб повторити перевірку
        /// DuelPolicy й не показувати кнопку тому, хто нею не скористається —
        /// той самий привід, що в MatchDto.HomeTeamCaptainId.
        /// </summary>
        public int ChallengerUserId { get; set; }

        /// <summary>Null у відкритому виклику — суперника ще не названо.</summary>
        public int? OpponentUserId { get; set; }

        /// <summary>Відкритий виклик: прийняти може будь-хто, крім ініціатора.</summary>
        public bool IsOpen { get; set; }
    }

    /// <summary>
    /// Виклик на дуель. Того, хто викликає, визначає сервер за токеном —
    /// як і скрізь, де є власник.
    /// </summary>
    public class CreateDuelDto
    {
        /// <summary>
        /// Null — відкритий виклик: суперника не названо, і прийняти дуель
        /// може будь-який інший гравець.
        /// </summary>
        public int? OpponentPlayerId { get; set; }
        public string Game { get; set; } = string.Empty;
        public DateTime ScheduledAt { get; set; }
        public string Format { get; set; } = "BO1";
        public string Message { get; set; } = string.Empty;
    }

    public class CompleteDuelDto
    {
        public int ChallengerScore { get; set; }
        public int OpponentScore { get; set; }

        /// <summary>Player.Id переможця, або null — нічия.</summary>
        public int? WinnerPlayerId { get; set; }
    }

    /// <summary>Рахунок гравця в дуелях — окремо від командного.</summary>
    public class DuelRecordDto
    {
        public int Played { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public int Draws { get; set; }
        public decimal WinRate { get; set; }
    }
}
