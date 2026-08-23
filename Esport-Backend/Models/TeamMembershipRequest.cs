using System.ComponentModel.DataAnnotations;
using TForge.Common;

namespace TForge.Models
{
    /// <summary>
    /// Запит на членство в команді. Напрям визначає лише те, хто має право відповісти:
    /// на запрошення відповідає гравець, на заявку — капітан. Шлях прийняття однаковий.
    /// </summary>
    public class TeamMembershipRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeamId { get; set; }

        [Required]
        public int PlayerId { get; set; }

        [Required]
        [StringLength(20)]
        public string Direction { get; set; } = MembershipRequestDirection.Application;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = MembershipRequestStatus.Pending;

        /// <summary>Id користувача, а не гравця. Заповнює сервер з токена.</summary>
        [Required]
        public int InitiatedByUserId { get; set; }

        /// <summary>
        /// Вакансія, з якої постав запит; null — звичайний шлях, не LFT.
        /// Потрібна, щоб прийняття закрило саме ту вакансію, яку заповнили:
        /// команда й гравець цього не визначають, бо вакансій у команди
        /// може бути дві, у різних дисциплінах.
        /// </summary>
        public int? TeamOpeningId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }

        public int? RespondedByUserId { get; set; }

        // Navigation Properties
        public virtual Team Team { get; set; } = null!;
        public virtual Player Player { get; set; } = null!;
    }
}
