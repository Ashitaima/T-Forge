namespace TForge.Common
{
    /// <summary>
    /// Правила запитів на членство. Чисті функції без EF та сервісів —
    /// саме тому їх можна перевіряти юніт-тестами без бази.
    /// Сервіс читає дані, а кожне рішення ухвалює цей клас.
    /// </summary>
    public static class MembershipRequestPolicy
    {
        /// <summary>
        /// Усі id — це id користувачів (User.Id), а не гравців:
        /// Team.CaptainId і Player.UserId посилаються саме на User.
        /// </summary>
        public record Context(
            string Direction,
            string Status,
            int InitiatedByUserId,
            int TeamCaptainUserId,
            int PlayerUserId);

        /// <summary>
        /// Напрям визначає лише те, хто відповідає: на запрошення — гравець,
        /// на заявку — капітан. Ініціатор відповісти сам собі не може.
        /// </summary>
        public static int ResponderUserId(Context context) =>
            context.Direction == MembershipRequestDirection.Invite
                ? context.PlayerUserId
                : context.TeamCaptainUserId;

        public static bool IsPending(Context context) =>
            context.Status == MembershipRequestStatus.Pending;

        public static bool CanRespond(Context context, int userId, bool isAdmin) =>
            IsPending(context) && (isAdmin || userId == ResponderUserId(context));

        public static bool CanCancel(Context context, int userId, bool isAdmin) =>
            IsPending(context) && (isAdmin || userId == context.InitiatedByUserId);
    }
}
