namespace Computational_Practice.DTOs
{
    public class MatchDto
    {
        public int Id { get; set; }
        public DateTime ScheduledAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int HomeTeamScore { get; set; }
        public int AwayTeamScore { get; set; }
        public string MatchType { get; set; } = string.Empty;
        public string Format { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public TeamDto? HomeTeam { get; set; }
        public TeamDto? AwayTeam { get; set; }
        public TeamDto? WinnerTeam { get; set; }
        public TournamentDto? Tournament { get; set; }
        public List<MatchPlayerDto> MatchPlayers { get; set; } = new();
    }

    public class CreateMatchDto
    {
        public int TournamentId { get; set; }
        public int HomeTeamId { get; set; }
        public int AwayTeamId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public string MatchType { get; set; } = "GroupStage";
        public string Format { get; set; } = "BO1";
        public string Notes { get; set; } = string.Empty;
    }

    public class UpdateMatchDto
    {
        public DateTime ScheduledAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public int HomeTeamScore { get; set; }
        public int AwayTeamScore { get; set; }
        public int? WinnerTeamId { get; set; }
        public string Notes { get; set; } = string.Empty;
        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
    }

    public class MatchPlayerDto
    {
        public int Id { get; set; }
        public int Kills { get; set; }
        public int Deaths { get; set; }
        public int Assists { get; set; }
        public string Champion { get; set; } = string.Empty;
        public bool IsStarter { get; set; }
        public PlayerDto? Player { get; set; }
    }
}
