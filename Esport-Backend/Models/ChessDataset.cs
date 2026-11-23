using System.ComponentModel.DataAnnotations;

namespace TForge.Models
{
    /// <summary>
    /// Завантажений набір партій, на якому можна навчити модель.
    ///
    /// Самі рядки лежать файлом на диску, а тут — лише шлях: та сама схема, що
    /// в аватарів і логотипів. Корінь при цьому інший і навмисно: MlData живе
    /// поза wwwroot, бо віддавати датасет статикою нікому не треба.
    /// </summary>
    public class ChessDataset
    {
        [Key]
        public int Id { get; set; }

        /// <summary>Мітка від адміністратора — те, що видно у списках.</summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>Як файл називався у клієнта. Лише для показу: шляхом це ніколи не стає.</summary>
        [Required]
        [StringLength(260)]
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>Шлях відносно ContentRootPath, не WebRootPath.</summary>
        [Required]
        [StringLength(400)]
        public string StoredPath { get; set; } = string.Empty;

        /// <summary>Common/ChessDatasetProfiles — вердикт train.py --validate.</summary>
        [Required]
        [StringLength(20)]
        public string Profile { get; set; } = string.Empty;

        public int RowCount { get; set; }

        public long SizeBytes { get; set; }

        /// <summary>
        /// JSON «контроль часу → кількість партій», як його віддав --validate.
        ///
        /// Текстом, а не jsonb: довільний словник у jsonb вимагає
        /// EnableDynamicJson() у Npgsql, і це вмикає динамічну сериалізацію для
        /// всієї моделі заради одного стовпця, який ніхто не фільтрує в SQL.
        ///
        /// Показувати його обов'язково: у вбудованій вибірці нуль партій у Bullet
        /// і чотири в Blitz, і адміністратор має бачити таку діру до навчання, а
        /// не дізнаватися про неї з позначки «мало даних» на готовому прогнозі.
        /// </summary>
        [Required]
        public string GamesByTimeControlJson { get; set; } = "{}";

        /// <summary>
        /// False — у файлі не було колонки впорядкування, тож спліт позиційний.
        /// Метрики від цього не хибні, але хронологічними вже не є.
        /// </summary>
        public bool IsTimeOrdered { get; set; }

        [Required]
        public int UploadedByUserId { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public virtual User UploadedBy { get; set; } = null!;
        public virtual ICollection<ChessTrainingRun> Runs { get; set; } = new List<ChessTrainingRun>();
    }
}
