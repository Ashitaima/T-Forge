namespace TForge.DTOs
{
    public class TeamOpeningDto
    {
        public int Id { get; set; }
        public int TeamId { get; set; }
        public TeamSummaryDto? Team { get; set; }
        public string Game { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string MinRank { get; set; } = string.Empty;
        public int? MinUtcOffsetMinutes { get; set; }
        public int? MaxUtcOffsetMinutes { get; set; }
        public string Pitch { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
    }

    /// <summary>
    /// Статусу тут немає: сервер завжди відкриває вакансію в Open — те саме
    /// правило, за яким статус прибрали зі створення турніру. Власника теж
    /// немає — його визначає токен.
    /// </summary>
    public class CreateTeamOpeningDto
    {
        public int TeamId { get; set; }
        public string Game { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string MinRank { get; set; } = string.Empty;
        public int? MinUtcOffsetMinutes { get; set; }
        public int? MaxUtcOffsetMinutes { get; set; }
        public string Pitch { get; set; } = string.Empty;
    }

    public class UpdateTeamOpeningDto
    {
        public string Position { get; set; } = string.Empty;
        public string MinRank { get; set; } = string.Empty;
        public int? MinUtcOffsetMinutes { get; set; }
        public int? MaxUtcOffsetMinutes { get; set; }
        public string Pitch { get; set; } = string.Empty;
    }
}
