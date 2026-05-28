using System.ComponentModel.DataAnnotations;
using System.Numerics;
namespace Computational_Practice.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }
        [Required]
        [StringLength(50)]
        public string Username { get; set; } = string.Empty;
        [Required]
        [StringLength(100)]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        [StringLength(50)]
        public string FirstName { get; set; } = string.Empty;

        [StringLength(50)]
        public string LastName { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Role { get; set; } = "Player"; // Player, Organizer, Admin

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Tournament> OrganizedTournaments { get; set; } = new List<Tournament>();
        public virtual ICollection<Team> CaptainedTeams { get; set; } = new List<Team>();
        public virtual Player? PlayerProfile { get; set; }
    }
}