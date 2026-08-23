using System.ComponentModel.DataAnnotations;
using TForge.Common;

namespace TForge.Models
{
    /// <summary>
    /// Оголошення про вільне місце у складі команди.
    ///
    /// Дисципліна стоїть тут, а не в Team, і це навмисно: команда сама по собі
    /// гри не має, а організація може шукати гравців одразу у дві. Порожня
    /// позиція, порожній MinRank і null у межах зсуву означають «будь-який» —
    /// вакансія без вимог видима всім, зокрема й тим, хто ранг або пояс не
    /// вказав.
    /// </summary>
    public class TeamOpening
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int TeamId { get; set; }

        [Required]
        [StringLength(50)]
        public string Game { get; set; } = string.Empty;

        /// <summary>Потрібна роль; порожня — будь-яка.</summary>
        [StringLength(50)]
        public string Position { get; set; } = string.Empty;

        /// <summary>Нижня межа рангу; порожня — будь-який.</summary>
        [StringLength(50)]
        public string MinRank { get; set; } = string.Empty;

        /// <summary>Вікно зсуву UTC у хвилинах; null з будь-якого боку — межі немає.</summary>
        public int? MinUtcOffsetMinutes { get; set; }

        public int? MaxUtcOffsetMinutes { get; set; }

        [StringLength(300)]
        public string Pitch { get; set; } = string.Empty;

        [Required]
        [StringLength(20)]
        public string Status { get; set; } = TeamOpeningStatus.Open;

        /// <summary>Id користувача, а не гравця. Заповнює сервер з токена.</summary>
        [Required]
        public int CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? ClosedAt { get; set; }

        // Navigation Properties
        public virtual Team Team { get; set; } = null!;
    }
}
