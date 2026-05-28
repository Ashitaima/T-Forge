namespace Computational_Practice.DTOs
{
    public class PlayerDto
    {
        public int Id { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int Age { get; set; }
        public int TotalMatches { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal WinRate { get; set; }
        public int Ranking { get; set; }
        public bool IsActive { get; set; }
        public DateTime JoinedAt { get; set; }
        public UserDto? User { get; set; }
        public TeamDto? Team { get; set; }
    }

    public class CreatePlayerDto
    {
        public string Nickname { get; set; } = string.Empty;
        public string Position { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int Age { get; set; }
        public int UserId { get; set; }
        public int? TeamId { get; set; }
    }

    public class UpdatePlayerDto
    {
        public string Position { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public int Age { get; set; }
        public int? TeamId { get; set; }
    }

    public class PlayerStatsDto
    {
        public int PlayerId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public int TotalMatches { get; set; }
        public int Wins { get; set; }
        public int Losses { get; set; }
        public decimal WinRate { get; set; }
        public double AverageKills { get; set; }
        public double AverageDeaths { get; set; }
        public double AverageAssists { get; set; }
        public double KDRatio { get; set; }
    }
}
