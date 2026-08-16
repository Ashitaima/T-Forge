namespace TForge.Common
{
    /// <summary>
    /// Хто має право вести матч: змінювати рахунок, починати й завершувати його,
    /// задавати посилання на трансляцію.
    ///
    /// Чисті функції без EF та сервісів — сервіс читає дані, а рішення ухвалює цей клас.
    ///
    /// Товариський матч народжується з виклику між двома капітанами, і жодного
    /// організатора в ньому немає — тож вести його можуть самі капітани.
    /// Турнірний матч вони вести не можуть: інакше капітан зараховував би собі
    /// перемогу в чужому турнірі.
    /// </summary>
    public static class FriendlyMatchPolicy
    {
        /// <summary>Усі id — це id користувачів (User.Id): Team.CaptainId посилається на User.</summary>
        public record Context(int? TournamentId, int HomeCaptainUserId, int AwayCaptainUserId);

        public static bool IsFriendly(Context context) => context.TournamentId == null;

        public static bool IsCaptain(Context context, int userId) =>
            userId == context.HomeCaptainUserId || userId == context.AwayCaptainUserId;

        public static bool CanManage(Context context, int userId, bool isAdmin, bool isOrganizer) =>
            isAdmin || isOrganizer || (IsFriendly(context) && IsCaptain(context, userId));
    }
}
