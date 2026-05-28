namespace Computational_Practice.DTOs
{
    public class TournamentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Game { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxTeams { get; set; }
        public int CurrentTeams { get; set; }
        public string Status { get; set; } = string.Empty;
        public decimal PrizePool { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public UserDto? Organizer { get; set; }
    }

    public class CreateTournamentDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Game { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxTeams { get; set; }
        public decimal PrizePool { get; set; }
        public int OrganizerId { get; set; }
    }

    public class UpdateTournamentDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxTeams { get; set; }
        public decimal PrizePool { get; set; }
        public string Status { get; set; } = string.Empty;
    }

    public class TournamentStatsDto
    {
        public int TotalTournaments { get; set; }
        public int ActiveTournaments { get; set; }
        public int CompletedTournaments { get; set; }
        public int RegistrationOpen { get; set; }
        public decimal TotalPrizePool { get; set; }
        public List<GameStatsDto> PopularGames { get; set; } = new();
    }

    public class GameStatsDto
    {
        public string Game { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
