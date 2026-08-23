namespace TForge.Common
{
    /// <summary>
    /// Хто що може в драфті. Чиста функція без EF, як FriendlyMatchPolicy.
    ///
    /// Тут два різні права, і плутати їх не можна.
    ///
    /// CanAct — банити й пікати. Може лише капітан тієї команди, чий зараз хід.
    /// Метод навмисно **не приймає isAdmin**: це виняток із правила
    /// «адміністратор стоїть над кожним правилом власності». Сенс драфту в
    /// тому, що обидві сторони обрали самі, а адміністратор, який банить за
    /// команду, лишає запис, який бреше про те, що сталося. Відсутнього
    /// капітана вже покриває таймер, тож адміністраторові лишається не бан, а
    /// скасування.
    ///
    /// CanManage — створити, скасувати, перезапустити. Тут isAdmin діє як
    /// завжди, і саме це заміняє адміністраторові право ходити.
    ///
    /// Той самий крій, що в MatchChallengePolicy.CanRespond, який теж
    /// відмовляє адміністраторові-ініціаторові.
    /// </summary>
    public static class DraftPolicy
    {
        /// <summary>Капітанські id — це User.Id: Team.CaptainId посилається на User.</summary>
        public record Context(
            int HomeTeamId,
            int? AwayTeamId,
            int HomeCaptainUserId,
            int? AwayCaptainUserId,
            int? TournamentId);

        /// <summary>Капітан названої команди або null, якщо команда до матчу не належить.</summary>
        public static int? CaptainOf(Context context, int teamId)
        {
            if (teamId == context.HomeTeamId)
            {
                return context.HomeCaptainUserId;
            }

            return context.AwayTeamId.HasValue && teamId == context.AwayTeamId.Value
                ? context.AwayCaptainUserId
                : null;
        }

        /// <summary>
        /// Чи може цей користувач зробити хід за команду actingTeamId.
        /// actingTeamId порожній у кроці Decider — його не робить ніхто.
        /// </summary>
        public static bool CanAct(Context context, int userId, int? actingTeamId)
        {
            if (!actingTeamId.HasValue)
            {
                return false;
            }

            var captain = CaptainOf(context, actingTeamId.Value);
            return captain.HasValue && captain.Value == userId;
        }

        /// <summary>Створити, скасувати, перезапустити драфт.</summary>
        public static bool CanManage(Context context, int userId, bool isAdmin, bool isOrganizer) =>
            isAdmin
            || isOrganizer
            || userId == context.HomeCaptainUserId
            || (context.AwayCaptainUserId.HasValue && userId == context.AwayCaptainUserId.Value);
    }
}
