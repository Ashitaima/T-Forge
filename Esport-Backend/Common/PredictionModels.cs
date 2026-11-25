namespace TForge.Common
{
    /// <summary>Назви моделей прогнозу — ключі в DTO. Дзеркало MODEL_LABELS у PredictionPanel.tsx.</summary>
    public static class PredictionModels
    {
        public const string LogisticRegression = "LogisticRegression";
        public const string GradientBoosting = "GradientBoosting";
        public const string Elo = "Elo";

        public static readonly string[] All = { LogisticRegression, GradientBoosting, Elo };

        /// <summary>
        /// Ключі метрик у model_info.json → назви моделей у DTO. Спільні для
        /// ChessModelRunner (який читає активну модель) і ChessTrainingService
        /// (який читає збережений MetricsJson запуску) — дві копії цієї
        /// відповідності неминуче розійшлися б.
        /// </summary>
        public static readonly IReadOnlyDictionary<string, string> FileKeys =
            new Dictionary<string, string>
            {
                ["logreg"] = LogisticRegression,
                ["gbt"] = GradientBoosting,
                ["elo"] = Elo
            };

        /// <summary>
        /// Менше стількох навчальних партій на контроль часу — прогноз для нього
        /// є екстраполяцією, і клієнт показує «мало даних».
        /// </summary>
        public const int MinTrainingGamesPerTimeControl = 100;
    }
}
