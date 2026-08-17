using System.ComponentModel.DataAnnotations;
using TForge.Common;

namespace TForge.Models
{
    /// <summary>
    /// Запит на участь команди в турнірі. Напрям визначає лише те, хто має
    /// право відповісти: на запрошення відповідає капітан, на заявку —
    /// організатор. Шлях прийняття однаковий — команда потрапляє до складу
    /// учасників турніру.
    /// </summary>
    public class TournamentInvitation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TournamentId { get; set; }

        [Required]
        public int TeamId { get; set; }

        [Required]
        [StringLength(20)]
        public string Direction { get; set; } = TournamentInvitationDirection.Application;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = TournamentInvitationStatus.Pending;

        /// <summary>Id користувача, а не команди. Заповнює сервер з токена.</summary>
        [Required]
        public int InitiatedByUserId { get; set; }

        [StringLength(300)]
        public string Message { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }

        public int? RespondedByUserId { get; set; }

        // Navigation Properties
        public virtual Tournament Tournament { get; set; } = null!;
        public virtual Team Team { get; set; } = null!;
    }
}
