using System.ComponentModel.DataAnnotations;
using TForge.Common;

namespace TForge.Models
{
    /// <summary>
    /// Виклик однієї команди на товариський матч. Відповідає завжди капітан
    /// викликаної команди; прийнятий виклик створює матч без турніру.
    /// Побудований за зразком TeamMembershipRequest.
    /// </summary>
    public class MatchChallenge
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int ChallengerTeamId { get; set; }

        [Required]
        public int OpponentTeamId { get; set; }

        /// <summary>Дисципліна. Товариський матч не має турніру, тож її обирає капітан.</summary>
        [Required]
        [StringLength(50)]
        public string Game { get; set; } = string.Empty;

        [Required]
        public DateTime ProposedAt { get; set; }

        [StringLength(10)]
        public string Format { get; set; } = "BO1";

        [StringLength(300)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = MatchChallengeStatus.Pending;

        /// <summary>Id користувача, а не команди. Заповнює сервер з токена.</summary>
        [Required]
        public int InitiatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }

        public int? RespondedByUserId { get; set; }

        /// <summary>Матч, створений при прийнятті виклику. До того — null.</summary>
        public int? MatchId { get; set; }

        // Navigation Properties
        public virtual Team ChallengerTeam { get; set; } = null!;
        public virtual Team OpponentTeam { get; set; } = null!;
        public virtual Match? Match { get; set; }
    }
}
