using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.RegularExpressions;
using TForge.Common;

namespace TForge.Models
{
    public class Tournament
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string Description { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string Game { get; set; } = string.Empty;

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        public int MaxTeams { get; set; }

        public int CurrentTeams { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Закритий турнір: склад учасників визначає організатор. Капітан не
        /// реєструє команду сам — або отримує запрошення, або подає заявку.
        /// Рішення про те, кому це дозволено, ухвалює TournamentInvitationPolicy.
        /// </summary>
        public bool IsInviteOnly { get; set; } = false;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = TournamentStatus.Registration; // див. Common/StatusConstants.cs

        [Column(TypeName = "decimal(10,2)")]
        public decimal PrizePool { get; set; } = 0;

        [Required]
        public int OrganizerId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Team> Teams { get; set; } = new List<Team>();
        public virtual ICollection<Match> Matches { get; set; } = new List<Match>();
        public virtual User Organizer { get; set; } = null!;
    }
}
