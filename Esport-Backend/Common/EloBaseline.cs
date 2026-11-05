namespace TForge.Common
{
    /// <summary>
    /// Базова лінія для порівняння моделей: очікуваний результат Ело плюс
    /// стала частка нічиїх із навчальної вибірки. Дзеркало elo_baseline у
    /// ml/chess/features.py. Обидві моделі мусять бути кращими за неї.
    ///
    /// Окремо від EloCalculator: той рухає рейтинг T-Forge після матчу, а
    /// ця лише оцінює шанси партії до її початку.
    /// </summary>
    public static class EloBaseline
    {
        public static ChessOutcome Predict(int white, int black, double drawRate)
        {
            var expected = 1.0 / (1.0 + Math.Pow(10, (black - white) / 400.0));
            var w = Math.Max(expected - drawRate / 2, 0);
            var b = Math.Max(1 - expected - drawRate / 2, 0);
            var total = w + drawRate + b;

            return new ChessOutcome(w / total, drawRate / total, b / total);
        }
    }
}
