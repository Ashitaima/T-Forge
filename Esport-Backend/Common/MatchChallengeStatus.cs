namespace TForge.Common
{
    /// <summary>
    /// Статуси виклику на матч. Accepted, Declined і Cancelled — термінальні.
    /// Значення навмисно збігаються зі статусами запитів на членство:
    /// фронтенд показує обидва однаковими пігулками.
    /// </summary>
    public static class MatchChallengeStatus
    {
        public const string Pending = "Pending";
        public const string Accepted = "Accepted";
        public const string Declined = "Declined";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, Accepted, Declined, Cancelled };

        public static bool IsValid(string? status) => status != null && All.Contains(status);
    }
}
