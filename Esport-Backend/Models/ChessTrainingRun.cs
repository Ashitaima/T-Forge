using System.ComponentModel.DataAnnotations;
using TForge.Common;

namespace TForge.Models
{
    /// <summary>
    /// Один запуск навчання і, якщо він удався, одна версія моделі.
    ///
    /// Активна версія — рівно одна, і нуль активних означає вбудований baseline
    /// із MlModels/chess. Через це міграція нічого не вмикає: чистий чекаут уже
    /// в правильному стані. Два активні рядки неможливі структурно —
    /// фільтрований унікальний індекс, той самий хід, що в рейтинговому журналі.
    /// </summary>
    public class ChessTrainingRun
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int DatasetId { get; set; }

        /// <summary>Common/TrainingRunStatuses.</summary>
        [Required]
        [StringLength(20)]
        public string Status { get; set; } = TrainingRunStatuses.Queued;

        public bool IsActive { get; set; }

        public DateTime QueuedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartedAt { get; set; }

        public DateTime? FinishedAt { get; set; }

        /// <summary>Тека з logreg.onnx, gbt.onnx і model_info.json. Null, поки запуск не вдався.</summary>
        [StringLength(400)]
        public string? ModelPath { get; set; }

        public int? RowsTrain { get; set; }

        public int? RowsTest { get; set; }

        /// <summary>
        /// Блок metrics із model_info.json як є — щоб список запусків
        /// порівнювався без читання диска.
        /// </summary>
        public string? MetricsJson { get; set; }

        /// <summary>stdout + stderr скрипта, обрізані. Єдине, з чим діагностувати збій.</summary>
        public string? Log { get; set; }

        [StringLength(500)]
        public string? ErrorMessage { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }

        // Navigation Properties
        public virtual ChessDataset Dataset { get; set; } = null!;
        public virtual User CreatedBy { get; set; } = null!;
    }
}
