namespace TForge.Common
{
    /// <summary>
    /// Правила викликів на матч. Чисті функції без EF та сервісів —
    /// саме тому їх можна перевіряти юніт-тестами без бази.
    /// Сервіс читає дані, а кожне рішення ухвалює цей клас.
    /// </summary>
    public static class MatchChallengePolicy
    {
        /// <summary>
        /// Усі id — це id користувачів (User.Id), а не команд:
        /// Team.CaptainId посилається саме на User.
        /// </summary>
        public record Context(
            string Status,
            int InitiatedByUserId,
            int ChallengerCaptainUserId,
            int OpponentCaptainUserId);

        /// <summary>
        /// Відповідає завжди капітан викликаної команди. Виклик надсилає
        /// капітан-ініціатор, тож відповісти сам собі він не може.
        /// </summary>
        public static int ResponderUserId(Context context) => context.OpponentCaptainUserId;

        public static bool IsPending(Context context) =>
            context.Status == MatchChallengeStatus.Pending;

        public static bool CanRespond(Context context, int userId, bool isAdmin) =>
            IsPending(context) && (isAdmin || userId == ResponderUserId(context));

        public static bool CanCancel(Context context, int userId, bool isAdmin) =>
            IsPending(context) && (isAdmin || userId == context.InitiatedByUserId);
    }
}
