using System.ComponentModel.DataAnnotations;

namespace TForge.Models
{
    /// <summary>
    /// Змах ліворуч: пара «гравець + вакансія», яку більше не показуємо.
    ///
    /// Один рядок прибирає пару з обох стрічок — і зі стрічки гравця, і зі
    /// стрічки капітана. Якщо одна сторона вже відмовила, показувати картку
    /// другій немає сенсу: погодитись удвох вони все одно не зможуть.
    /// PassedByUserId лишає слід, хто саме відмовив.
    /// </summary>
    public class LftPass
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int PlayerId { get; set; }

        [Required]
        public int TeamOpeningId { get; set; }

        /// <summary>Id користувача — гравця або капітана.</summary>
        [Required]
        public int PassedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual Player Player { get; set; } = null!;
        public virtual TeamOpening TeamOpening { get; set; } = null!;
    }
}
