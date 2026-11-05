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
        /// Менше стількох навчальних партій на контроль часу — прогноз для нього
        /// є екстраполяцією, і клієнт показує «мало даних».
        /// </summary>
        public const int MinTrainingGamesPerTimeControl = 100;
    }
}
