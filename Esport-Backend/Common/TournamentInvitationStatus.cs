namespace TForge.Common
{
    /// <summary>
    /// Напрям запиту на участь у турнірі: хто його ініціював.
    /// Значення повинні збігатися з тими, що використовує фронтенд.
    /// </summary>
    public static class TournamentInvitationDirection
    {
        /// <summary>Організатор запросив команду — відповідає її капітан.</summary>
        public const string Invite = "Invite";

        /// <summary>Капітан подав заявку — відповідає організатор.</summary>
        public const string Application = "Application";

        public static readonly string[] All = { Invite, Application };

        public static bool IsValid(string? direction) => direction != null && All.Contains(direction);
    }

    /// <summary>
    /// Статуси запиту на участь. Accepted, Declined і Cancelled — термінальні.
    /// </summary>
    public static class TournamentInvitationStatus
    {
        public const string Pending = "Pending";
        public const string Accepted = "Accepted";
        public const string Declined = "Declined";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, Accepted, Declined, Cancelled };

        public static bool IsValid(string? status) => status != null && All.Contains(status);
    }
}
