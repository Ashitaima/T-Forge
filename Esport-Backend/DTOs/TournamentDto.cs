namespace TForge.DTOs
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

        /// <summary>Закритий турнір: склад учасників визначає організатор.</summary>
        public bool IsInviteOnly { get; set; }

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

        /// <summary>Закритий турнір: реєструватися самостійно капітани не можуть.</summary>
        public bool IsInviteOnly { get; set; }

        /// <summary>Заповнює сервер з токена. Задати вручну може лише адміністратор.</summary>
        public int? OrganizerId { get; set; }

        // Статус новоствореного турніру завжди Registration — сервер проставляє
        // його сам, тож у DTO створення цього поля немає й бути не повинно.
    }

    public class UpdateTournamentDto
    {
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int MaxTeams { get; set; }
        public decimal PrizePool { get; set; }
        public bool IsInviteOnly { get; set; }
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
