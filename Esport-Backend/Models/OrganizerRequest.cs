using System.ComponentModel.DataAnnotations;
using TForge.Common;

namespace TForge.Models
{
    /// <summary>
    /// Заявка на роль організатора.
    ///
    /// Роль організатора дає право створювати турніри, тож видавати її за
    /// самим лише вибором у формі реєстрації не можна — інакше нею стає
    /// будь-хто. Заявку розглядає адміністратор, і лише схвалення переводить
    /// User.Role у Organizer.
    ///
    /// Форма повторює TeamMembershipRequest, але відповідач тут не друга
    /// сторона обміну, а будь-який адміністратор — тому окремої колонки під
    /// нього немає.
    /// </summary>
    public class OrganizerRequest
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        /// <summary>Чим заявник обґрунтовує запит. Бачить лише адміністратор.</summary>
        [StringLength(500)]
        public string Message { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = OrganizerRequestStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? RespondedAt { get; set; }

        /// <summary>Адміністратор, який розглянув заявку.</summary>
        public int? RespondedByUserId { get; set; }

        /// <summary>Чому відмовлено. Порожнє для схвалених.</summary>
        [StringLength(500)]
        public string ResponseNote { get; set; } = string.Empty;

        // Navigation Properties
        public virtual User User { get; set; } = null!;
    }
}
