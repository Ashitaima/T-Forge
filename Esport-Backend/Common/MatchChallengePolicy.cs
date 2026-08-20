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
            int? OpponentCaptainUserId);

        /// <summary>
        /// Відповідає капітан викликаної команди. У відкритому виклику такої
        /// команди ще немає, тож і конкретного адресата теж — null.
        /// </summary>
        public static int? ResponderUserId(Context context) => context.OpponentCaptainUserId;

        /// <summary>
        /// Відкритий виклик — той, у якому суперника ще не названо. Прийняти
        /// його може капітан будь-якої іншої команди; адресний — лише той,
        /// кого викликали. Той самий поділ, що в DuelPolicy.
        /// </summary>
        public static bool IsOpen(Context context) => context.OpponentCaptainUserId == null;

        public static bool IsPending(Context context) =>
            context.Status == MatchChallengeStatus.Pending;

        /// <summary>
        /// Прийняти власний виклик не можна в жодному разі — інакше згоди
        /// другої сторони не існувало б, і це стосується адміністратора теж.
        /// </summary>
        public static bool CanRespond(Context context, int userId, bool isAdmin)
        {
            if (!IsPending(context))
            {
                return false;
            }

            if (IsOpen(context))
            {
                return userId != context.InitiatedByUserId;
            }

            return isAdmin || userId == context.OpponentCaptainUserId;
        }

        public static bool CanCancel(Context context, int userId, bool isAdmin) =>
            IsPending(context) && (isAdmin || userId == context.InitiatedByUserId);
    }
}
