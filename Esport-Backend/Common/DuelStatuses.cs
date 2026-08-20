namespace TForge.Common
{
    /// <summary>
    /// Стани дуелі. Навмисно окремий перелік, а не MatchStatus: у дуелі є
    /// Pending і Declined, яких у матчу немає, а Postponed тут зайвий.
    /// Спільний перелік означав би, що половина значень недосяжна для кожного
    /// з двох власників — і жодна перевірка про це не сказала б.
    /// </summary>
    public static class DuelStatuses
    {
        /// <summary>Виклик надіслано, суперник ще не відповів.</summary>
        public const string Pending = "Pending";

        /// <summary>Суперник погодився — дуель у розкладі.</summary>
        public const string Accepted = "Accepted";

        public const string Declined = "Declined";
        public const string InProgress = "InProgress";
        public const string Completed = "Completed";
        public const string Cancelled = "Cancelled";

        public static readonly string[] All =
        {
            Pending, Accepted, Declined, InProgress, Completed, Cancelled
        };

        public static bool IsValid(string? status) => status != null && All.Contains(status);

        /// <summary>
        /// Чи чекає дуель на відповідь суперника. Саме цей стан адресує виклик
        /// іншій стороні — так само, як у NotificationAddressing.
        /// </summary>
        public static bool IsAwaitingResponse(string? status) => status == Pending;

        /// <summary>
        /// Дуель, яка вже нікуди не рухається. Скасовану, відхилену чи
        /// завершену не можна ані вести, ані відповідати на неї.
        /// </summary>
        public static bool IsFinal(string? status) =>
            status is Declined or Completed or Cancelled;

        /// <summary>Чи можна її ще грати — тобто стартувати, вести рахунок, завершити.</summary>
        public static bool IsPlayable(string? status) => status is Accepted or InProgress;
    }
}
