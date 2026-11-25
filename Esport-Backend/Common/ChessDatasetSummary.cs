using System.Text.Json;
using System.Text.Json.Serialization;

namespace TForge.Common
{
    /// <summary>
    /// Вердикт `train.py --validate`, розібраний.
    ///
    /// Схему датасету вирішує Python і лише він — тут її правил немає, лише
    /// результат. Непридатний файл приходить непорожнім Issues при нульовому
    /// коді виходу; ненульовий код чи нерозбірливий stdout означає зламаний
    /// інструментарій, і сервіс розрізняє ці випадки саме так.
    /// </summary>
    public sealed record ChessDatasetSummary(
        string? Profile,
        int Rows,
        IReadOnlyDictionary<string, int> ByTimeControl,
        bool IsTimeOrdered,
        IReadOnlyList<string> Issues)
    {
        /// <summary>
        /// Профіль, якого немає в каталозі, так само непридатний, як відсутня
        /// колонка: він означає, що Python і C# розійшлися.
        /// </summary>
        public bool IsUsable =>
            Issues.Count == 0 && Rows > 0 && ChessDatasetProfiles.IsValid(Profile);

        public string IssueText => string.Join("; ", Issues);

        public static ChessDatasetSummary Rejected(string issue) =>
            new(null, 0, new Dictionary<string, int>(), false, new[] { issue });

        public static ChessDatasetSummary Parse(string json)
        {
            try
            {
                var file = JsonSerializer.Deserialize<Payload>(json, Options);
                if (file == null)
                {
                    return Rejected("Порожній вердикт перевірки датасету");
                }

                return new ChessDatasetSummary(
                    file.Profile,
                    file.Rows,
                    file.ByTimeControl ?? new Dictionary<string, int>(),
                    file.IsTimeOrdered,
                    file.Issues ?? Array.Empty<string>());
            }
            catch (JsonException exception)
            {
                return Rejected($"Не вдалося прочитати вердикт перевірки: {exception.Message}");
            }
        }

        private static readonly JsonSerializerOptions Options = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
        };

        private sealed class Payload
        {
            public string? Profile { get; set; }
            public int Rows { get; set; }

            [JsonPropertyName("by_time_control")]
            public Dictionary<string, int>? ByTimeControl { get; set; }

            [JsonPropertyName("is_time_ordered")]
            public bool IsTimeOrdered { get; set; }

            public string[]? Issues { get; set; }
        }
    }
}
