namespace TForge.Common
{
    /// <summary>
    /// Хто що може робити із заявкою на роль організатора. Чиста функція, як
    /// TournamentOwnershipPolicy і FriendlyMatchPolicy — сервіс читає рядки,
    /// рішення ухвалює цей клас.
    /// </summary>
    public static class OrganizerRequestPolicy
    {
        public record Context(string Status, int SubjectUserId);

        public static bool IsPending(Context context) =>
            context.Status == OrganizerRequestStatus.Pending;

        /// <summary>
        /// Розглядає заявку лише адміністратор — саме тому роль і не видається
        /// самою реєстрацією.
        /// </summary>
        public static bool CanRespond(Context context, bool isAdmin) =>
            IsPending(context) && isAdmin;

        /// <summary>
        /// Відкликати може лише заявник і лише поки заявку не розглянуто.
        /// Адміністратор натомість відмовляє — це різні події, і слід від них
        /// має лишатися різний.
        /// </summary>
        public static bool CanCancel(Context context, int userId) =>
            IsPending(context) && userId == context.SubjectUserId;

        /// <summary>
        /// Подати заявку може лише той, хто ще не організатор і не
        /// адміністратор: обидві ролі вже містять це право, і заявка від них
        /// нічого не змінила б.
        /// </summary>
        public static bool CanApply(string currentRole) => currentRole == UserRoles.Player;
    }
}
