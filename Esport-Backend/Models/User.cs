using System.ComponentModel.DataAnnotations;
using System.Numerics;
namespace TForge.Models
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

        /// <summary>Шлях до файлу аватара відносно wwwroot. Null — аватара немає.</summary>
        [StringLength(200)]
        public string? AvatarPath { get; set; }

        /// <summary>
        /// Коли користувач востаннє відкривав сповіщення. Непрочитане — це те,
        /// що сталося пізніше за цю мітку. Одна колонка замість прапорця на
        /// кожен рядок: самих рядків сповіщень ми не зберігаємо, вони виводяться
        /// із запитів (див. NotificationService). null означає «ще не відкривав»,
        /// тож новий користувач бачить усе як нове.
        /// </summary>
        public DateTime? NotificationsSeenAt { get; set; }

        /// <summary>
        /// Чи ховати справжнє імʼя від інших. Нікнейм лишається видимим завжди —
        /// без нього гравця не впізнати ні в складі, ні в таблиці.
        /// Рішення ухвалює Common/ProfileVisibility.cs.
        /// </summary>
        public bool IsNameHidden { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime LastLoginAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual ICollection<Tournament> OrganizedTournaments { get; set; } = new List<Tournament>();
        public virtual ICollection<Team> CaptainedTeams { get; set; } = new List<Team>();
        public virtual Player? PlayerProfile { get; set; }
    }
}