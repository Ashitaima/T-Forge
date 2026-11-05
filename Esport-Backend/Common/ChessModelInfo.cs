namespace TForge.Common
{
    public sealed class ChessModelMetrics
    {
        public double LogLoss { get; set; }
        public double Brier { get; set; }
        public double Accuracy { get; set; }
        public double DrawRecall { get; set; }
    }

    /// <summary>
    /// Що відомо про навчені моделі: коли й на чому навчено, метрики на тесті
    /// (ключі — PredictionModels) і скільки навчальних партій мав кожен
    /// контроль часу. Останнє вирішує, чи прогноз — екстраполяція.
    /// </summary>
    public sealed record ChessModelInfo(
        string TrainedAt,
        string Dataset,
        int RowsTrain,
        double EloDrawRate,
        IReadOnlyDictionary<string, int> TrainGamesByTimeControl,
        IReadOnlyDictionary<string, ChessModelMetrics> Metrics)
    {
        public bool HasLittleDataFor(string timeControl) =>
            TrainGamesByTimeControl.GetValueOrDefault(timeControl) < PredictionModels.MinTrainingGamesPerTimeControl;
    }
}
