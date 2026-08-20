using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Text.RegularExpressions;
namespace TForge.Models
{
    public class Team
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(50)]
        public string Name { get; set; } = string.Empty;

        [StringLength(10)]
        public string Tag { get; set; } = string.Empty; // Короткий тег команди (наприклад, "FNC")

        [StringLength(300)]
        public string Description { get; set; } = string.Empty;

        [Required]
        public int CaptainId { get; set; }

        [StringLength(100)]
        public string Region { get; set; } = string.Empty;

        /// <summary>
        /// Шлях від кореня до логотипа, як User.AvatarPath. Самі байти лежать
        /// у wwwroot/uploads/team-logos/. Заповнює сервер — у CreateTeamDto
        /// та UpdateTeamDto цього поля навмисно немає.
        /// </summary>
        [StringLength(255)]
        public string? LogoPath { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual User Captain { get; set; } = null!;
        public virtual ICollection<Player> Players { get; set; } = new List<Player>();
        public virtual ICollection<Tournament> Tournaments { get; set; } = new List<Tournament>();
        public virtual ICollection<Match> HomeMatches { get; set; } = new List<Match>();
        public virtual ICollection<Match> AwayMatches { get; set; } = new List<Match>();
    }
}
