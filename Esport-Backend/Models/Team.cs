using System.ComponentModel.DataAnnotations;
using System.Numerics;
using System.Text.RegularExpressions;
namespace Computational_Practice.Models
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
