namespace TForge.DTOs
{
    /// <summary>Імовірності однієї моделі. First — викликач дуелі або господар матчу.</summary>
    public class ModelOutcomeDto
    {
        public string Model { get; set; } = string.Empty;
        public double FirstWin { get; set; }
        public double Draw { get; set; }
        public double SecondWin { get; set; }
    }

    public class ModelMetricsDto
    {
        public string Model { get; set; } = string.Empty;
        public double LogLoss { get; set; }
        public double Brier { get; set; }
        public double Accuracy { get; set; }
    }

    public class PredictionModelInfoDto
    {
        public string TrainedAt { get; set; } = string.Empty;
        public string Dataset { get; set; } = string.Empty;
        public int RowsTrain { get; set; }
        public List<ModelMetricsDto> Metrics { get; set; } = new();
    }

    /// <summary>
    /// Available = false — не помилка, а «прогнозу немає» з причиною: не шахи,
    /// немає рейтингу, моделі не завантажено. Клієнт показує причину замість
    /// вигаданого числа.
    /// </summary>
    public class DuelPredictionDto
    {
        public bool Available { get; set; }
        public string? Reason { get; set; }
        public string? TimeControl { get; set; }

        /// <summary>
        /// Моделі майже не бачили партій із цим контролем часу — прогноз є
        /// екстраполяцією (див. ChessModelInfo.HasLittleDataFor).
        /// </summary>
        public bool LittleData { get; set; }
        public int? WhitePlayerId { get; set; }
        public int? ChallengerRating { get; set; }
        public int? OpponentRating { get; set; }
        public List<ModelOutcomeDto> Outcomes { get; set; } = new();
        public PredictionModelInfoDto? ModelInfo { get; set; }
    }

    public class BoardPredictionDto
    {
        public int Board { get; set; }
        public string HomeNickname { get; set; } = string.Empty;
        public string AwayNickname { get; set; } = string.Empty;
        public int HomeRating { get; set; }
        public int AwayRating { get; set; }
        public bool HomeIsWhite { get; set; }
        public List<ModelOutcomeDto> Outcomes { get; set; } = new();
    }

    /// <summary>Очікувані очки команди за всіма дошками: перемога — 1, нічия — ½.</summary>
    public class ExpectedPointsDto
    {
        public string Model { get; set; } = string.Empty;
        public double Home { get; set; }
        public double Away { get; set; }
    }

    public class MatchPredictionDto
    {
        public bool Available { get; set; }
        public string? Reason { get; set; }
        public string? TimeControl { get; set; }
        public bool LittleData { get; set; }
        public List<BoardPredictionDto> Boards { get; set; } = new();
        public List<ExpectedPointsDto> Totals { get; set; } = new();
        public PredictionModelInfoDto? ModelInfo { get; set; }
    }
}
