namespace TForge.Common
{
    /// <summary>
    /// Хто має право створити матч. Чиста функція, як FriendlyMatchPolicy та
    /// TournamentOwnershipPolicy — сервіс читає рядки, рішення ухвалює цей клас.
    ///
    /// Досі створення було закрите атрибутом «Admin,Organizer», і капітан не
    /// мав жодного способу призначити матч, окрім виклику іншого капітана.
    /// Поділ той самий, що й у веденні матчу:
    ///
    ///   • товариський матч (без турніру) — його ставлять самі команди, тож
    ///     створити його може капітан будь-якої з двох сторін;
    ///   • турнірний матч — лише організатор *цього* турніру або адміністратор.
    ///     Роль Organizer сама по собі права не дає: інакше чужий організатор
    ///     дописував би матчі в турнір, якого не веде. Та сама помилка, яку
    ///     свого часу виправила TournamentOwnershipPolicy.
    /// </summary>
    public static class MatchCreationPolicy
    {
        /// <summary>Усі id — це id користувачів (User.Id): Team.CaptainId посилається на User.</summary>
        public record Context(
            int? TournamentId,
            int? TournamentOrganizerUserId,
            int? HomeCaptainUserId,
            int? AwayCaptainUserId);

        public static bool IsFriendly(Context context) => context.TournamentId == null;

        public static bool IsCaptain(Context context, int userId) =>
            (context.HomeCaptainUserId.HasValue && context.HomeCaptainUserId == userId)
            || (context.AwayCaptainUserId.HasValue && context.AwayCaptainUserId == userId);

        public static bool CanCreate(Context context, int userId, bool isAdmin, bool isOrganizer)
        {
            if (isAdmin)
            {
                return true; // адміністратор вище за будь-яке правило власності
            }

            if (IsFriendly(context))
            {
                return isOrganizer || IsCaptain(context, userId);
            }

            return context.TournamentOrganizerUserId.HasValue
                && context.TournamentOrganizerUserId == userId;
        }
    }
}
