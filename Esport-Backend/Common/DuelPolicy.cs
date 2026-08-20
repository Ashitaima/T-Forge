namespace TForge.Common
{
    /// <summary>
    /// Хто що може робити з дуеллю. Чиста функція, як FriendlyMatchPolicy та
    /// MatchCreationPolicy — сервіс читає рядки, рішення ухвалює цей клас.
    ///
    /// Дуель не має ні організатора, ні капітана: її ведуть двоє, і жодна роль
    /// тут нічого не додає, крім адміністраторської.
    /// </summary>
    public static class DuelPolicy
    {
        /// <summary>
        /// Id гравців (Player.Id), а не користувачів: дуель грають гравці.
        /// `ChallengerUserId`/`OpponentUserId` — акаунти за цими профілями,
        /// саме з ними порівнюється той, хто прийшов із токеном.
        /// </summary>
        public record Context(
            string Status,
            int ChallengerUserId,
            int? OpponentUserId);

        /// <summary>
        /// Відкритий виклик — той, у якому суперника ще не названо. Прийняти
        /// його може будь-хто, крім ініціатора; адресний — лише названий гравець.
        /// </summary>
        public static bool IsOpen(Context context) => context.OpponentUserId == null;

        public static bool IsParticipant(Context context, int userId) =>
            userId == context.ChallengerUserId || userId == context.OpponentUserId;

        /// <summary>
        /// На адресний виклик відповідає лише той, кого викликали; на
        /// відкритий — будь-хто, крім ініціатора. Прийняти власний виклик не
        /// можна в жодному разі: інакше згоди другої сторони не існувало б,
        /// і це стосується адміністратора теж.
        /// </summary>
        public static bool CanRespond(Context context, int userId, bool isAdmin)
        {
            if (!DuelStatuses.IsAwaitingResponse(context.Status))
            {
                return false;
            }

            if (IsOpen(context))
            {
                return userId != context.ChallengerUserId;
            }

            return isAdmin || userId == context.OpponentUserId;
        }

        /// <summary>
        /// Скасувати може той, хто викликав, і лише поки на виклик не відповіли.
        /// Після згоди дуель належить обом, і зникати вона має інакше — через
        /// завершення.
        /// </summary>
        public static bool CanCancel(Context context, int userId, bool isAdmin) =>
            DuelStatuses.IsAwaitingResponse(context.Status)
            && (isAdmin || userId == context.ChallengerUserId);

        /// <summary>
        /// Вести дуель — стартувати, ставити рахунок, завершувати — можуть
        /// обидва учасники: організатора, який зробив би це за них, тут немає.
        /// </summary>
        public static bool CanManage(Context context, int userId, bool isAdmin) =>
            DuelStatuses.IsPlayable(context.Status) && (isAdmin || IsParticipant(context, userId));
    }
}
