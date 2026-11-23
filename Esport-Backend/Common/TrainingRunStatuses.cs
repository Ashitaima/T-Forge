namespace TForge.Common
{
    /// <summary>
    /// Стани запуску навчання. Рядкові статуси ніде не пишуться літералами —
    /// та сама причина, що й у MatchStatus: чотири шари, які розійшлися в
    /// написанні, вже одного разу тихо зламали живі матчі.
    /// </summary>
    public static class TrainingRunStatuses
    {
        public const string Queued = "Queued";
        public const string Running = "Running";
        public const string Succeeded = "Succeeded";
        public const string Failed = "Failed";

        public static readonly string[] All = { Queued, Running, Succeeded, Failed };

        public static bool IsValid(string? status) => status != null && All.Contains(status);

        /// <summary>Запуск завершено — результат уже не зміниться.</summary>
        public static bool IsFinished(string status) => status is Succeeded or Failed;

        /// <summary>Запуск ще в роботі: саме це блокує постановку наступного.</summary>
        public static bool IsUnfinished(string status) => !IsFinished(status);
    }
}
