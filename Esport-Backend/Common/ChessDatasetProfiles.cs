namespace TForge.Common
{
    /// <summary>
    /// Профілі датасетів — дзеркало LICHESS/MINIMAL у ml/chess/datasets.py.
    /// Схему вирішує Python і лише він; тут значення потрібні, щоб зберегти
    /// вердикт і показати його, а не щоб повторити правила.
    /// </summary>
    public static class ChessDatasetProfiles
    {
        public const string Minimal = "Minimal";
        public const string Lichess = "Lichess";

        public static readonly string[] All = { Minimal, Lichess };

        public static bool IsValid(string? profile) => profile != null && All.Contains(profile);
    }
}
