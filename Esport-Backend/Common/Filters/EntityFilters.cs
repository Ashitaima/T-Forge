namespace Computational_Practice.Common.Filters
{
    public class TournamentFilter : PagedRequest
    {
        public string? Status { get; set; }
        public string? Game { get; set; }
        public DateTime? StartDateFrom { get; set; }
        public DateTime? StartDateTo { get; set; }
        public decimal? MinPrizePool { get; set; }
        public decimal? MaxPrizePool { get; set; }
        public bool? IsActive { get; set; }
        public int? OrganizerId { get; set; }
    }

    public class TeamFilter : PagedRequest
    {
        public string? Name { get; set; }
        public string? Region { get; set; }
        public int? CaptainId { get; set; }
        public bool? IsActive { get; set; }
        public int? TournamentId { get; set; }
        public int? MinPlayersCount { get; set; }
        public int? MaxPlayersCount { get; set; }
    }

    public class PlayerFilter : PagedRequest
    {
        public string? Position { get; set; }
        public string? Country { get; set; }
        public int? MinAge { get; set; }
        public int? MaxAge { get; set; }
        public int? TeamId { get; set; }
        public bool? IsActive { get; set; }
        public bool? FreeAgents { get; set; }
    }

    public class MatchFilter : PagedRequest
    {
        public string? Status { get; set; }
        public int? TournamentId { get; set; }
        public int? TeamId { get; set; }
        public int? PlayerId { get; set; }
        public DateTime? ScheduledFrom { get; set; }
        public DateTime? ScheduledTo { get; set; }
        public string? MatchType { get; set; }
    }

    public class UserFilter : PagedRequest
    {
        public string? Role { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}
