namespace TForge.Common
{
    /// <summary>
    /// Ліги рейтингу. Ключі зберігаються й порівнюються, підписи живуть на
    /// фронтенді (Esport-Frontend/src/constants/ratingTiers.ts) поруч із рештою
    /// українського тексту — так само, як у Games.
    /// </summary>
    public static class RatingTiers
    {
        public const string Bronze = "Bronze";
        public const string Silver = "Silver";
        public const string Gold = "Gold";
        public const string Platinum = "Platinum";
        public const string Elite = "Elite";

        public static readonly string[] All = { Bronze, Silver, Gold, Platinum, Elite };

        public static bool IsValid(string? tier) => tier != null && All.Contains(tier);

        /// <summary>
        /// Межі лише зростають, тож ліга однозначна для будь-якого рейтингу,
        /// зокрема й нижче підлоги EloCalculator.
        ///
        /// Нижня межа навмисно вища за EloCalculator.BaseRating: новачок
        /// має починати з бронзи. Коли старт потрапляв у срібло, бронза
        /// діставалася лише поразками й читалася як покарання, а не як
        /// початок шляху.
        /// </summary>
        public static string ForRating(int rating) => rating switch
        {
            < 1100 => Bronze,
            < 1250 => Silver,
            < 1400 => Gold,
            < 1550 => Platinum,
            _ => Elite
        };
    }
}
