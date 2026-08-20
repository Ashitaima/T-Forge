namespace TForge.Common
{
    /// <summary>
    /// Статуси заявки на роль організатора. Approved, Declined і Cancelled —
    /// термінальні. Значення навмисно збігаються з іншими запитами:
    /// фронтенд показує їх однаковими пігулками.
    ///
    /// Approved замість Accepted — рішення тут ухвалює адміністратор, а не
    /// друга сторона обміну, і слово має це відображати.
    /// </summary>
    public static class OrganizerRequestStatus
    {
        public const string Pending = "Pending";
        public const string Approved = "Approved";
        public const string Declined = "Declined";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All = { Pending, Approved, Declined, Cancelled };

        public static bool IsValid(string? status) => status != null && All.Contains(status);

        public static bool IsFinal(string? status) =>
            status == Approved || status == Declined || status == Cancelled;
    }
}
