namespace TForge.Common
{
    /// <summary>
    /// Правила участі в турнірі за запрошеннями. Чисті функції без EF та
    /// сервісів — так само, як MembershipRequestPolicy і MatchChallengePolicy.
    /// Сервіс читає дані, а кожне рішення ухвалює цей клас.
    /// </summary>
    public static class TournamentInvitationPolicy
    {
        /// <summary>
        /// Усі id — це id користувачів (User.Id): Tournament.OrganizerId
        /// і Team.CaptainId посилаються саме на User.
        /// </summary>
        public record Context(
            string Direction,
            string Status,
            int InitiatedByUserId,
            int OrganizerUserId,
            int TeamCaptainUserId);

        /// <summary>
        /// Напрям визначає лише те, хто відповідає: на запрошення — капітан,
        /// на заявку — організатор. Ініціатор відповісти сам собі не може.
        /// </summary>
        public static int ResponderUserId(Context context) =>
            context.Direction == TournamentInvitationDirection.Invite
                ? context.TeamCaptainUserId
                : context.OrganizerUserId;

        public static bool IsPending(Context context) =>
            context.Status == TournamentInvitationStatus.Pending;

        public static bool CanRespond(Context context, int userId, bool isAdmin) =>
            IsPending(context) && (isAdmin || userId == ResponderUserId(context));

        public static bool CanCancel(Context context, int userId, bool isAdmin) =>
            IsPending(context) && (isAdmin || userId == context.InitiatedByUserId);

        /// <summary>Надіслати запрошення може лише організатор турніру.</summary>
        public static bool CanInvite(int userId, int organizerUserId, bool isAdmin) =>
            isAdmin || userId == organizerUserId;

        /// <summary>Подати заявку може лише капітан команди.</summary>
        public static bool CanApply(int userId, int teamCaptainUserId, bool isAdmin) =>
            isAdmin || userId == teamCaptainUserId;

        /// <summary>
        /// Хто може зареєструвати команду одним рухом, без запиту. На закритому
        /// турнірі капітан цього права не має — саме в цьому й суть перемикача:
        /// склад учасників визначає організатор, а не швидкість кліку.
        /// Організатор і адміністратор реєструють будь-кого й далі: інакше
        /// вони не могли б зібрати турнір, який самі ж оголосили закритим.
        /// </summary>
        public static bool CanRegisterDirectly(
            bool isInviteOnly, int userId, int organizerUserId, int teamCaptainUserId, bool isAdmin) =>
            isAdmin
            || userId == organizerUserId
            || (!isInviteOnly && userId == teamCaptainUserId);
    }
}
