namespace TForge.DTOs
{
    /// <summary>
    /// Виклик на матч для клієнта. Містить назви обох команд, бо той самий тип
    /// показують і на сторінці команди, і в індикаторі у бічній панелі.
    /// </summary>
    public class MatchChallengeDto
    {
        public int Id { get; set; }
        public int ChallengerTeamId { get; set; }
        public string ChallengerTeamName { get; set; } = string.Empty;
        public string ChallengerTeamTag { get; set; } = string.Empty;
        public int OpponentTeamId { get; set; }
        public string OpponentTeamName { get; set; } = string.Empty;
        public string OpponentTeamTag { get; set; } = string.Empty;
        public string Game { get; set; } = string.Empty;
        public DateTime ProposedAt { get; set; }
        public string Format { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? RespondedAt { get; set; }
        public int? MatchId { get; set; }
    }

    /// <summary>Капітан створює виклик. Право на це сервер перевіряє за токеном.</summary>
    public class CreateMatchChallengeDto
    {
        public int ChallengerTeamId { get; set; }
        public int OpponentTeamId { get; set; }
        public string Game { get; set; } = string.Empty;
        public DateTime ProposedAt { get; set; }
        public string Format { get; set; } = "BO1";
        public string Message { get; set; } = string.Empty;
    }
}
