namespace TForge.DTOs
{
    /// <summary>
    /// Супутник multipart-завантаження. Самі байти приходять окремим
    /// параметром IFormFile; тут лише назва.
    ///
    /// UploadedByUserId тут немає навмисно — автора сервер визначає за токеном,
    /// як CaptainId і OrganizerId. Поля, якого немає, не можна підробити.
    /// </summary>
    public class UploadChessDatasetDto
    {
        public string Name { get; set; } = string.Empty;
    }

    /// <summary>Поставити навчання в чергу. Усе інше вирішує сервер.</summary>
    public class CreateTrainingRunDto
    {
        public int DatasetId { get; set; }
    }

    public class ChessDatasetDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string OriginalFileName { get; set; } = string.Empty;
        public string Profile { get; set; } = string.Empty;
        public int RowCount { get; set; }
        public long SizeBytes { get; set; }

        /// <summary>Контроль часу → кількість партій. Розібраний GamesByTimeControlJson.</summary>
        public Dictionary<string, int> GamesByTimeControl { get; set; } = new();

        /// <summary>False — спліт буде позиційним, бо колонки впорядкування не було.</summary>
        public bool IsTimeOrdered { get; set; }
        public DateTime UploadedAt { get; set; }
        public string UploadedByUsername { get; set; } = string.Empty;

        /// <summary>Скільки запусків посилається на датасет — стільки ж блокує його видалення.</summary>
        public int RunCount { get; set; }
    }

    public class TrainingRunDto
    {
        public int Id { get; set; }
        public int DatasetId { get; set; }
        public string DatasetName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime QueuedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public int? RowsTrain { get; set; }
        public int? RowsTest { get; set; }

        /// <summary>Розібраний MetricsJson, у порядку PredictionModels.All.</summary>
        public List<ModelMetricsDto> Metrics { get; set; } = new();

        /// <summary>stdout + stderr. Єдине, з чим діагностувати збій.</summary>
        public string? Log { get; set; }
        public string? ErrorMessage { get; set; }
        public string CreatedByUsername { get; set; } = string.Empty;
    }

    /// <summary>
    /// Available = false — не помилка, а «навчати нічим» із причиною: не
    /// знайдено Python або скрипта. Та сама форма відповіді, що в прогнозу.
    /// </summary>
    public class ChessTrainingStatusDto
    {
        public bool Available { get; set; }
        public string? Reason { get; set; }

        /// <summary>Null — активна вбудована модель із MlModels/chess.</summary>
        public TrainingRunDto? ActiveRun { get; set; }

        /// <summary>Назва набору, на якому навчено вбудовану модель — для підпису «вбудована».</summary>
        public string BaselineDataset { get; set; } = string.Empty;
    }
}
