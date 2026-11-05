namespace TForge.Common
{
    /// <summary>
    /// Шаховий рейтинг гравця: точне число та смуга з GameRanks.Chess.
    /// Смуга виводиться з числа, а не вводиться окремо, — інакше профіль міг
    /// би стверджувати «1850» і «до 1000» водночас. Модель прогнозу бере
    /// точне число, а якщо його немає — середину смуги.
    /// Дзеркало BandFor на клієнті — Esport-Frontend/src/constants/chessRatings.ts.
    /// </summary>
    public static class ChessRatings
    {
        public const int Min = 100;
        public const int Max = 3500;

        private const string Prefix = "Chess";

        private static int LowerBound(string band) => int.Parse(band[Prefix.Length..]);

        public static string BandFor(int rating)
        {
            for (var i = GameRanks.Chess.Length - 1; i > 0; i--)
            {
                if (rating >= LowerBound(GameRanks.Chess[i]))
                {
                    return GameRanks.Chess[i];
                }
            }

            return GameRanks.Chess[0];
        }

        public static int? Midpoint(string? band)
        {
            if (string.IsNullOrEmpty(band) || !GameRanks.Chess.Contains(band))
            {
                return null;
            }

            // Крайні смуги відкриті; беремо по сотні вглиб, як і для решти.
            if (band == GameRanks.Chess[0]) return 900;
            return LowerBound(band) + 100;
        }

        public static int? Effective(int? rating, string? band) => rating ?? Midpoint(band);
    }
}
