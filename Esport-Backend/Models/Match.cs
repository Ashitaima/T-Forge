using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using TForge.Common;
namespace TForge.Models
{
    public class Match
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TournamentId { get; set; }

        [Required]
        public int HomeTeamId { get; set; }

        [Required]
        public int AwayTeamId { get; set; }

        [Required]
        public DateTime ScheduledAt { get; set; }

        public DateTime? StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = MatchStatus.Scheduled; // див. Common/StatusConstants.cs

        public int HomeTeamScore { get; set; } = 0;
        public int AwayTeamScore { get; set; } = 0;

        public int? WinnerTeamId { get; set; } // Nullable - може бути нічия

        [StringLength(20)]
        public string MatchType { get; set; } = MatchTypes.GroupStage; // див. Common/StatusConstants.cs

        /// <summary>
        /// Раунд у турнірній сітці: 0 — матч поза сіткою, 1..n — раунди single elimination.
        /// </summary>
        public int Round { get; set; } = 0;

        [StringLength(10)]
        public string Format { get; set; } = "BO1"; // BO1, BO3, BO5 (Best of 1, 3, 5)

        [StringLength(500)]
        public string Notes { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Tournament Tournament { get; set; } = null!;
        public virtual Team HomeTeam { get; set; } = null!;
        public virtual Team AwayTeam { get; set; } = null!;
        public virtual Team? WinnerTeam { get; set; }
        public virtual ICollection<MatchPlayer> MatchPlayers { get; set; } = new List<MatchPlayer>();
    }
}
